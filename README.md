# Dependify

[![Build](https://github.com/NikiforovAll/dependify/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/NikiforovAll/dependify/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/dt/Dependify.Tool.svg)](https://nuget.org/packages/Dependify.Tool)
[![contributionswelcome](https://img.shields.io/badge/contributions-welcome-brightgreen.svg?style=flat)](https://github.com/nikiforovall/dependify)
[![Conventional Commits](https://img.shields.io/badge/Conventional%20Commits-1.0.0-yellow.svg)](https://conventionalcommits.org)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/nikiforovall/dependify/blob/main/LICENSE.md)


## Install

```bash
dotnet tool install -g Dependify.Tool
```

## Example

```bash
dotnet run --project ./src/Dependify.Cli/ \
    -- graph scan $dev/keycloak-authorization-services-dotnet/ \
    --framework net8 \
    --include-packages true
```

```
┌────────────────────────────────────────────────────────────┬──────────┬──────────────────────────┬────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ Name                                                       │ Type     │ Dependencies (d/a) count │ Path C:/Users/Oleksii_Nikiforov/dev/keycloak-authorization-services-dotnet                         │
├────────────────────────────────────────────────────────────┼──────────┼──────────────────────────┼────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ KeycloakAuthorizationServicesDotNet.sln                    │ Solution │ 24/0                     │ KeycloakAuthorizationServicesDotNet.sln                                                            │
│ AuthGettingStarted.csproj                                  │ Project  │ 5/1                      │ samples/AuthGettingStarted/AuthGettingStarted.csproj                                               │
│ AuthorizationAndCleanArchitecture.csproj                   │ Project  │ 8/1                      │ samples/AuthorizationAndCleanArchitecture/AuthorizationAndCleanArchitecture.csproj                 │
│ AuthorizationGettingStarted.csproj                         │ Project  │ 6/1                      │ samples/AuthorizationGettingStarted/AuthorizationGettingStarted.csproj                             │
│ Blazor.Client.csproj                                       │ Project  │ 5/2                      │ samples/Blazor/Client/Blazor.Client.csproj                                                         │
│ Blazor.Server.csproj                                       │ Project  │ 7/1                      │ samples/Blazor/Server/Blazor.Server.csproj                                                         │
│ Blazor.Shared.csproj                                       │ Project  │ 0/3                      │ samples/Blazor/Shared/Blazor.Shared.csproj                                                         │
│ GettingStarted.csproj                                      │ Project  │ 1/1                      │ samples/GettingStarted/GettingStarted.csproj                                                       │
│ ResourceAuthorization.csproj                               │ Project  │ 13/1                     │ samples/ResourceAuthorization/ResourceAuthorization.csproj                                         │
│ WebApp.csproj                                              │ Project  │ 5/1                      │ samples/WebApp/WebApp.csproj                                                                       │
│ Keycloak.AuthServices.Aspire.Hosting.csproj                │ Project  │ 3/1                      │ src/Keycloak.AuthServices.Aspire.Hosting/Keycloak.AuthServices.Aspire.Hosting.csproj               │
│ Keycloak.AuthServices.Authentication.csproj                │ Project  │ 9/11                     │ src/Keycloak.AuthServices.Authentication/Keycloak.AuthServices.Authentication.csproj               │
│ Keycloak.AuthServices.Authorization.csproj                 │ Project  │ 7/11                     │ src/Keycloak.AuthServices.Authorization/Keycloak.AuthServices.Authorization.csproj                 │
│ Keycloak.AuthServices.Common.csproj                        │ Project  │ 4/11                     │ src/Keycloak.AuthServices.Common/Keycloak.AuthServices.Common.csproj                               │
│ Keycloak.AuthServices.OpenTelemetry.csproj                 │ Project  │ 3/2                      │ src/Keycloak.AuthServices.OpenTelemetry/Keycloak.AuthServices.OpenTelemetry.csproj                 │
│ Keycloak.AuthServices.Sdk.csproj                           │ Project  │ 5/7                      │ src/Keycloak.AuthServices.Sdk/Keycloak.AuthServices.Sdk.csproj                                     │
│ Keycloak.AuthServices.Sdk.Kiota.csproj                     │ Project  │ 12/3                     │ src/Keycloak.AuthServices.Sdk.Kiota/Keycloak.AuthServices.Sdk.Kiota.csproj                         │
│ Keycloak.AuthServices.Templates.csproj                     │ Project  │ 3/1                      │ src/Keycloak.AuthServices.Templates/Keycloak.AuthServices.Templates.csproj                         │
│ Keycloak.AuthServices.Authentication.Tests.csproj          │ Project  │ 8/1                      │ tests/Keycloak.AuthServices.Authentication.Tests/Keycloak.AuthServices.Authentication.Tests.csproj │
│ Keycloak.AuthServices.Authorization.Tests.csproj           │ Project  │ 9/1                      │ tests/Keycloak.AuthServices.Authorization.Tests/Keycloak.AuthServices.Authorization.Tests.csproj   │
│ Keycloak.AuthServices.Common.Tests.csproj                  │ Project  │ 8/1                      │ tests/Keycloak.AuthServices.Common.Tests/Keycloak.AuthServices.Common.Tests.csproj                 │
│ Keycloak.AuthServices.IntegrationTests.csproj              │ Project  │ 20/1                     │ tests/Keycloak.AuthServices.IntegrationTests/Keycloak.AuthServices.IntegrationTests.csproj         │
│ Keycloak.AuthServices.Sdk.Tests.csproj                     │ Project  │ 8/1                      │ tests/Keycloak.AuthServices.Sdk.Tests/Keycloak.AuthServices.Sdk.Tests.csproj                       │
│ TestWebApi.csproj                                          │ Project  │ 7/2                      │ tests/TestWebApi/TestWebApi.csproj                                                                 │
│ TestWebApiWithControllers.csproj                           │ Project  │ 7/2                      │ tests/TestWebApiWithControllers/TestWebApiWithControllers.csproj                                   │
│ System.Diagnostics.DiagnosticSource                        │ Package  │ 0/1                      │ https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource                                 │
│ Microsoft.NET.Test.Sdk                                     │ Package  │ 0/7                      │ https://www.nuget.org/packages/Microsoft.NET.Test.Sdk                                              │
│ Testcontainers                                             │ Package  │ 0/1                      │ https://www.nuget.org/packages/Testcontainers                                                      │
│ coverlet.collector                                         │ Package  │ 0/7                      │ https://www.nuget.org/packages/coverlet.collector                                                  │
│ OpenTelemetry.Exporter.OpenTelemetryProtocol               │ Package  │ 0/1                      │ https://www.nuget.org/packages/OpenTelemetry.Exporter.OpenTelemetryProtocol                        │
│ MediatR                                                    │ Package  │ 0/1                      │ https://www.nuget.org/packages/MediatR                                                             │
│ Meziantou.Extensions.Logging.Xunit                         │ Package  │ 0/1                      │ https://www.nuget.org/packages/Meziantou.Extensions.Logging.Xunit                                  │
│ Microsoft.IdentityModel.Protocols.OpenIdConnect            │ Package  │ 0/1                      │ https://www.nuget.org/packages/Microsoft.IdentityModel.Protocols.OpenIdConnect                     │
│ Microsoft.SourceLink.GitHub                                │ Package  │ 0/8                      │ https://www.nuget.org/packages/Microsoft.SourceLink.GitHub                                         │
│ OpenTelemetry.Instrumentation.AspNetCore                   │ Package  │ 0/1                      │ https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNetCore                            │
│ Swashbuckle.AspNetCore                                     │ Package  │ 0/4                      │ https://www.nuget.org/packages/Swashbuckle.AspNetCore                                              │
│ OpenTelemetry.Extensions.Hosting                           │ Package  │ 0/1                      │ https://www.nuget.org/packages/OpenTelemetry.Extensions.Hosting                                    │
│ RichardSzalay.MockHttp                                     │ Package  │ 0/7                      │ https://www.nuget.org/packages/RichardSzalay.MockHttp                                              │
│ Microsoft.AspNetCore.Components.WebAssembly.DevServer      │ Package  │ 0/1                      │ https://www.nuget.org/packages/Microsoft.AspNetCore.Components.WebAssembly.DevServer               │
│ Microsoft.Extensions.Configuration.Json                    │ Package  │ 0/1                      │ https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Json                             │
│ Serilog.Sinks.SpectreConsole                               │ Package  │ 0/1                      │ https://www.nuget.org/packages/Serilog.Sinks.SpectreConsole                                        │
│ Microsoft.Extensions.Configuration.Binder                  │ Package  │ 0/2                      │ https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Binder                           │
│ Microsoft.Extensions.DependencyInjection.Abstractions      │ Package  │ 0/4                      │ https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions               │
│ xunit.runner.visualstudio                                  │ Package  │ 0/7                      │ https://www.nuget.org/packages/xunit.runner.visualstudio                                           │
│ Microsoft.Extensions.Http.Resilience                       │ Package  │ 0/1                      │ https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience                                │
│ Alba                                                       │ Package  │ 0/1                      │ https://www.nuget.org/packages/Alba                                                                │
│ Aspire.Hosting                                             │ Package  │ 0/1                      │ https://www.nuget.org/packages/Aspire.Hosting                                                      │
│ Microsoft.TemplateEngine.Tasks                             │ Package  │ 0/1                      │ https://www.nuget.org/packages/Microsoft.TemplateEngine.Tasks                                      │
│ Microsoft.AspNetCore.Components.WebAssembly.Server         │ Package  │ 0/1                      │ https://www.nuget.org/packages/Microsoft.AspNetCore.Components.WebAssembly.Server                  │
│ Serilog.AspNetCore                                         │ Package  │ 0/3                      │ https://www.nuget.org/packages/Serilog.AspNetCore                                                  │
│ Microsoft.Extensions.Configuration.Abstractions            │ Package  │ 0/2                      │ https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Abstractions                     │
│ xunit                                                      │ Package  │ 0/7                      │ https://www.nuget.org/packages/xunit                                                               │
│ Serilog.Extensions.Hosting                                 │ Package  │ 0/1                      │ https://www.nuget.org/packages/Serilog.Extensions.Hosting                                          │
│ Microsoft.Kiota.Serialization.Text                         │ Package  │ 0/1                      │ https://www.nuget.org/packages/Microsoft.Kiota.Serialization.Text                                  │
│ Microsoft.Kiota.Serialization.Json                         │ Package  │ 0/1                      │ https://www.nuget.org/packages/Microsoft.Kiota.Serialization.Json                                  │
│ NSwag.AspNetCore                                           │ Package  │ 0/1                      │ https://www.nuget.org/packages/NSwag.AspNetCore                                                    │
│ Microsoft.Kiota.Authentication.Azure                       │ Package  │ 0/1                      │ https://www.nuget.org/packages/Microsoft.Kiota.Authentication.Azure                                │
│ Duende.AccessTokenManagement                               │ Package  │ 0/1                      │ https://www.nuget.org/packages/Duende.AccessTokenManagement                                        │
│ Microsoft.AspNetCore.Authentication.JwtBearer              │ Package  │ 0/1                      │ https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer                       │
│ Duende.AccessTokenManagement                               │ Package  │ 0/1                      │ https://www.nuget.org/packages/Duende.AccessTokenManagement                                        │
│ Microsoft.AspNetCore.Components.WebAssembly.Authentication │ Package  │ 0/1                      │ https://www.nuget.org/packages/Microsoft.AspNetCore.Components.WebAssembly.Authentication          │
│ Microsoft.Extensions.Configuration.Json                    │ Package  │ 0/1                      │ https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Json                             │
│ Testcontainers.Keycloak                                    │ Package  │ 0/1                      │ https://www.nuget.org/packages/Testcontainers.Keycloak                                             │
│ Microsoft.AspNetCore.Components.WebAssembly                │ Package  │ 0/1                      │ https://www.nuget.org/packages/Microsoft.AspNetCore.Components.WebAssembly                         │
│ MinVer                                                     │ Package  │ 0/8                      │ https://www.nuget.org/packages/MinVer                                                              │
│ Microsoft.EntityFrameworkCore.Design                       │ Package  │ 0/1                      │ https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Design                                │
│ OpenTelemetry.Instrumentation.Http                         │ Package  │ 0/1                      │ https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Http                                  │
│ FluentAssertions                                           │ Package  │ 0/7                      │ https://www.nuget.org/packages/FluentAssertions                                                    │
│ OpenTelemetry                                              │ Package  │ 0/1                      │ https://www.nuget.org/packages/OpenTelemetry                                                       │
│ AutoFixture.Xunit2                                         │ Package  │ 0/2                      │ https://www.nuget.org/packages/AutoFixture.Xunit2                                                  │
│ Microsoft.Kiota.Http.HttpClientLibrary                     │ Package  │ 0/1                      │ https://www.nuget.org/packages/Microsoft.Kiota.Http.HttpClientLibrary                              │
│ Microsoft.Extensions.Diagnostics                           │ Package  │ 0/1                      │ https://www.nuget.org/packages/Microsoft.Extensions.Diagnostics                                    │
│ Microsoft.Kiota.Serialization.Multipart                    │ Package  │ 0/1                      │ https://www.nuget.org/packages/Microsoft.Kiota.Serialization.Multipart                             │
│ Npgsql.EntityFrameworkCore.PostgreSQL                      │ Package  │ 0/1                      │ https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL                               │
│ Microsoft.Kiota.Serialization.Form                         │ Package  │ 0/1                      │ https://www.nuget.org/packages/Microsoft.Kiota.Serialization.Form                                  │
│ Microsoft.Extensions.Http                                  │ Package  │ 0/4                      │ https://www.nuget.org/packages/Microsoft.Extensions.Http                                           │
│ Microsoft.Extensions.Http.Resilience                       │ Package  │ 0/1                      │ https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience                                │
│ Microsoft.AspNetCore.Authentication.OpenIdConnect          │ Package  │ 0/2                      │ https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.OpenIdConnect                   │
│ Microsoft.Kiota.Abstractions                               │ Package  │ 0/1                      │ https://www.nuget.org/packages/Microsoft.Kiota.Abstractions                                        │
└────────────────────────────────────────────────────────────┴──────────┴──────────────────────────┴────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

## Build and Development

`dotnet cake --target build`

`dotnet cake --target test`

`dotnet cake --target pack`
