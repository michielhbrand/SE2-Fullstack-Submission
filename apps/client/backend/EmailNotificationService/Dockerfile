FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY certs/ca-bundle.crt /usr/local/share/ca-certificates/corporate.crt
RUN update-ca-certificates

COPY shared/Shared.Database/Shared.Database.csproj shared/Shared.Database/
COPY apps/client/backend/EmailNotificationService/EmailNotificationService.csproj apps/client/backend/EmailNotificationService/
RUN dotnet restore apps/client/backend/EmailNotificationService/EmailNotificationService.csproj

COPY shared/Shared.Database/ shared/Shared.Database/
COPY apps/client/backend/EmailNotificationService/ apps/client/backend/EmailNotificationService/
RUN dotnet publish apps/client/backend/EmailNotificationService/EmailNotificationService.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5003
ENTRYPOINT ["dotnet", "EmailNotificationService.dll"]
