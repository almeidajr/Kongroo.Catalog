FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src
COPY . .
RUN dotnet restore src/Kongroo.Catalog.Api --locked-mode

FROM restore AS build
RUN dotnet publish src/Kongroo.Catalog.Api \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN groupadd --system --gid 1001 kongroo \
    && useradd --system --uid 1001 --gid kongroo --no-create-home kongroo
WORKDIR /app
COPY --from=build --chown=kongroo:kongroo /app/publish .
USER kongroo
EXPOSE 8080
ENTRYPOINT ["dotnet", "Kongroo.Catalog.Api.dll"]
