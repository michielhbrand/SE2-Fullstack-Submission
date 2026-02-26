using FluentAssertions;
using EmailNotificationService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace EmailNotificationService.Tests.UnitTests;

/// <summary>
/// Unit tests for TokenService — covers the HMAC-signed token generation and validation
/// without any I/O dependencies.
/// </summary>
public class TokenServiceTests
{
    private readonly TokenService _service;

    public TokenServiceTests()
    {
        var loggerMock = new Mock<ILogger<TokenService>>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:TokenSecret"] = "test-secret-key-at-least-32-chars!!"
            })
            .Build();

        _service = new TokenService(loggerMock.Object, config);
    }

    // ─── GenerateToken ────────────────────────────────────────────────────────

    [Fact]
    public void GenerateToken_ReturnsStringWithExactlyOneDot()
    {
        var token = _service.GenerateToken(workflowId: 1, quoteId: 10, action: "approve");

        token.Split('.').Should().HaveCount(2, "token format is payload.signature");
    }

    [Fact]
    public void GenerateToken_TwoCallsProduceDifferentTokens_DueToNonce()
    {
        var token1 = _service.GenerateToken(1, 10, "approve");
        var token2 = _service.GenerateToken(1, 10, "approve");

        token1.Should().NotBe(token2, "the nonce makes each token unique");
    }

    [Fact]
    public void GenerateToken_IsUrlSafe_ContainsNoInvalidChars()
    {
        var token = _service.GenerateToken(1, 10, "reject");

        token.Should().MatchRegex(@"^[A-Za-z0-9\-_\.]+$",
            "URL-safe base64 uses - and _ instead of + and /");
    }

    // ─── ValidateToken — happy path ───────────────────────────────────────────

    [Fact]
    public void ValidateToken_ValidToken_ReturnsCorrectPayload()
    {
        var token = _service.GenerateToken(workflowId: 42, quoteId: 7, action: "approve");

        var result = _service.ValidateToken(token);

        result.Should().NotBeNull();
        result!.Value.WorkflowId.Should().Be(42);
        result!.Value.QuoteId.Should().Be(7);
        result!.Value.Action.Should().Be("approve");
    }

    [Fact]
    public void ValidateToken_RejectAction_ReturnsCorrectAction()
    {
        var token = _service.GenerateToken(workflowId: 5, quoteId: 3, action: "reject");

        var result = _service.ValidateToken(token);

        result.Should().NotBeNull();
        result!.Value.Action.Should().Be("reject");
    }

    // ─── ValidateToken — security / error cases ───────────────────────────────

    [Fact]
    public void ValidateToken_TamperedSignature_ReturnsNull()
    {
        var token = _service.GenerateToken(1, 10, "approve");
        var parts = token.Split('.');
        var tampered = parts[0] + ".INVALIDSIGNATURE";

        var result = _service.ValidateToken(tampered);

        result.Should().BeNull("tampered signature must not validate");
    }

    [Fact]
    public void ValidateToken_TamperedPayload_ReturnsNull()
    {
        var token = _service.GenerateToken(1, 10, "approve");
        var parts = token.Split('.');
        // Replace the payload with a different one while keeping the original signature
        var differentPayload = _service.GenerateToken(99, 88, "reject").Split('.')[0];
        var tampered = differentPayload + "." + parts[1];

        var result = _service.ValidateToken(tampered);

        result.Should().BeNull("mismatched payload and signature must not validate");
    }

    [Fact]
    public void ValidateToken_MalformedToken_NoDot_ReturnsNull()
    {
        var result = _service.ValidateToken("completelyinvalidtoken");

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_EmptyString_ReturnsNull()
    {
        var result = _service.ValidateToken("");

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_DifferentSigningKey_ReturnsNull()
    {
        // Token generated with a different secret should not validate
        var otherConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:TokenSecret"] = "completely-different-secret-key!!"
            })
            .Build();
        var otherService = new TokenService(new Mock<ILogger<TokenService>>().Object, otherConfig);

        var tokenFromOtherService = otherService.GenerateToken(1, 10, "approve");
        var result = _service.ValidateToken(tokenFromOtherService);

        result.Should().BeNull("tokens signed with a different key must not validate");
    }
}
