using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EmailNotificationService.Services;

/// <summary>
/// Simple token service using Base64-encoded JSON with HMAC signature.
/// Tokens encode the workflow ID, quote ID, action, and expiry.
/// </summary>
public class TokenService : ITokenService
{
    private readonly ILogger<TokenService> _logger;
    private readonly byte[] _signingKey;
    private static readonly TimeSpan TokenExpiry = TimeSpan.FromDays(7);

    public TokenService(ILogger<TokenService> logger, IConfiguration configuration)
    {
        _logger = logger;
        // Use a configured secret or generate a deterministic one from connection string
        var secret = configuration["App:TokenSecret"]
            ?? "email-notification-service-default-secret-key-change-in-production";
        _signingKey = Encoding.UTF8.GetBytes(secret);
    }

    public string GenerateToken(int workflowId, int quoteId, string action)
    {
        var payload = new TokenPayload
        {
            WorkflowId = workflowId,
            QuoteId = quoteId,
            Action = action,
            ExpiresAt = DateTime.UtcNow.Add(TokenExpiry).Ticks,
            Nonce = Guid.NewGuid().ToString("N")[..8]
        };

        var json = JsonSerializer.Serialize(payload);
        var payloadBytes = Encoding.UTF8.GetBytes(json);
        var payloadBase64 = Convert.ToBase64String(payloadBytes);

        // Make payload URL-safe
        var urlSafePayload = ToUrlSafeBase64(payloadBase64);

        // Compute signature over the STANDARD base64 payload
        var urlSafeSignature = ComputeSignature(payloadBase64);

        // Token format: urlSafePayload.urlSafeSignature
        return $"{urlSafePayload}.{urlSafeSignature}";
    }

    public (int WorkflowId, int QuoteId, string Action)? ValidateToken(string token)
    {
        try
        {
            // Split the token by '.' delimiter
            var parts = token.Split('.');
            if (parts.Length != 2)
            {
                _logger.LogWarning("Invalid token format: wrong number of parts ({Count})", parts.Length);
                return null;
            }

            var urlSafePayload = parts[0];
            var providedSignature = parts[1];

            // Restore standard base64 for the payload
            var payloadBase64 = FromUrlSafeBase64(urlSafePayload);

            // Verify signature: ComputeSignature takes standard base64 and returns URL-safe signature
            var expectedSignature = ComputeSignature(payloadBase64);

            if (providedSignature != expectedSignature)
            {
                _logger.LogWarning("Invalid token: signature mismatch");
                return null;
            }

            // Decode payload
            var payloadBytes = Convert.FromBase64String(payloadBase64);
            var json = Encoding.UTF8.GetString(payloadBytes);
            var payload = JsonSerializer.Deserialize<TokenPayload>(json);

            if (payload == null)
            {
                _logger.LogWarning("Invalid token: failed to deserialize payload");
                return null;
            }

            // Check expiry
            var expiresAt = new DateTime(payload.ExpiresAt, DateTimeKind.Utc);
            if (DateTime.UtcNow > expiresAt)
            {
                _logger.LogWarning("Token expired for Workflow #{WorkflowId}, Quote #{QuoteId}",
                    payload.WorkflowId, payload.QuoteId);
                return null;
            }

            return (payload.WorkflowId, payload.QuoteId, payload.Action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return null;
        }
    }

    private string ComputeSignature(string payloadBase64)
    {
        using var hmac = new HMACSHA256(_signingKey);
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64));
        return ToUrlSafeBase64(Convert.ToBase64String(signatureBytes));
    }

    /// <summary>
    /// Converts standard base64 to URL-safe base64 (no padding)
    /// </summary>
    private static string ToUrlSafeBase64(string base64)
    {
        return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    /// <summary>
    /// Converts URL-safe base64 back to standard base64 (with padding)
    /// </summary>
    private static string FromUrlSafeBase64(string urlSafe)
    {
        var base64 = urlSafe.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: return base64 + "==";
            case 3: return base64 + "=";
            default: return base64;
        }
    }

    private class TokenPayload
    {
        public int WorkflowId { get; set; }
        public int QuoteId { get; set; }
        public string Action { get; set; } = string.Empty;
        public long ExpiresAt { get; set; }
        public string Nonce { get; set; } = string.Empty;
    }
}
