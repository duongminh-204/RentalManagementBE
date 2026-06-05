# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ENV DOTNET_SYSTEM_NET_DISABLEIPV6=1
ENV DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0

WORKDIR /src

COPY ["RentalManagementBE.csproj", "./"]

RUN dotnet restore "RentalManagementBE.csproj" \
    --disable-parallel \
    --verbosity minimal

COPY . .

# appsettings.json không commit Git — dùng file mẫu khi build Docker (Render)
COPY appsettings.example.json ./appsettings.json

RUN dotnet publish "RentalManagementBE.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

RUN mkdir -p /app/wwwroot/uploads/cccd \
             /app/wwwroot/uploads/templates \
             /app/wwwroot/uploads/rooms \
             /app/wwwroot/uploads/vehicles \
             /app/wwwroot/uploads/contracts \
             /app/wwwroot/uploads/avatars \
    && chmod -R 777 /app/wwwroot

COPY --from=build /app/publish .
COPY entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh

EXPOSE 8080

ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["/app/entrypoint.sh"]
