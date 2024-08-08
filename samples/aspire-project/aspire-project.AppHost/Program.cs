var builder = DistributedApplication.CreateBuilder(args);

var useLocalModelParam = builder.AddParameter("use-local-model");
var endpointParam = builder.AddParameter("endpoint");
var deploymentNameParam = builder.AddParameter("deployment-name");
var apiKeyParam = builder.AddParameter("api-key", secret: true);

var apiService = builder.AddProject<Projects.aspire_project_ApiService>("apiservice");

builder.AddProject<Projects.aspire_project_Web>("webfrontend").WithExternalHttpEndpoints().WithReference(apiService);

var dependify = builder.AddDependify().ServeFrom("../../../");

if (useLocalModelParam.Resource.Value.ToString().Equals("true", StringComparison.OrdinalIgnoreCase))
{
    var modelName = "phi3:mini";
    var ollama = builder.AddOllama("ollama").WithDataVolume().AddModel(modelName).WithOpenWebUI();

    dependify.WithOpenAI(ollama, modelName);
}
else
{
    // Configure the AppHost with the following command:
    // dotnet user-secrets set "Parameters:api-key" "<api-key>"
    // dotnet user-secrets set "Parameters:deployment-name" "gpt-4o-mini"
    // dotnet user-secrets set "Parameters:endpoint" "<endpoint>"

    dependify.WithAzureOpenAI(
        endpointParam.Resource.Value.ToString(),
        deploymentNameParam.Resource.Value.ToString(),
        apiKeyParam.Resource.Value.ToString()
    );
}

builder.Build().Run();
