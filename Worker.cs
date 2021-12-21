using DBotDDRefuge.Common;
using DBotDDRefuge.Common.Audio;
using DBotDDRefuge.Common.Handlers;
using DBotDDRefuge.Handlers;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Victoria;

namespace DBotDDRefuge;

/// <summary>
/// Worker
/// <para>參考：https://www.gss.com.tw/blog/net-core-worker-service </para>
/// </summary>
public class Worker : BackgroundService
{
	private readonly IConfigurationRoot _configurationRoot;
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<Worker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DiscordShardedClient _shardedClient;
	private readonly InteractionService _interactionService;
	private readonly LavaNode<XLavaPlayer> _lavaNode;

	public Worker(
		IConfigurationRoot configurationRoot,
		IServiceProvider serviceProvider,
		ILogger<Worker> logger,
        IHttpClientFactory httpClientFactory,
        DiscordShardedClient shardedClient, 
		InteractionService interactionService,
		LavaNode<XLavaPlayer> lavaNode)
    {
		_configurationRoot = configurationRoot;
		_serviceProvider = serviceProvider;
		_logger = logger;
        _httpClientFactory = httpClientFactory;
        _shardedClient = shardedClient;
		_interactionService = interactionService;
		_lavaNode = lavaNode;
	}

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!stoppingToken.IsCancellationRequested)
        {
			await RunAsync();
        }
	}

	private async Task RunAsync()
	{
		string rawBotName = Assembly.GetEntryAssembly()?.GetName().Name ?? "機器人";
		string rawToken = _configurationRoot["discord:token"];

		bool enableLavalink = bool.TryParse(_configurationRoot["dataSource:lavalink:enable"], out bool tempBool1) && tempBool1;

		if (!string.IsNullOrEmpty(rawToken))
        {
			_shardedClient.Log += Log;
			_shardedClient.ShardReady += async (socketClient) =>
			{
				await _interactionService.RegisterCommandsGloballyAsync(true);

				_logger.LogInformation("{BotName} - Shard 編號 {ShardID}，已連接且已準備就緒！",
					rawBotName,
					socketClient.ShardId);

				if (enableLavalink)
                {
					try
					{
						if (!_lavaNode.IsConnected)
						{
							await _lavaNode.ConnectAsync();
						}
					}
					catch (Exception ex)
					{
						_logger.LogError("連接 Lavalink 失敗，錯誤訊息：{Exception}", ex);
					}
				}
				else
                {
					_logger.LogInformation("未啟用 Lavalink，音樂撥放相關的斜線命令將無法使用。");
				}
			};
            _shardedClient.SelectMenuExecuted += SelectMenuExecuted;

			_interactionService.Log += Log;

			await _serviceProvider.GetRequiredService<CommandHandler>().InitializeAsync();

			await _shardedClient.LoginAsync(TokenType.Bot, rawToken);
			await _shardedClient.StartAsync();

			await Task.Delay(Timeout.Infinite);
		}
		else
        {
			// 強制讓程式中斷執行。
			throw new Exception("Token 無效，請在 appsettings.json 檔案內設定 \"token\" 的值。");
		}
	}

    private Task Log(LogMessage arg)
	{
		CustomFunction.WriteLog(_logger, arg);

		return Task.CompletedTask;
	}

	private async Task SelectMenuExecuted(SocketMessageComponent arg)
	{
		try
		{
			string rawCustomID = arg.Data.CustomId;

			if (!string.IsNullOrEmpty(rawCustomID))
			{
				switch (rawCustomID)
				{
					case "menu_calendar":
						string? rawQueryType = arg.Data.Values.FirstOrDefault();

						if (!string.IsNullOrEmpty(rawQueryType))
                        {
							CustomInteractionHandler.GetICalendarEventAsync(
								_httpClientFactory,
								_logger,
								arg,
								rawQueryType);
						}
						else
                        {
							_logger.LogError("發生錯誤，rawQueryType 是 Null 或空白。");

							await arg.UpdateAsync(n =>
							{
								n.Content = $"{arg.User.Mention}" +
									$"{Environment.NewLine}{Environment.NewLine}發生錯誤，您選擇的項目無效。";
								n.Components = null;
							});
						}
						break;
					case "menu_activity":
						string? rawActionType = arg.Data.Values.FirstOrDefault();

						if (!string.IsNullOrEmpty(rawActionType))
						{
							CustomInteractionHandler.GetActivityEventAsync(
								_httpClientFactory,
								_logger,
								_shardedClient,
								arg,
								rawActionType);
						}
						else
						{
							_logger.LogError("發生錯誤，rawActivityAction 是 Null 或空白。");

							await arg.UpdateAsync(n =>
							{
								n.Content = $"{arg.User.Mention}" +
									$"{Environment.NewLine}{Environment.NewLine}發生錯誤，您選擇的項目無效。";
								n.Components = null;
							});
						}
						break;
					default:
						break;
				}
			}
			else
			{
				_logger.LogError("發生錯誤，從 SocketMessageComponent 取得的 CustomID 是 Null 或空白。");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError("發生錯誤，錯誤訊息：{Exception}", ex);
		}
	}
}