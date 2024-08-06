namespace Web;

using MudBlazor;
using MudBlazor.Services;
using Web.Components;

public static class Program
{
    public static Task Main() => Run();

    public static async Task Run(
        WebApplicationOptions? appOptions = default,
        Action<WebApplicationBuilder>? webBuilder = default,
        Action<WebApplication>? webApp = default
    )
    {
        appOptions ??= new();

        var builder = WebApplication.CreateBuilder(appOptions);

        builder.Services.AddMudServices(c =>
        {
            c.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
        });

        // Add services to the container.
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        webBuilder?.Invoke(builder);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

        webApp?.Invoke(app);

        await app.RunAsync();
    }
}
