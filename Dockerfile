# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Fix network issues
ENV DOTNET_SYSTEM_NET_DISABLEIPV6=1
ENV DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0

WORKDIR /src

# Copy csproj trước để cache restore tốt hơn
COPY ["RentalManagementBE.csproj", "./"]

# Restore packages
RUN dotnet restore "RentalManagementBE.csproj" \
    --disable-parallel \
    --verbosity minimal

# Copy toàn bộ source code
COPY . .

# Publish application
RUN dotnet publish "RentalManagementBE.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

# Tạo thư mục uploads
RUN mkdir -p /app/wwwroot/uploads/cccd \
             /app/wwwroot/uploads/templates \
             /app/wwwroot/uploads/rooms \
             /app/wwwroot/uploads/vehicles \
             /app/wwwroot/uploads/contracts \
             /app/wwwroot/uploads/avatars \
    && chmod -R 777 /app/wwwroot

# Copy published app
COPY --from=build /app/publish .

# Expose API port
EXPOSE 8080

# ASP.NET Core settings
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

# Start app
ENTRYPOINT ["dotnet", "RentalManagementBE.dll"]