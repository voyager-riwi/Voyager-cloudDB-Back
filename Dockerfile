# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia los archivos .csproj de cada proyecto
COPY ["CrudCloudDb.API/CrudCloudDb.API.csproj", "CrudCloudDb.API/"]
COPY ["CrudCloudDb.Application/CrudCloudDb.Application.csproj", "CrudCloudDb.Application/"]
COPY ["CrudCloudDb.Infrastructure/CrudCloudDb.Infrastructure.csproj", "CrudCloudDb.Infrastructure/"]
COPY ["CrudCloudDb.Core/CrudCloudDb.Core.csproj", "CrudCloudDb.Core/"]

# Restaura dependencias
RUN dotnet restore "CrudCloudDb.API/CrudCloudDb.API.csproj"

# Copia todo el código
COPY . .

# Compila y publica
WORKDIR "/src/CrudCloudDb.API"
RUN dotnet publish "CrudCloudDb.API.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Expone el puerto
EXPOSE 8081

# Punto de entrada
ENTRYPOINT ["dotnet", "CrudCloudDb.API.dll"]
