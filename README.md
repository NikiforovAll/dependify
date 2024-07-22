# Dependify

[![Build](https://github.com/NikiforovAll/dependify/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/NikiforovAll/dependify/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/dt/Dependify.Cli.svg)](https://nuget.org/packages/Dependify.Cli)
[![contributionswelcome](https://img.shields.io/badge/contributions-welcome-brightgreen.svg?style=flat)](https://github.com/nikiforovall/dependify)
[![Conventional Commits](https://img.shields.io/badge/Conventional%20Commits-1.0.0-yellow.svg)](https://conventionalcommits.org)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/nikiforovall/dependify/blob/main/LICENSE.md)


## Install

```bash
dotnet tool install -g Dependify.Cli
```

```bash
dependify graph --help
```
    
```text
USAGE:
    dependify graph [OPTIONS] <COMMAND>

EXAMPLES:
    dependify graph scan ./path/to/folder --framework net8
    dependify graph show ./path/to/project --framework net8

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    scan <path>    Scans for projects and solutions and retrives their dependencies
    show <path>    Shows the dependencies of a project or solution located in the specified path
```

## Example

```bash
dependify graph scan \
    $dev/keycloak-authorization-services-dotnet/ \
    --framework net8
```

![tui-demo1](./assets/tui-demo1.png)

```bash
dependify graph scan \
    $dev/keycloak-authorization-services-dotnet/ \
    --exclude-sln \
    --framework net8 \
    --format mermaid \
    --output ./graph.md
```

```mermaid
graph LR
    Keycloak.AuthServices.Authentication.csproj:::project
    Keycloak.AuthServices.Templates.csproj:::project
    Blazor.Server.csproj:::project
    GettingStarted.csproj:::project
    AuthorizationGettingStarted.csproj:::project
    AuthorizationAndCleanArchitecture.csproj:::project
    Blazor.Client.csproj:::project
    TestWebApi.csproj:::project
    Keycloak.AuthServices.Authorization.csproj:::project
    Keycloak.AuthServices.Common.csproj:::project
    TestWebApiWithControllers.csproj:::project
    Keycloak.AuthServices.Sdk.csproj:::project
    WebApp.csproj:::project
    Keycloak.AuthServices.Sdk.Tests.csproj:::project
    AuthGettingStarted.csproj:::project
    Keycloak.AuthServices.Common.Tests.csproj:::project
    Keycloak.AuthServices.Authentication.Tests.csproj:::project
    Keycloak.AuthServices.Aspire.Hosting.csproj:::project
    ResourceAuthorization.csproj:::project
    Keycloak.AuthServices.OpenTelemetry.csproj:::project
    Keycloak.AuthServices.Authorization.Tests.csproj:::project
    Keycloak.AuthServices.IntegrationTests.csproj:::project
    Keycloak.AuthServices.Sdk.Kiota.csproj:::project
    Blazor.Shared.csproj:::project
    Keycloak.AuthServices.IntegrationTests.csproj --> Keycloak.AuthServices.Sdk.csproj
    Keycloak.AuthServices.Common.Tests.csproj --> Keycloak.AuthServices.Common.csproj
    AuthGettingStarted.csproj --> Keycloak.AuthServices.Authorization.csproj
    ResourceAuthorization.csproj --> Keycloak.AuthServices.OpenTelemetry.csproj
    Keycloak.AuthServices.Authorization.Tests.csproj --> Keycloak.AuthServices.Authentication.csproj
    AuthGettingStarted.csproj --> Keycloak.AuthServices.Sdk.csproj
    Keycloak.AuthServices.Authorization.csproj --> Keycloak.AuthServices.Common.csproj
    WebApp.csproj --> Keycloak.AuthServices.Authorization.csproj
    GettingStarted.csproj --> Keycloak.AuthServices.Authentication.csproj
    ResourceAuthorization.csproj --> Keycloak.AuthServices.Sdk.Kiota.csproj
    WebApp.csproj --> Keycloak.AuthServices.Common.csproj
    Keycloak.AuthServices.Sdk.Kiota.csproj --> Keycloak.AuthServices.Common.csproj
    AuthorizationGettingStarted.csproj --> Keycloak.AuthServices.Authentication.csproj
    Keycloak.AuthServices.Authorization.Tests.csproj --> Keycloak.AuthServices.Authorization.csproj
    ResourceAuthorization.csproj --> Keycloak.AuthServices.Authentication.csproj
    Keycloak.AuthServices.Authorization.Tests.csproj --> Keycloak.AuthServices.Common.csproj
    Keycloak.AuthServices.IntegrationTests.csproj --> Keycloak.AuthServices.Sdk.Kiota.csproj
    Keycloak.AuthServices.Sdk.csproj --> Keycloak.AuthServices.Common.csproj
    Blazor.Client.csproj --> Blazor.Shared.csproj
    Blazor.Server.csproj --> Blazor.Shared.csproj
    Keycloak.AuthServices.Authentication.Tests.csproj --> Keycloak.AuthServices.Authentication.csproj
    Blazor.Server.csproj --> Keycloak.AuthServices.Authentication.csproj
    AuthorizationAndCleanArchitecture.csproj --> Keycloak.AuthServices.Authentication.csproj
    AuthorizationGettingStarted.csproj --> Keycloak.AuthServices.Authorization.csproj
    Keycloak.AuthServices.Authentication.csproj --> Keycloak.AuthServices.Common.csproj
    Keycloak.AuthServices.Sdk.Tests.csproj --> Keycloak.AuthServices.Sdk.csproj
    ResourceAuthorization.csproj --> Keycloak.AuthServices.Authorization.csproj
    Keycloak.AuthServices.IntegrationTests.csproj --> Keycloak.AuthServices.Authentication.csproj
    AuthorizationGettingStarted.csproj --> Keycloak.AuthServices.Sdk.csproj
    ResourceAuthorization.csproj --> Keycloak.AuthServices.Common.csproj
    ResourceAuthorization.csproj --> Keycloak.AuthServices.Sdk.csproj
    Blazor.Server.csproj --> Blazor.Client.csproj
    Blazor.Server.csproj --> Keycloak.AuthServices.Authorization.csproj
    TestWebApi.csproj --> Keycloak.AuthServices.Authorization.csproj
    AuthGettingStarted.csproj --> Keycloak.AuthServices.Authentication.csproj
    AuthorizationAndCleanArchitecture.csproj --> Keycloak.AuthServices.Authorization.csproj
    Keycloak.AuthServices.Authentication.Tests.csproj --> Keycloak.AuthServices.Common.csproj
    Keycloak.AuthServices.IntegrationTests.csproj --> TestWebApi.csproj
    TestWebApiWithControllers.csproj --> Keycloak.AuthServices.Authorization.csproj
    Keycloak.AuthServices.IntegrationTests.csproj --> Keycloak.AuthServices.Authorization.csproj
    AuthorizationAndCleanArchitecture.csproj --> Keycloak.AuthServices.Sdk.csproj
    WebApp.csproj --> Keycloak.AuthServices.Authentication.csproj
    Keycloak.AuthServices.IntegrationTests.csproj --> Keycloak.AuthServices.Common.csproj
    Keycloak.AuthServices.IntegrationTests.csproj --> TestWebApiWithControllers.csproj
    classDef project fill:#74200154;
    classDef package fill:#22aaee;

```

## Build and Development

`dotnet cake --target build`

`dotnet cake --target test`

`dotnet cake --target pack`
