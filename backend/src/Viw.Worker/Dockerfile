FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src/backend
COPY backend/ ./
RUN dotnet publish src/Viw.Worker/Viw.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /data/media && chown -R $APP_UID:$APP_UID /data
USER $APP_UID
ENTRYPOINT ["dotnet", "Viw.Worker.dll"]
