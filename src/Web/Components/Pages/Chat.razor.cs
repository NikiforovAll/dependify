namespace Web.Components.Pages;

using Dependify.Core.Graph;
using Dependify.Core.Serializers;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.SemanticKernel;
using MudBlazor;

public partial class Chat
{
    [Parameter]
    public string? CurrentMessage { get; set; }

    private readonly List<ChatMessage> messages = [];

    private List<SolutionReferenceNode> solutionNodes = [];

    private bool IsLoadingResponse { get; set; }

    private string? selectedSolution;

    protected override Task OnInitializedAsync()
    {
        this.LoadSolutions();

        this.selectedSolution = this.SolutionRegistry.Solutions.FirstOrDefault()?.Id;

        this.messages.Add(
            new ChatMessage
            {
                Message = "Welcome to Dependify, a chatbot that helps you manage your dependencies.",
                CreatedDate = DateTime.Now,
                Role = ChatRole.Assistant
            }
        );

        return Task.CompletedTask;
    }

    [JSInvokable]
    public async Task SubmitMessageAsync(string? input)
    {
        if (!string.IsNullOrWhiteSpace(this.CurrentMessage) || !string.IsNullOrWhiteSpace(input))
        {
            var message = string.IsNullOrWhiteSpace(input) ? this.CurrentMessage : input;

            this.messages.Add(
                new ChatMessage
                {
                    Message = message,
                    CreatedDate = DateTime.Now,
                    Role = ChatRole.User
                }
            );

            if (!this.OpenAIOptions.Value.IsEnabled)
            {
                this.PromptUserToRestartWithAISettings();
            }
            else if (!string.IsNullOrWhiteSpace(message))
            {
                if (!this.SolutionRegistry.IsLoaded)
                {
                    this.Snackbar.Add($"Analyzing - {this.selectedSolution}, please wait...", Severity.Normal);

                    return;
                }

                var diagramContent = this.CalculateCurrentContext();
                var prompt = $"""
                    You are .NET expert (aka Dependify expert) that will assist a user to answer his question regarding project and it's dependencies.

                    For example:
                    A.csproj --> B.csproj

                    Means that: A.csproj "uses" or "depends on" B.csproj

                    For example:
                    A.csproj --> PackageB

                    Means that: A.csproj "uses" or "depends on" PackageB

                    Constraints:
                    - You can only use the information from the diagram.
                    - Instead of mentioning the diagram in your answer, use the "source"
                    - Use the diagram as source of knowledge.
                    - When user asks for a project or a package, find the best match use it even if it is misspelled or abbreviated
                    - When referring to a project or package, use full name of the project as it is in the diagram
                    - Short answers are preferred
                    - Provide exhaustive answers, for example if multiple projects or packages match the user's question, provide all of them

                    Task:
                    Give a answer to the question: "{message}"
                    ---
                    Diagram: {diagramContent}
                    """;

                var resultTask = this.ServiceProvider.GetRequiredService<Kernel>().InvokePromptAsync(prompt);

                resultTask.ContinueWith(
                    async result =>
                    {
                        var functionResult = await result;
                        this.messages.Add(
                            new ChatMessage
                            {
                                Message = functionResult.ToString(),
                                CreatedDate = DateTime.Now,
                                Role = ChatRole.Assistant
                            }
                        );

                        this.IsLoadingResponse = false;

                        await this.InvokeAsync(this.StateHasChanged);
                    },
                    TaskScheduler.FromCurrentSynchronizationContext()
                );

                var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

                this.IsLoadingResponse = true;

                Task.WhenAny(resultTask, delayTask)
                    .ContinueWith(
                        async task =>
                        {
                            if (task.Result == delayTask)
                            {
                                this.messages.Add(
                                    new ChatMessage
                                    {
                                        Message = "It will take some time to answer your question, please wait...⏰",
                                        CreatedDate = DateTime.Now,
                                        Role = ChatRole.DisplayOnly
                                    }
                                );
                            }

                            await this.InvokeAsync(this.StateHasChanged);
                        },
                        TaskScheduler.FromCurrentSynchronizationContext()
                    );
            }

            this.CurrentMessage = string.Empty;
            await this.InvokeAsync(this.StateHasChanged);
        }
    }

    private void PromptUserToRestartWithAISettings()
    {
        this.messages.Add(
            new ChatMessage
            {
                Message = "It looks like you haven't configured Dependify to use AI capabilities.",
                CreatedDate = DateTime.Now,
                Role = ChatRole.DisplayOnly
            }
        );

        this.messages.Add(
            new ChatMessage
            {
                Message = "export DEPENDIFY__AI__ENDPOINT=\"https:/<endpoint>.openai.azure.com\"",
                CreatedDate = DateTime.Now,
                Role = ChatRole.DisplayOnly
            }
        );

        this.messages.Add(
            new ChatMessage
            {
                Message = "export DEPENDIFY__AI__DEPLOYMENT_NAME=\"<deployment-name>\"",
                CreatedDate = DateTime.Now,
                Role = ChatRole.DisplayOnly
            }
        );

        this.messages.Add(
            new ChatMessage
            {
                Message = "export DEPENDIFY__AI__API_KEY=\"<api-key>\"",
                CreatedDate = DateTime.Now,
                Role = ChatRole.DisplayOnly
            }
        );

        this.messages.Add(
            new ChatMessage
            {
                Message =
                    "Or by setting --endpoint, --deployment-name and --api-key command line arguments.",
                CreatedDate = DateTime.Now,
                Role = ChatRole.DisplayOnly
            }
        );
    }

    private async Task ShowDiagramModal()
    {
        if (!this.SolutionRegistry.IsLoaded)
        {
            this.Snackbar.Add($"Analyzing - {this.selectedSolution}, please wait...", Severity.Normal);

            return;
        }
        var diagramContent = this.CalculateCurrentContext();

        var parameters = new DialogParameters { ["DiagramContent"] = diagramContent };
        var options = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.ExtraExtraLarge,
            FullWidth = true,
            FullScreen = false,
            CloseOnEscapeKey = true,
        };

        var dialog = await this.DialogService.ShowAsync<DiagramModal>(this.selectedSolution, parameters, options);

        await dialog.Result;
    }

    private string CalculateCurrentContext()
    {
        var solution = this.solutionNodes.Find(n => n.Id == this.selectedSolution);

        if (solution is not null && this.SolutionRegistry.GetGraph(solution) is var graph && graph is not null)
        {
            var content = MermaidSerializer.ToString(graph);

            return content;
        }

        return string.Empty;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await this.JSRuntime.InvokeAsync<string>("ScrollToBottom", "chatContainer");

        if (firstRender)
        {
            await this.JSRuntime.InvokeVoidAsync("bindCtrlEnter", DotNetObjectReference.Create(this));
        }
    }

    private void LoadSolutions()
    {
        this.solutionNodes = [.. this.SolutionRegistry.Solutions];
    }

    private void ClearChat()
    {
        this.messages.Clear();
    }

    private string ContextInformation =>
        this.OpenAIOptions.Value.IsEnabled
            ? $"Model: {this.OpenAIOptions.Value.DeploymentName ?? this.OpenAIOptions.Value.ModelId}, Endpoint: {this.OpenAIOptions.Value.Endpoint}"
            : "Model: None, Endpoint: None";
}

public class ChatMessage
{
    public long Id { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedDate { get; set; }
    public ChatRole Role { get; set; }
}

public enum ChatRole
{
    User,
    Assistant,
    DisplayOnly
}
