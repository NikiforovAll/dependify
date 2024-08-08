# AppHost

```csharp
builder.AddDependify("dependify2", port: 10001).ServeFrom("../../aspire-project/");
```

Control what is gets copied to workspace with dockerfile

```csharp
builder.AddDependify("dependify1", port: 10000).WithDockerfile("..", "./aspire-project.AppHost/dependify.dockerfile");
```
