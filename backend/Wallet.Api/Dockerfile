FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Wallet.Api.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /out --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out .
RUN mkdir -p /data && chown app:app /data
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ConnectionStrings__Wallet="Data Source=/data/wallet.db;Default Timeout=10"
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Wallet.Api.dll"]
