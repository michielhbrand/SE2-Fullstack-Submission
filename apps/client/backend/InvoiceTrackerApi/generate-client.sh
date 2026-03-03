#!/bin/bash

# Script to generate TypeScript API client from OpenAPI specification
# This script should be run from the apps/client/backend/InvoiceTrackerApi directory

echo "🔧 Generating TypeScript API Client..."
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

# Check if backend is running
if ! curl -s http://localhost:5000/swagger/v1/swagger.json > /dev/null 2>&1; then
    echo "❌ Error: Backend API is not running at http://localhost:5000"
    echo "   Please start the backend first with: dotnet run"
    exit 1
fi

echo "✅ Backend API is running"
echo "📥 Fetching OpenAPI specification..."

# Generate the TypeScript client using NSwag
"$NSWAG_PATH" run nswag.json

if [ $? -eq 0 ]; then
    OUTPUT="../../frontend/src/api/generated/api-client.ts"
    printf '%s\n' '// @ts-nocheck' "$(cat "$OUTPUT")" > "$OUTPUT"
    echo ""
    echo "✅ TypeScript API client generated successfully!"
    echo "   Location: apps/client/frontend/src/api/generated/api-client.ts"
    echo ""
else
    echo ""
    echo "❌ Failed to generate TypeScript client"
    exit 1
fi
