# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/NWSDB.API/NWSDB.API.csproj src/NWSDB.API/
RUN dotnet restore src/NWSDB.API/NWSDB.API.csproj
COPY src/ src/
RUN dotnet publish src/NWSDB.API/NWSDB.API.csproj -c Release -o /app/publish --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "NWSDB.API.dll"]
