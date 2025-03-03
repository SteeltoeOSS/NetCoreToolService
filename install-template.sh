#!/usr/bin/env sh

# dotnet nuget add source https://pkgs.dev.azure.com/dotnet/Steeltoe/_packaging/dev/nuget/v3/index.json -n SteeltoeDev

if [[ -z "$TEMPLATE_CHECKOUT_TARGET" ]] ;then
    dotnet new install Steeltoe.NetCoreTool.Templates::${templates_version} &&\
      dotnet new --list | grep steeltoe-webapi
else
    cd /usr/local/src
    git clone https://github.com/SteeltoeOSS/NetCoreToolTemplates
    git -C NetCoreToolTemplates checkout $TEMPLATE_CHECKOUT_TARGET
    dotnet new install NetCoreToolTemplates/src/Content
fi
