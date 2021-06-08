FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build
WORKDIR /source

COPY . .
RUN dotnet restore
RUN dotnet publish -c release -o /srv --no-restore

FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine
WORKDIR /srv
COPY --from=build /srv .
ENTRYPOINT ["dotnet", "Steeltoe.NetCoreToolService.dll", "--urls=http://0.0.0.0:80"]
