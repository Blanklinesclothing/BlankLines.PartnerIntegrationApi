FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY BlankLines.PartnerIntegrationApi.sln .
COPY BlankLines.PartnerIntegrationApi.Domain/BlankLines.PartnerIntegrationApi.Domain.csproj BlankLines.PartnerIntegrationApi.Domain/
COPY BlankLines.PartnerIntegrationApi.Application/BlankLines.PartnerIntegrationApi.Application.csproj BlankLines.PartnerIntegrationApi.Application/
COPY BlankLines.PartnerIntegrationApi.Infrastructure/BlankLines.PartnerIntegrationApi.Infrastructure.csproj BlankLines.PartnerIntegrationApi.Infrastructure/
COPY BlankLines.PartnerIntegrationApi.Api/BlankLines.PartnerIntegrationApi.Api.csproj BlankLines.PartnerIntegrationApi.Api/

RUN dotnet restore

COPY . .

RUN dotnet publish BlankLines.PartnerIntegrationApi.Api/BlankLines.PartnerIntegrationApi.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "BlankLines.PartnerIntegrationApi.Api.dll"]
