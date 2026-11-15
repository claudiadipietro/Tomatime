FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Tomatime.Api/Tomatime.Api.csproj src/Tomatime.Api/
RUN dotnet restore src/Tomatime.Api/Tomatime.Api.csproj
COPY src/Tomatime.Api/ src/Tomatime.Api/
RUN dotnet publish src/Tomatime.Api/Tomatime.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Tomatime.Api.dll"]
