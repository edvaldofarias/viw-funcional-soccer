FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /src
COPY backend/src/Viw.Contracts/ backend/src/Viw.Contracts/
COPY backend/Directory.Build.props backend/Directory.Build.props
COPY frontend/mvc/ frontend/mvc/
WORKDIR /src/frontend/mvc
ENTRYPOINT ["dotnet", "test", "Viw.Frontend.slnx", "--configuration", "Release"]
