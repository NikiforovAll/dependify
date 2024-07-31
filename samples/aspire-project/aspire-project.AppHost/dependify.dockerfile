FROM ghcr.io/nikiforovall/dependify:main

COPY ./. /workspace/

WORKDIR /app

ENTRYPOINT ["dotnet", "Dependify.Cli.dll"]
