FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY certs/ca-bundle.crt /usr/local/share/ca-certificates/corporate.crt
RUN update-ca-certificates

COPY shared/Shared.Core/Shared.Core.csproj shared/Shared.Core/
COPY shared/Shared.Database/Shared.Database.csproj shared/Shared.Database/
COPY apps/client/backend/PdfGeneratorService/PdfGeneratorService.csproj apps/client/backend/PdfGeneratorService/
RUN dotnet restore apps/client/backend/PdfGeneratorService/PdfGeneratorService.csproj

COPY shared/Shared.Core/ shared/Shared.Core/
COPY shared/Shared.Database/ shared/Shared.Database/
COPY apps/client/backend/PdfGeneratorService/ apps/client/backend/PdfGeneratorService/
RUN dotnet publish apps/client/backend/PdfGeneratorService/PdfGeneratorService.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install Chromium and its dependencies for PuppeteerSharp PDF generation
RUN apt-get update && apt-get install -y \
    chromium \
    fonts-liberation \
    fonts-noto-cjk \
    libgbm1 \
    libxss1 \
    --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*

# Point PuppeteerSharp at the system Chromium so it skips the runtime download
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium

COPY --from=build /app/publish .
# Copy HTML templates (not included in dotnet publish by default)
COPY --from=build /src/apps/client/backend/PdfGeneratorService/Templates/ ./Templates/

EXPOSE 5001
ENTRYPOINT ["dotnet", "PdfGeneratorService.dll"]
