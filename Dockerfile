
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /source
COPY . .
RUN dotnet restore src/NetCoreToolService
RUN dotnet build src/NetCoreToolService --configuration Release --no-restore
RUN dotnet publish src/NetCoreToolService --output /srv --no-build

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine
ARG templates_version=1.3.0
ARG TEMPLATE_CHECKOUT_TARGET
WORKDIR /srv
COPY --from=build /srv .
COPY install-template.sh /srv/install-template.sh
RUN chmod +x /srv/install-template.sh
RUN /srv/install-template.sh
ENV DOTNET_URLS=http://0.0.0.0:80
ENTRYPOINT ["dotnet", "Steeltoe.NetCoreToolService.dll"]
