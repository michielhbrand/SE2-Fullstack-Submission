# Keycloak Event-Driven Sync Implementation Guide

## Overview

This guide explains how to implement real-time event-driven sync from Keycloak to UserDirectory, replacing the current on-demand sync approach.

## Current Implementation

The system currently uses **on-demand sync**:
- Sync happens after user create/update operations
- Manual sync via `POST /api/users/directory/sync` endpoint
- Simple but requires explicit sync calls

## Recommended Approaches

### Option 1: Keycloak Event Listener SPI with Webhooks (Recommended)

**Best for**: Production systems requiring real-time sync with minimal latency

**How it works**:
1. Create custom Keycloak Event Listener extension
2. Configure it to send webhooks to ManagementBackend
3. Implement webhook endpoint to receive and process events
4. Sync affected users immediately

**Implementation Steps**:

#### 1. Create Keycloak Event Listener Extension

Create a Java project for the Keycloak extension:

```java
// KeycloakWebhookEventListener.java
package com.yourcompany.keycloak.events;

import org.keycloak.events.Event;
import org.keycloak.events.EventListenerProvider;
import org.keycloak.events.EventType;
import org.keycloak.events.admin.AdminEvent;
import org.keycloak.events.admin.OperationType;
import org.keycloak.events.admin.ResourceType;

import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;

public class WebhookEventListener implements EventListenerProvider {
    
    private final String webhookUrl;
    private final HttpClient httpClient;
    
    public WebhookEventListener(String webhookUrl) {
        this.webhookUrl = webhookUrl;
        this.httpClient = HttpClient.newHttpClient();
    }
    
    @Override
    public void onEvent(Event event) {
        // Handle user events (login, logout, etc.)
        // Not needed for UserDirectory sync
    }
    
    @Override
    public void onEvent(AdminEvent adminEvent, boolean includeRepresentation) {
        // Handle admin events (user CRUD operations)
        if (adminEvent.getResourceType() == ResourceType.USER) {
            if (adminEvent.getOperationType() == OperationType.CREATE ||
                adminEvent.getOperationType() == OperationType.UPDATE ||
                adminEvent.getOperationType() == OperationType.DELETE) {
                
                // Extract user ID from resource path
                String userId = extractUserIdFromPath(adminEvent.getResourcePath());
                
                // Send webhook
                sendWebhook(userId, adminEvent.getOperationType().toString());
            }
        }
    }
    
    private void sendWebhook(String userId, String operation) {
        try {
            String payload = String.format(
                "{\"userId\":\"%s\",\"operation\":\"%s\",\"timestamp\":\"%s\"}",
                userId, operation, System.currentTimeMillis()
            );
            
            HttpRequest request = HttpRequest.newBuilder()
                .uri(URI.create(webhookUrl))
                .header("Content-Type", "application/json")
                .POST(HttpRequest.BodyPublishers.ofString(payload))
                .build();
            
            httpClient.sendAsync(request, HttpResponse.BodyHandlers.ofString());
        } catch (Exception e) {
            // Log error but don't fail the Keycloak operation
            System.err.println("Failed to send webhook: " + e.getMessage());
        }
    }
    
    private String extractUserIdFromPath(String resourcePath) {
        // Extract user ID from path like "users/abc-123-def"
        String[] parts = resourcePath.split("/");
        return parts[parts.length - 1];
    }
    
    @Override
    public void close() {
        // Cleanup if needed
    }
}
```

#### 2. Deploy Extension to Keycloak

```bash
# Build the extension JAR
mvn clean package

# Copy to Keycloak providers directory
cp target/keycloak-webhook-listener.jar /opt/keycloak/providers/

# Restart Keycloak
docker restart keycloak
```

#### 3. Configure Keycloak

Add to Keycloak realm settings or `keycloak.conf`:

```properties
# Enable event listener
spi-events-listener-webhook-enabled=true
spi-events-listener-webhook-webhook-url=http://management-backend:5002/api/webhooks/keycloak-events
```

#### 4. Implement Webhook Endpoint in ManagementBackend

```csharp
// File: management/backend/Endpoints/Webhooks/KeycloakEventWebhookEndpoint.cs
using ManagementApi.Services.User;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ManagementApi.Endpoints.Webhooks;

public static class KeycloakEventWebhookEndpoint
{
    public static RouteHandlerBuilder MapKeycloakEventWebhook(this IEndpointRouteBuilder app)
    {
        return app.MapPost("/api/webhooks/keycloak-events", Handle)
            .WithName("KeycloakEventWebhook")
            .WithSummary("Webhook endpoint for Keycloak events")
            .AllowAnonymous(); // Or use API key authentication
    }

    private static async Task<Results<Ok, BadRequest>> Handle(
        KeycloakEventPayload payload,
        IUserDirectoryService userDirectoryService,
        ILogger<KeycloakEventWebhookEndpoint> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Received Keycloak event: UserId={UserId}, Operation={Operation}", 
                payload.UserId, payload.Operation);

            if (payload.Operation == "DELETE")
            {
                // Handle user deletion - remove from UserDirectory
                // await userDirectoryService.RemoveUserAsync(payload.UserId, cancellationToken);
            }
            else
            {
                // Handle create/update - sync user
                await userDirectoryService.SyncUserAsync(payload.UserId, cancellationToken);
            }

            return TypedResults.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Keycloak event");
            return TypedResults.BadRequest();
        }
    }
}

public record KeycloakEventPayload(
    string UserId,
    string Operation,
    long Timestamp
);
```

### Option 2: Message Queue Integration

**Best for**: High-volume systems, microservices architecture

**How it works**:
1. Configure Keycloak to publish events to RabbitMQ/Kafka
2. Create consumer service to process events
3. Sync users based on events

**Implementation Steps**:

#### 1. Configure Keycloak Event Publisher

Use Keycloak Event Listener SPI to publish to message queue:

```java
// RabbitMQEventListener.java
public class RabbitMQEventListener implements EventListenerProvider {
    private final ConnectionFactory factory;
    
    @Override
    public void onEvent(AdminEvent adminEvent, boolean includeRepresentation) {
        if (adminEvent.getResourceType() == ResourceType.USER) {
            publishToQueue(adminEvent);
        }
    }
    
    private void publishToQueue(AdminEvent event) {
        try (Connection connection = factory.newConnection();
             Channel channel = connection.createChannel()) {
            
            channel.queueDeclare("keycloak.user.events", true, false, false, null);
            
            String message = serializeEvent(event);
            channel.basicPublish("", "keycloak.user.events", null, message.getBytes());
        } catch (Exception e) {
            // Log error
        }
    }
}
```

#### 2. Create Consumer in ManagementBackend

```csharp
// File: management/backend/BackgroundServices/KeycloakEventConsumerService.cs
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class KeycloakEventConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KeycloakEventConsumerService> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare("keycloak.user.events", true, false, false, null);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            
            await ProcessEventAsync(message, stoppingToken);
            
            _channel.BasicAck(ea.DeliveryTag, false);
        };

        _channel.BasicConsume("keycloak.user.events", false, consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessEventAsync(string message, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var userDirectoryService = scope.ServiceProvider.GetRequiredService<IUserDirectoryService>();
        
        var eventData = JsonSerializer.Deserialize<KeycloakEvent>(message);
        await userDirectoryService.SyncUserAsync(eventData.UserId, cancellationToken);
    }
}
```

### Option 3: Keycloak Admin Events API Polling

**Best for**: Simple setups, development environments

**How it works**:
1. Periodically poll Keycloak Admin Events API
2. Filter for user-related events
3. Sync affected users

**Implementation**: See [`KeycloakEventListenerService.cs`](../management/backend/BackgroundServices/KeycloakEventListenerService.cs) for polling skeleton.

## Comparison

| Approach | Latency | Complexity | Scalability | Reliability |
|----------|---------|------------|-------------|-------------|
| Webhook (Option 1) | Low (< 1s) | Medium | High | High |
| Message Queue (Option 2) | Low (< 1s) | High | Very High | Very High |
| Polling (Option 3) | High (30s+) | Low | Medium | Medium |

## Recommendation

For production: **Option 1 (Webhooks)** or **Option 2 (Message Queue)**
- Real-time sync with minimal latency
- Reliable event delivery
- Scales well

For development: **Current on-demand sync** or **Option 3 (Polling)**
- Simple to implement
- No Keycloak configuration needed
- Good enough for low-volume scenarios

## Configuration

Add to `appsettings.json`:

```json
{
  "KeycloakEventListener": {
    "Enabled": true,
    "Type": "Webhook", // or "MessageQueue" or "Polling"
    "PollIntervalSeconds": 30,
    "WebhookSecret": "your-secret-key",
    "RabbitMQ": {
      "Host": "localhost",
      "Queue": "keycloak.user.events"
    }
  }
}
```

## Security Considerations

1. **Webhook Authentication**: Use API key or HMAC signature
2. **Network Security**: Ensure Keycloak can reach ManagementBackend
3. **Retry Logic**: Implement exponential backoff for failed syncs
4. **Idempotency**: Handle duplicate events gracefully

## Monitoring

Track these metrics:
- Event processing latency
- Sync success/failure rate
- UserDirectory staleness (LastSyncedAt)
- Event queue depth (for message queue approach)

## See Also

- [Keycloak Event Listener SPI Documentation](https://www.keycloak.org/docs/latest/server_development/#_events)
- [Hybrid User Data Model Architecture](./HYBRID_USER_DATA_MODEL.md)
