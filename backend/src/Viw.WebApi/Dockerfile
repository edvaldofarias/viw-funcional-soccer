FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src/backend
COPY backend/ ./
RUN dotnet publish src/Viw.WebApi/Viw.WebApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Viw.WebApi.dll"]
