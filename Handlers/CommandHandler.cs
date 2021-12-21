using DBotDDRefuge.Modules;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DBotDDRefuge.Handlers;

public class CommandHandler
{
    private readonly IConfigurationRoot _configurationRoot;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommandHandler> _logger;
    private readonly DiscordShardedClient _shardedClient;
    private readonly InteractionService _interactionService;
 
    public CommandHandler(
        IConfigurationRoot configurationRoot,
        IServiceProvider serviceProvider,
        ILogger<CommandHandler> logger,
        DiscordShardedClient shardedClient,
        InteractionService interactionService)
    {
        _configurationRoot = configurationRoot;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _shardedClient = shardedClient;
        _interactionService = interactionService;
    }

    public async Task InitializeAsync()
    {
        // Add the public modules that inherit InteractionModuleBase<T> to the InteractionService.
        //await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

        bool enableICalendar = bool.TryParse(
            _configurationRoot["dataSource:iCalendar:enable"],
            out bool tempBool1) &&
            tempBool1;

        bool enableLavalink = bool.TryParse(
            _configurationRoot["dataSource:lavalink:enable"],
            out bool tempBool2) &&
            tempBool2;

        // 因特殊需求，改為手動載入 Module。
        await _interactionService.AddModuleAsync<CommonModule>(_serviceProvider);

        if (enableICalendar)
        {
            await _interactionService.AddModuleAsync<ICalendarModule>(_serviceProvider);
        }

        await _interactionService.AddModuleAsync<ModeratorModule>(_serviceProvider);

        if (enableLavalink)
        {
            await _interactionService.AddModuleAsync<AudioModule>(_serviceProvider);
        }

        // Process the InteractionCreated payloads to execute Interactions commands.
        _shardedClient.InteractionCreated += HandleInteraction;

        // Process the command execution results.
        _interactionService.SlashCommandExecuted += SlashCommandExecuted;
        _interactionService.ContextCommandExecuted += ContextCommandExecuted;
        _interactionService.ComponentCommandExecuted += ComponentCommandExecuted;
    }

    private async Task ComponentCommandExecuted(
        ComponentCommandInfo componentCommandInfo,
        IInteractionContext interactionContext,
        IResult result)
    {
        if (!result.IsSuccess)
        {
            switch (result.Error)
            {
                case InteractionCommandError.UnmetPrecondition:
                    _logger.LogError("{Error}", result.ErrorReason);
                    break;
                case InteractionCommandError.UnknownCommand:
                    _logger.LogError("{Error}", result.ErrorReason);
                    break;
                case InteractionCommandError.BadArgs:
                    _logger.LogError("{Error}", result.ErrorReason);
                    break;
                case InteractionCommandError.Exception:
                    _logger.LogError("{Error}", result.ErrorReason);
                    break;
                case InteractionCommandError.Unsuccessful:
                    _logger.LogError("{Error}", result.ErrorReason);
                    break;
                default:
                    break;
            }

            Embed replyEmbed = new EmbedBuilder()
            {
                Title = "Component 指令執行失敗",
                Description = $"錯誤訊息：{result.ErrorReason}",
                Color = Color.Red
            }.Build();

            await interactionContext.Channel.SendMessageAsync(embed: replyEmbed);
        }
    }

    private async Task ContextCommandExecuted(
        ContextCommandInfo contextCommandInfo, 
        IInteractionContext interactionContext,
        IResult result)
    {
        if (!result.IsSuccess)
        {
            switch (result.Error)
            {
                case InteractionCommandError.UnmetPrecondition:
                    _logger.LogError("{Error}", result.ErrorReason);
                    break;
                case InteractionCommandError.UnknownCommand:
                    _logger.LogError("{Error}", result.ErrorReason);
                    break;
                case InteractionCommandError.BadArgs:
                    _logger.LogError("{Error}", result.ErrorReason);
                    break;
                case InteractionCommandError.Exception:
                    _logger.LogError("{Error}", result.ErrorReason);
                    break;
                case InteractionCommandError.Unsuccessful:
                    _logger.LogError("{Error}", result.ErrorReason);
                    break;
                default:
                    break;
            }

            Embed replyEmbed = new EmbedBuilder()
            {
                Title = "Context 指令執行失敗",
                Description = $"錯誤訊息：{result.ErrorReason}",
                Color = Color.Red
            }.Build();

            await interactionContext.Channel.SendMessageAsync(embed: replyEmbed);
        }
    }

    private async Task SlashCommandExecuted(
        SlashCommandInfo slashCommandInfo,
        IInteractionContext interactionContext,
        IResult result)
    {
        if (!result.IsSuccess)
        {
            switch (result.Error)
            {
                case InteractionCommandError.UnmetPrecondition:
                    _logger.LogError("{Error}", result.ErrorReason);
                    break;
                case InteractionCommandError.UnknownCommand:
                    _logger.LogError("{Error}", result.ErrorReason);
                    break;
                case InteractionCommandError.BadArgs:
                    _logger.LogError("{Error}", result.ErrorReason);
                    break;
                case InteractionCommandError.Exception:
                    _logger.LogError("{Error}", result.ErrorReason);
                    break;
                case InteractionCommandError.Unsuccessful:
                    _logger.LogError("{Error}", result.ErrorReason);
                    break;
                default:
                    break;
            }

            Embed replyEmbed = new EmbedBuilder()
            {
                Title = "斜線命令執行失敗",
                Description = $"錯誤訊息：{result.ErrorReason}",
                Color = Color.Red
            }.Build();

            await interactionContext.Channel.SendMessageAsync(embed: replyEmbed);
        }
    }

    private async Task HandleInteraction(SocketInteraction socketInteraction)
    {
        try
        {
            // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules.
            ShardedInteractionContext shardedInteractionContext = new(_shardedClient, socketInteraction);

            await _interactionService.ExecuteCommandAsync(shardedInteractionContext, _serviceProvider);
        }
        catch (Exception ex)
        {
            _logger.LogError("{Exception}", ex.Message);

            // If a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist.
            // It is a good idea to delete the original response,
            // or at least let the user know that something went wrong during the command execution.
            if (socketInteraction.Type == InteractionType.ApplicationCommand)
            {
                await socketInteraction
                    .GetOriginalResponseAsync()
                    .ContinueWith(async (message) => 
                        await message.Result.DeleteAsync());
            } 
        }
    }
}