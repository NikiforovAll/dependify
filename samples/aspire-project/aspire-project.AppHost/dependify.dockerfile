FROM ghcr.io/nikiforovall/dependify:latest

COPY ./. /workspace/

WORKDIR /app

ENTRYPOINT ["dotnet", "Dependify.Cli.dll"]
