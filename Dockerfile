FROM node:22-alpine AS frontend
WORKDIR /web
COPY frontend/package*.json .
RUN npm ci
COPY frontend .
RUN npm run build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend
WORKDIR /src
COPY backend/Wallet.Api/Wallet.Api.csproj .
RUN dotnet restore
COPY backend/Wallet.Api .
RUN dotnet publish -c Release -o /out --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=backend /out .
COPY --from=frontend /web/dist ./wwwroot
RUN mkdir -p /data /home/data && chown -R app:app /data /home
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ConnectionStrings__Wallet="Data Source=/data/wallet.db;Default Timeout=10"
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Wallet.Api.dll"]
