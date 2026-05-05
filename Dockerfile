FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["MetalLink.Api/MetalLink.Api.csproj", "MetalLink.Api/"]
COPY ["MetalLink.Application/MetalLink.Application.csproj", "MetalLink.Application/"]
COPY ["MetalLink.Infrastructure/MetalLink.Infrastructure.csproj", "MetalLink.Infrastructure/"]
COPY ["MetalLink.Domain/MetalLink.Domain.csproj", "MetalLink.Domain/"]
COPY ["MetalLink.Shared/MetalLink.Shared.csproj", "MetalLink.Shared/"]

RUN dotnet restore "MetalLink.Api/MetalLink.Api.csproj"

# Copy everything else
COPY . .
WORKDIR /src/MetalLink.Api

RUN dotnet publish "MetalLink.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

ENV PORT=8080
ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT
ENV ASPNETCORE_ENVIRONMENT=Production
ENV TZ=Africa/Johannesburg

EXPOSE 8080

ENTRYPOINT ["dotnet", "MetalLink.Api.dll"]