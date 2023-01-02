using Microsoft.Extensions.Logging;
using Grpc.Net.Client.Web;
using Grpc.Net.Client;
using Radzen;
using static lumine8_GrpcService.MainProto;
using lumine8.Client.Identity;
using lumine8;
using AccountManager = lumine8.Client.Identity.AccountManager;
using lumine8.Client.Pages;

namespace lumine8_App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

        builder.Services.AddScoped<DialogService>();
        builder.Services.AddScoped<NotificationService>();
        builder.Services.AddScoped<TooltipService>();
        builder.Services.AddScoped<ContextMenuService>();

        builder.Services.AddScoped<Mobile>();
        builder.Services.AddScoped<EnumToString>();

        builder.Services.AddSingleton<AccountManager>();
        builder.Services.AddSingleton<AuthenticationService>();

        builder.Services.AddSingleton<SingletonVariables>();

        var sv = new SingletonVariables();

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(sv.uri) });
        builder.Services.AddSingleton(services =>
        {
            var httpClient = new HttpClient(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
            var channel = GrpcChannel.ForAddress("https://lumine8.com", new GrpcChannelOptions { HttpClient = httpClient });

            return new MainProtoClient(channel);
        });

		return builder.Build();
	}
}
