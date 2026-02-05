#!/bin/bash

# Script to generate TypeScript API client from OpenAPI specification
# This script should be run from the management/backend directory

echo "🔧 Generating Management API TypeScript Client..."
echo ""

# Set DOTNET_ROOT if not already set (required for .NET tools on macOS)
if [ -z "$DOTNET_ROOT" ]; then
    if [ -d "/usr/local/share/dotnet" ]; then
        export DOTNET_ROOT=/usr/local/share/dotnet
    elif [ -d "/opt/homebrew/opt/dotnet/libexec" ]; then
        export DOTNET_ROOT=/opt/homebrew/opt/dotnet/libexec
    fi
fi

# Check if NSwag is installed
NSWAG_PATH="$HOME/.dotnet/tools/nswag"
if [ ! -f "$NSWAG_PATH" ]; then
    echo "❌ Error: NSwag is not installed"
    echo "   Install it globally with: dotnet tool install -g NSwag.ConsoleCore"
    exit 1
fi

echo "✅ NSwag is installed"

# Check if backend is running and OpenAPI is available
if ! curl -s http://localhost:5002/health > /dev/null 2>&1; then
    echo "❌ Error: Management API is not running at http://localhost:5002"
    echo "   Please start the backend first with: ASPNETCORE_ENVIRONMENT=Development dotnet run"
    exit 1
fi

echo "✅ Management API is running"

# Check if OpenAPI endpoint is available (requires Development environment)
if ! curl -s http://localhost:5002/swagger/v1/swagger.json > /dev/null 2>&1; then
    echo "❌ Error: OpenAPI specification not available"
    echo "   The API must be running in Development mode for Swagger/OpenAPI to be enabled."
    echo "   Please restart the backend with: ASPNETCORE_ENVIRONMENT=Development dotnet run"
    exit 1
fi

echo "✅ OpenAPI specification is available"
echo "📥 Fetching OpenAPI specification..."

# Create the output directory if it doesn't exist
mkdir -p ../frontend/src/api/generated

# Generate the TypeScript client using NSwag
"$NSWAG_PATH" run nswag.json

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ TypeScript API client generated successfully!"
    echo "   Location: management/frontend/src/api/generated/api-client.ts"
    echo ""
else
    echo ""
    echo "❌ Failed to generate TypeScript client"
    exit 1
fi
