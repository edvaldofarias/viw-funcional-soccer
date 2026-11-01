FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /src/backend
COPY backend/ ./
ENTRYPOINT ["dotnet", "test", "Viw.FuncionalSoccer.slnx", "--configuration", "Release"]
