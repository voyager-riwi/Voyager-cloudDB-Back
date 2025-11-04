# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["CrudCloudDb.API/CrudCloudDb.API.csproj", "CrudCloudDb.API/"]
COPY ["CrudCloudDb.Application/CrudCloudDb.Application.csproj", "CrudCloudDb.Application/"]
COPY ["CrudCloudDb.Infrastructure/CrudCloudDb.Infrastructure.csproj", "CrudCloudDb.Infrastructure/"]
COPY ["CrudCloudDb.Core/CrudCloudDb.Core.csproj", "CrudCloudDb.Core/"]

RUN dotnet restore "CrudCloudDb.API/CrudCloudDb.API.csproj"

COPY . .

WORKDIR "/src/CrudCloudDb.API"
RUN dotnet publish "CrudCloudDb.API.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# ⭐ CRUCIAL: Define el puerto aquí
ENV ASPNETCORE_URLS=http://+:5191
EXPOSE 5191

ENTRYPOINT ["dotnet", "CrudCloudDb.API.dll"]