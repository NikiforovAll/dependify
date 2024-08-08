// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Ollama;

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;
using OllamaSharp;

internal sealed class OllamaLifecycleHook(
    ResourceLoggerService loggerService,
    ResourceNotificationService notificationService,
    DistributedApplicationExecutionContext context
) : IDistributedApplicationLifecycleHook, IAsyncDisposable
{
    private const string DateFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

    private readonly CancellationTokenSource cancellationTokenSource = new();

    public Task AfterResourcesCreatedAsync(
        DistributedApplicationModel appModel,
        CancellationToken cancellationToken = default
    )
    {
        if (context.IsPublishMode)
        {
            return Task.CompletedTask;
        }

        foreach (var resource in appModel.Resources.OfType<OllamaResource>())
        {
            this.DownloadModel(resource, this.cancellationTokenSource.Token);
        }

        return Task.CompletedTask;
    }

    private void DownloadModel(OllamaResource resource, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(resource.Name))
        {
            return;
        }

        var logger = loggerService.GetLogger(resource);

        _ = Task.Run(
                async () =>
                {
                    try
                    {
                        // get the allocated endpoint connection string
                        var connectionString = await resource
                            .ConnectionStringExpression.GetValueAsync(cancellationToken)
                            .ConfigureAwait(false);

                        var httpClient = new HttpClient
                        {
                            Timeout = TimeSpan.FromMinutes(10),
                            BaseAddress = new Uri(connectionString!)
                        };
                        var ollamaClient = new OllamaApiClient(httpClient);

                        await notificationService
                            .PublishUpdateAsync(
                                resource,
                                state => state with { State = new("Checking models...", KnownResourceStateStyles.Info) }
                            )
                            .ConfigureAwait(false);

                        // retrieve the list of models available in the ollama server
                        var modelsAvailable = await ollamaClient
                            .ListLocalModels(cancellationToken)
                            .ConfigureAwait(false);
                        var availableModelNames = modelsAvailable.Select(m => m.Name) ?? [];

                        // get the list of models to download excluding named ones that are already available
                        var modelsToDownload = resource.Models.Except(availableModelNames);

                        if (!modelsToDownload.Any())
                        {
                            logger.LogInformation(
                                "{TimeStamp}: [{Models}] are already downloaded for resource {ResourceName}",
                                DateTime.UtcNow.ToString(DateFormat, CultureInfo.InvariantCulture),
                                string.Join(", ", resource.Models),
                                resource.Name
                            );
                            return;
                        }

                        logger.LogInformation(
                            "{TimeStamp}: Downloading models [{Models}] for resource {ResourceName}...",
                            DateTime.UtcNow.ToString(DateFormat, CultureInfo.InvariantCulture),
                            string.Join(", ", modelsToDownload),
                            resource.Name
                        );

                        await notificationService
                            .PublishUpdateAsync(
                                resource,
                                state => state with { State = new("Downloading models", KnownResourceStateStyles.Info) }
                            )
                            .ConfigureAwait(false);

                        // download the models in parallel
                        await Parallel
                            .ForEachAsync(
                                modelsToDownload,
                                async (model, ct) =>
                                    await this.PullModel(logger, resource, ollamaClient, model, ct)
                                        .ConfigureAwait(false)
                            )
                            .ConfigureAwait(false);

                        await notificationService
                            .PublishUpdateAsync(
                                resource,
                                state => state with { State = new("Running", KnownResourceStateStyles.Success) }
                            )
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        await notificationService
                            .PublishUpdateAsync(
                                resource,
                                state => state with { State = new(ex.Message, KnownResourceStateStyles.Error) }
                            )
                            .ConfigureAwait(false);
                    }
                },
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    private async Task PullModel(
        ILogger logger,
        OllamaResource resource,
        OllamaApiClient ollamaClient,
        string model,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation(
            "{TimeStamp}: Pulling ollama model {Model}...",
            DateTime.UtcNow.ToString(DateFormat, CultureInfo.InvariantCulture),
            model
        );

        await notificationService
            .PublishUpdateAsync(
                resource,
                state => state with { State = new("Downloading model", KnownResourceStateStyles.Info) }
            )
            .ConfigureAwait(false);

        long percentage = 0;

        await ollamaClient
            .PullModel(
                model,
                async status =>
                {
                    if (status.Total != 0)
                    {
                        var newPercentage = (long)(status.Completed / (double)status.Total * 100);
                        if (newPercentage != percentage)
                        {
                            percentage = newPercentage;

                            var percentageState =
                                percentage == 0 ? "Downloading model" : $"Downloading ({model}) {percentage}%";
                            await notificationService
                                .PublishUpdateAsync(
                                    resource,
                                    state => state with { State = new(percentageState, KnownResourceStateStyles.Info) }
                                )
                                .ConfigureAwait(false);
                        }
                    }
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        logger.LogInformation(
            "{TimeStamp}: Finished pulling ollama model {Model}",
            DateTime.UtcNow.ToString(DateFormat, CultureInfo.InvariantCulture),
            model
        );
    }

    public ValueTask DisposeAsync()
    {
        this.cancellationTokenSource.Cancel();

        return default;
    }
}
