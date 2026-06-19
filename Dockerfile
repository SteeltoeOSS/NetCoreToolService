FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /source
COPY . .
RUN dotnet restore /p:Configuration=Release /p:NuGetAudit=false
RUN dotnet build --no-restore --configuration Release
RUN dotnet test --no-build --configuration Release
RUN dotnet publish --no-build src/NetCoreToolService --output /srv

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine
ARG templates_version=1.*-*
ARG TEMPLATE_CHECKOUT_TARGET
WORKDIR /srv
COPY --from=build /srv .
COPY install-template.sh /srv/install-template.sh
RUN chmod +x /srv/install-template.sh
RUN /srv/install-template.sh
ENV DOTNET_ENVIRONMENT=Docker
ENV HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "Steeltoe.NetCoreToolService.dll"]
