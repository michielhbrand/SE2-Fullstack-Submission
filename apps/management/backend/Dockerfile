FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY certs/ca-bundle.crt /usr/local/share/ca-certificates/corporate.crt
RUN update-ca-certificates

COPY shared/Shared.Database/Shared.Database.csproj shared/Shared.Database/
COPY apps/management/backend/ManagementApi.csproj apps/management/backend/
RUN dotnet restore apps/management/backend/ManagementApi.csproj

COPY shared/Shared.Database/ shared/Shared.Database/
COPY apps/management/backend/ apps/management/backend/
RUN dotnet publish apps/management/backend/ManagementApi.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5002
ENTRYPOINT ["dotnet", "ManagementApi.dll"]
