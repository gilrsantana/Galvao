# Base image for running the app
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# Build image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/Galvao.Presentation/Galvao.Presentation.csproj", "src/Galvao.Presentation/"]
COPY ["src/Galvao.Infrastructure/Galvao.Infrastructure.csproj", "src/Galvao.Infrastructure/"]
COPY ["src/Galvao.Application/Galvao.Application.csproj", "src/Galvao.Application/"]
COPY ["src/Galvao.Domain/Galvao.Domain.csproj", "src/Galvao.Domain/"]
COPY ["src/Galvao.Shared/Galvao.Shared.csproj", "src/Galvao.Shared/"]

RUN dotnet restore "src/Galvao.Presentation/Galvao.Presentation.csproj"

# Copy remaining code and build
COPY . .
WORKDIR "/src/src/Galvao.Presentation"
RUN dotnet build "Galvao.Presentation.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "Galvao.Presentation.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Galvao.Presentation.dll"]
