FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY Authentication.slnx ./
COPY src/Core/Domain/Authentication.Domain.csproj src/Core/Domain/
COPY src/Core/Application/Authentication.Application.csproj src/Core/Application/
COPY src/Infrastructure/Authentication.Infrastructure.csproj src/Infrastructure/
COPY src/API/Authentication.API.csproj src/API/
COPY tests/Application.UnitTests/Authentication.Application.UnitTests.csproj tests/Application.UnitTests/

RUN dotnet restore src/API/Authentication.API.csproj

COPY src/ src/

RUN dotnet publish src/API/Authentication.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN groupadd --system --gid 1500 appgroup \
    && useradd --system --uid 1500 --gid appgroup --create-home --home-dir /home/appuser appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080;https://+:8443
EXPOSE 8080 8443

USER appuser

ENTRYPOINT ["dotnet", "Authentication.API.dll"]
