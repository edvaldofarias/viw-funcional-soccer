FROM node:24.12.0-alpine AS admin
WORKDIR /admin
COPY frontend/viw-admin/package*.json ./
RUN npm ci --no-audit --no-fund
COPY frontend/viw-admin/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src/backend
COPY backend/ ./
COPY --from=admin /admin/dist/viw-admin/browser/ src/Viw.WebApi/wwwroot/admin/
RUN dotnet publish src/Viw.WebApi/Viw.WebApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
RUN mkdir -p /data/keys && chown "$APP_UID" /data/keys
USER $APP_UID
ENTRYPOINT ["dotnet", "Viw.WebApi.dll"]
