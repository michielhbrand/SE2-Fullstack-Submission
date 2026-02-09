# Rate Limiting Implementation

## Overview

This document describes the rate limiting implementation for the Management API using AspNetCoreRateLimit middleware. Rate limiting helps protect the API from abuse, prevents DDoS attacks, and ensures fair resource usage across clients.

## Implementation Details

### Package Used
- **AspNetCoreRateLimit** (v5.0.0) - A flexible rate limiting solution for ASP.NET Core

### Configuration Location
Rate limiting configuration is stored in `appsettings.json` under the following sections:
- `IpRateLimiting` - IP-based rate limiting rules
- `IpRateLimitPolicies` - Specific IP policies
- `ClientRateLimiting` - Client ID-based rate limiting
- `EndpointRateLimits` - Documentation of per-endpoint limits

## Rate Limiting Rules

### General Rules (All Endpoints)
- **60 requests per minute** per IP address
- **1000 requests per hour** per IP address

### Authentication Endpoints (Strict Throttling)

#### POST /api/auth/login
- **5 requests per minute**
- **10 requests per 15 minutes**
- Protects against brute force attacks

#### POST /api/auth/refresh
- **10 requests per minute**
- Prevents token refresh abuse

#### POST /api/auth/logout
- **20 requests per minute**
- More lenient as logout is less sensitive

### User Endpoints
- **GET /api/users\***: 30 requests per minute
- **POST /api/users\***: 10 requests per minute
- **PUT /api/users\***: 20 requests per minute

### Organization Endpoints
- **GET /api/organizations\***: 30 requests per minute
- **POST /api/organizations\***: 10 requests per minute
- **PUT /api/organizations\***: 20 requests per minute

### Whitelisted Endpoints
The following endpoints are exempt from rate limiting:
- `GET /health` - Health check endpoint

### Localhost Exception
Requests from `127.0.0.1` (localhost) have higher limits:
- **200 requests per minute** for development convenience

## Configuration

### appsettings.json Structure

```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429,
    "GeneralRules": [...],
    "EndpointWhitelist": [...]
  }
}
```

### Key Configuration Options

- **EnableEndpointRateLimiting**: `true` - Enables per-endpoint rate limiting
- **StackBlockedRequests**: `false` - Blocked requests don't count toward the limit
- **RealIpHeader**: `X-Real-IP` - Header to extract real IP (useful behind proxies)
- **HttpStatusCode**: `429` - HTTP status code for rate limit exceeded (Too Many Requests)

## Response Headers

When rate limiting is active, the following headers are included in responses:

- `X-Rate-Limit-Limit`: Maximum number of requests allowed
- `X-Rate-Limit-Remaining`: Number of requests remaining in current window
- `X-Rate-Limit-Reset`: Time when the rate limit resets (Unix timestamp)

## Rate Limit Exceeded Response

When a client exceeds the rate limit, they receive:

**Status Code**: `429 Too Many Requests`

**Response Body**:
```json
{
  "statusCode": 429,
  "message": "API calls quota exceeded! maximum admitted 5 per 1m."
}
```

## Middleware Order

Rate limiting middleware is positioned in the pipeline as follows:

```
1. Exception Handler
2. CORS
3. Rate Limiting ← Applied here
4. Authentication
5. Authorization
```

This ensures:
- Rate limiting is applied before expensive authentication operations
- CORS headers are included in rate limit responses
- Exceptions are properly handled

## Storage

Rate limit counters are stored in **memory cache** using:
- `MemoryCacheIpPolicyStore` - Stores IP policies
- `MemoryCacheRateLimitCounterStore` - Stores request counters
- `MemoryCacheClientPolicyStore` - Stores client policies

### Production Considerations

For production environments with multiple instances, consider using distributed cache:
- Redis
- SQL Server
- Other distributed cache providers

Update `RateLimitingServiceExtensions.cs` to use distributed cache stores instead of memory cache.

## Testing Rate Limits

### Manual Testing

1. **Test general rate limit**:
```bash
# Make 61 requests in quick succession
for i in {1..61}; do
  curl -X GET http://localhost:5002/api/organizations
done
# The 61st request should return 429
```

2. **Test authentication endpoint throttling**:
```bash
# Make 6 login attempts in quick succession
for i in {1..6}; do
  curl -X POST http://localhost:5002/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"username":"test","password":"test"}'
done
# The 6th request should return 429
```

3. **Check rate limit headers**:
```bash
curl -i http://localhost:5002/api/organizations
# Look for X-Rate-Limit-* headers
```

### Automated Testing

Consider adding integration tests to verify rate limiting behavior:

```csharp
[Fact]
public async Task LoginEndpoint_ShouldReturnTooManyRequests_WhenRateLimitExceeded()
{
    // Make 6 requests
    for (int i = 0; i < 6; i++)
    {
        var response = await _client.PostAsync("/api/auth/login", content);
        if (i < 5)
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        else
            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }
}
```

## Customization

### Adding New Endpoint Rules

To add rate limiting for a new endpoint, update `appsettings.json`:

```json
{
  "IpRateLimiting": {
    "GeneralRules": [
      {
        "Endpoint": "post:/api/your-endpoint",
        "Period": "1m",
        "Limit": 10
      }
    ]
  }
}
```

### Period Formats
- `1s` - 1 second
- `1m` - 1 minute
- `1h` - 1 hour
- `1d` - 1 day

### Whitelisting IPs

To whitelist specific IP addresses, add to `IpRateLimitPolicies`:

```json
{
  "IpRateLimitPolicies": {
    "IpRules": [
      {
        "Ip": "192.168.1.100",
        "Rules": [
          {
            "Endpoint": "*",
            "Period": "1m",
            "Limit": 1000
          }
        ]
      }
    ]
  }
}
```

## Monitoring

### Logging

Rate limiting events are logged automatically. Monitor logs for:
- Rate limit exceeded events
- Configuration errors
- Policy updates

### Metrics

Consider implementing metrics to track:
- Number of rate-limited requests per endpoint
- Top rate-limited IP addresses
- Rate limit hit patterns

## Security Considerations

1. **Proxy Configuration**: Ensure `RealIpHeader` is correctly configured when behind a proxy/load balancer
2. **DDoS Protection**: Rate limiting is one layer; combine with other DDoS protection mechanisms
3. **Distributed Attacks**: Consider implementing distributed rate limiting for multi-instance deployments
4. **Client Identification**: Use both IP and client ID for more accurate rate limiting

## Troubleshooting

### Rate Limits Not Applied

1. Check middleware order in `Program.cs`
2. Verify configuration in `appsettings.json`
3. Check logs for configuration errors
4. Ensure memory cache is properly registered

### Wrong IP Address Detected

1. Verify `RealIpHeader` configuration
2. Check proxy/load balancer configuration
3. Ensure X-Real-IP or X-Forwarded-For headers are set correctly

### Rate Limits Too Strict/Lenient

1. Adjust limits in `appsettings.json`
2. Monitor actual usage patterns
3. Consider different limits for different environments (dev/staging/prod)

## References

- [AspNetCoreRateLimit GitHub](https://github.com/stefanprodan/AspNetCoreRateLimit)
- [RFC 6585 - HTTP Status Code 429](https://tools.ietf.org/html/rfc6585)
- [OWASP Rate Limiting](https://cheatsheetseries.owasp.org/cheatsheets/Denial_of_Service_Cheat_Sheet.html)
