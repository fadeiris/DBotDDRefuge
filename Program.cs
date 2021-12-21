using DBotDDRefuge;
using DBotDDRefuge.Common.Audio;
using DBotDDRefuge.Handlers;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Victoria;

IConfigurationRoot _configurationRoot;
DiscordShardedClient _shardedClient;
DiscordSocketConfig _sockeCconfig;
InteractionService _interactionService;

CreateHostBuilder(args).Build().Run();

IHostBuilder CreateHostBuilder(string[] args) =>
	Host.CreateDefaultBuilder(args)
		.UseWindowsService()
		.UseSystemd()
		.ConfigureHostOptions(option =>
		{
			option.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
		})
		.ConfigureServices((hostContext, services) =>
		{
			string? rawRootPath = Directory
				.GetParent(AppContext.BaseDirectory)
				?.FullName ?? string.Empty;

			_configurationRoot = new ConfigurationBuilder()
				.SetBasePath(rawRootPath)
				.AddJsonFile(
					path: "appsettings.json",
					optional: false,
					reloadOnChange: true)
				.Build();

			services.AddSingleton(_configurationRoot);
			services.AddHttpClient();
			services.AddLogging(loggingBuilder =>
			{
				string rawFileName = Path.Combine(rawRootPath!, "Logs\\app_log_{0:yyyyMMdd}_.txt");

				loggingBuilder.AddFile(rawFileName, fileLoggerOpts =>
				{
					fileLoggerOpts.MinLevel = LogLevel.Information;
					fileLoggerOpts.Append = true;
					fileLoggerOpts.MaxRollingFiles = 3;
					fileLoggerOpts.FileSizeLimitBytes = 8 * 1024 * 1024;
					fileLoggerOpts.FormatLogFileName = outputFileName =>
					{
						return string.Format(outputFileName, DateTime.Now);
					};
				});
			});

			int totalShards = int.TryParse(
				_configurationRoot["discord:totalShards"],
				out int tempInt1) ?
				tempInt1 : 1;

			int messageCacheSize = int.TryParse(
				_configurationRoot["discord:messageCacheSize"],
				out int tempInt2) ?
				tempInt2 : 0;

			_sockeCconfig = new DiscordSocketConfig()
			{
				TotalShards = totalShards,
				GatewayIntents = GatewayIntents.AllUnprivileged,
				MessageCacheSize = messageCacheSize,
				LogLevel = LogSeverity.Verbose
			};

			_shardedClient = new DiscordShardedClient(_sockeCconfig);

			services.AddSingleton(_shardedClient);

			_interactionService = new InteractionService(_shardedClient);

			services.AddSingleton(_interactionService);
			services.AddSingleton<CommandHandler>();
			services.AddHostedService<Worker>();

			ushort lavalinkPort = ushort.TryParse(
				_configurationRoot["dataSource:lavalink:port"],
				out ushort tempUshort1) ?
				tempUshort1 : (ushort)2333;

			LavaConfig _lavaConfig = new()
			{
				Hostname = _configurationRoot["dataSource:lavalink:hostname"],
				Port = lavalinkPort,
				Authorization = _configurationRoot["dataSource:lavalink:authorization"],
				SelfDeaf = false
			};

			services.AddSingleton(_lavaConfig);

			// 2021-12-30
			// 未確認，Shard 的 ID 理論上是屬於 n-1 的形式。
			// 即第一個 Shard 為 0，第二個 Shard 為 1，後續以此類推。
			// 因 Victoria 需要 DiscordSocketClient 才可以正常初始化，
			// 故目前只能使用此方式讓 LavaNode 初始化後，並 DI，
			// 這樣 AudioService.cs 才能正常運作。
			for (int i = 0; i < totalShards; i++)
            {
				DiscordSocketClient _socketClient = _shardedClient.GetShard(0);

				LavaNode<XLavaPlayer> _lavaNode = new(_socketClient, _lavaConfig);

				services.AddSingleton(_lavaNode);
			}

			services.AddSingleton<AudioService>();
		});