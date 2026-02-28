FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Inject corporate CA certificate so dotnet restore can reach NuGet through SSL-inspecting proxies
COPY certs/ca-bundle.crt /usr/local/share/ca-certificates/corporate.crt
RUN update-ca-certificates

# Copy project files first for layer caching
COPY shared/Shared.Database/Shared.Database.csproj shared/Shared.Database/
COPY apps/client/backend/InvoiceTrackerApi/InvoiceTrackerApi.csproj apps/client/backend/InvoiceTrackerApi/
RUN dotnet restore apps/client/backend/InvoiceTrackerApi/InvoiceTrackerApi.csproj

# Copy source and publish
COPY shared/Shared.Database/ shared/Shared.Database/
COPY apps/client/backend/InvoiceTrackerApi/ apps/client/backend/InvoiceTrackerApi/
RUN dotnet publish apps/client/backend/InvoiceTrackerApi/InvoiceTrackerApi.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "InvoiceTrackerApi.dll"]
