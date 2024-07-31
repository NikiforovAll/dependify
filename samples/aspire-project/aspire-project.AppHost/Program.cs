var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.aspire_project_ApiService>("apiservice");

builder.AddProject<Projects.aspire_project_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.AddDependify("dependify1", port: 10000).WithDockerfile("..", "./aspire-project.AppHost/dependify.dockerfile");

builder.AddDependify("dependify2", port: 10001).ServeFrom("../../aspire-project/");

builder.Build().Run();
