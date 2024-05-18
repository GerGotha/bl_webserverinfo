using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ListedServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using TaleWorlds.MountAndBlade.DedicatedCustomServer.WebPanel;
using WebServerInfo.Helper;
using WebServerInfo.Api.Controller;

namespace WebServerInfo;

public class WebServerInfoSubModule : MBSubModuleBase
{
   
    protected override void OnSubModuleLoad()
    {
        Debug.Print("Web Stats API by Gotha");
    }

    public override void OnGameInitializationFinished(Game game)
    {
        base.OnGameInitializationFinished(game);
        InitialListedGameServerState.OnActivated += OnActivate;
    }

    private async void OnActivate()
    {
        IWebHost? host = (IWebHost?)ReflectionHelper.GetField(DedicatedCustomServerWebPanelSubModule.Instance, "_host");
        if (host != null)
        {
            Debug.Print("Stopping webservice...");
            await host.StopAsync();

            IWebHostBuilder hostBuilder = WebHost.CreateDefaultBuilder().ConfigureLogging(delegate (ILoggingBuilder logging)
            {
                logging.ClearProviders();
            }).UseStartup<Startup>();

            // Add custom controller for server informations
            hostBuilder.ConfigureServices(services =>
            {
                services.AddMvc().AddApplicationPart(typeof(ServerDetailsController).Assembly).AddSessionStateTempDataProvider()
                    .AddNewtonsoftJson();
            });

            host = hostBuilder.UseUrls(new[] { @$"http://*:{DedicatedCustomServerWebPanelSubModule.Instance.Port}" }).Build();

            Debug.Print("Restarting webservice...");
            await host.StartAsync();
            Debug.Print("Started webservice!");
        }
    }
}
