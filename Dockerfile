
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /source
COPY . .
RUN dotnet restore /p:Configuration=Release
RUN dotnet build --configuration Release --no-restore /p:TreatWarningsAsErrors=true
RUN dotnet test --configuration Release --no-build
RUN dotnet publish src/NetCoreToolService --output /srv --no-build

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine
ARG templates_version=1.*-*
ARG TEMPLATE_CHECKOUT_TARGET
WORKDIR /srv
COPY --from=build /srv .
COPY install-template.sh /srv/install-template.sh
RUN chmod +x /srv/install-template.sh
RUN /srv/install-template.sh
ENV DOTNET_URLS=http://0.0.0.0:8080
ENTRYPOINT ["dotnet", "Steeltoe.NetCoreToolService.dll"]
