# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Fix network issues (NU1301 / IPv6 / HTTP instability)
ENV DOTNET_SYSTEM_NET_DISABLEIPV6=1
ENV DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0

WORKDIR /src

# Copy solution + project file trước để tối ưu cache restore
COPY ["Backend.sln", "./"]
COPY ["Backend/Backend.csproj", "Backend/"]

# Restore NuGet packages
RUN dotnet restore "Backend/Backend.csproj" \
    --disable-parallel \
    --verbosity minimal \
    --no-cache

# Copy toàn bộ source code
COPY . .

# Build project
WORKDIR /src/Backend
RUN dotnet build "Backend.csproj" -c Release -o /app/build

# Publish project
RUN dotnet publish "Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false


# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

# Tạo folder upload cần thiết
RUN mkdir -p /app/wwwroot/uploads/cccd \
             /app/wwwroot/uploads/templates \
             /app/wwwroot/uploads/rooms \
             /app/wwwroot/uploads/vehicles \
             /app/wwwroot/uploads/contracts \
             /app/wwwroot/uploads/avatars \
    && chmod -R 777 /app/wwwroot

# Copy published output
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# ASP.NET listen all interfaces
ENV ASPNETCORE_URLS=http://+:8080

# Run app
ENTRYPOINT ["dotnet", "Backend.dll"]