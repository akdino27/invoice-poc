# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["invoice-v1.csproj", "./"]
RUN dotnet restore "invoice-v1.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "invoice-v1.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "invoice-v1.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create directory for secrets
RUN mkdir -p /app/secrets

# Expose ports
EXPOSE 80
EXPOSE 443

# Copy published files
COPY --from=publish /app/publish .

# Set environment
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "invoice-v1.dll"]
