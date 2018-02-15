FROM microsoft/dotnet:runtime

WORKDIR /caas
COPY out .
ENTRYPOINT ["dotnet", "Caas.Web.dll"]