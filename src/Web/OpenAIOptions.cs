namespace Web;

public sealed class OpenAIOptions
{
    [ConfigurationKeyName("MODEL_ID")]
    public string ModelId { get; set; } = string.Empty;

    [ConfigurationKeyName("DEPLOYMENT_NAME")]
    public string DeploymentName { get; set; } = string.Empty;

    [ConfigurationKeyName("ENDPOINT")]
    public string Endpoint { get; set; } = string.Empty;

    [ConfigurationKeyName("API_KEY")]
    public string ApiKey { get; set; } = string.Empty;

    public bool IsAzureOpenAI =>
        !string.IsNullOrWhiteSpace(this.DeploymentName)
        && !string.IsNullOrWhiteSpace(this.Endpoint)
        && !string.IsNullOrWhiteSpace(this.ApiKey);

    public bool IsEnabled => !string.IsNullOrWhiteSpace(this.Endpoint);
}
