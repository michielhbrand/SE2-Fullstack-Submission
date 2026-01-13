#!/bin/bash

# Script to generate TypeScript API client from OpenAPI specification
# This script should be run from the backend/InvoiceTrackerApi directory

echo "🔧 Generating TypeScript API Client..."
echo ""

# Check if backend is running
if ! curl -s http://localhost:5000/swagger/v1/swagger.json > /dev/null 2>&1; then
    echo "❌ Error: Backend API is not running at http://localhost:5000"
    echo "   Please start the backend first with: dotnet run"
    exit 1
fi

echo "✅ Backend API is running"
echo "📥 Fetching OpenAPI specification..."

# Generate the TypeScript client using NSwag
nswag run nswag.json

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ TypeScript API client generated successfully!"
    echo "   Location: frontend/app/src/api/generated/api-client.ts"
    echo ""
else
    echo ""
    echo "❌ Failed to generate TypeScript client"
    exit 1
fi
