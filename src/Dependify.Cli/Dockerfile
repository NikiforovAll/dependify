FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY *.sln .
COPY Directory.Build.props Directory.Build.props
COPY src/Directory.Build.props src/Directory.Build.props
COPY src/Directory.Packages.props src/Directory.Packages.props
COPY tests/Directory.Build.props tests/Directory.Build.props
COPY tests/Directory.Packages.props tests/Directory.Packages.props

COPY src/Dependify.Cli/Dependify.Cli.csproj src/Dependify.Cli/
COPY src/Dependify.Core/Dependify.Core.csproj src/Dependify.Core/
COPY src/Web/Web.csproj src/Web/
COPY tests/Dependify.Core.Tests/Dependify.Core.Tests.csproj tests/Dependify.Core.Tests/

RUN dotnet restore

COPY src/. ./src/
WORKDIR /source/src/Dependify.Cli
RUN ls -la
RUN dotnet publish --configuration Release -o /app

WORKDIR /app

ENTRYPOINT ["dotnet", "Dependify.Cli.dll"]
