using DBotDDRefuge.Common.Handlers;
using DBotDDRefuge.Common.POCO;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DBotDDRefuge.Modules;

public class ICalendarModule : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly IConfigurationRoot _configurationRoot;
    private readonly ILogger<ICalendarModule> _logger;

    /// <summary>
    /// 手動控制斜線命令的權限
    /// </summary>
    private const GuildPermission _permission = GuildPermission.UseApplicationCommands;

    public ICalendarModule(
        IConfigurationRoot configurationRoot,
        ILogger<ICalendarModule> logger)
    {
        _configurationRoot = configurationRoot;
        _logger = logger;
    }

    [SlashCommand("link", "接收臺 V 初配、歌回、新衣及重大發表日曆的連結。")]
    [DefaultMemberPermissions(_permission)]
    public async Task GetCalendarLinkAsync()
    {
        try
        {
            string rawUrl = _configurationRoot["dataSource:iCalendar:embedUrl"];

            ComponentBuilder componentBuilder = new ComponentBuilder()
                .WithButton(
                    label: "臺 V 初配、歌回、新衣及重大發表日曆",
                    style: ButtonStyle.Link,
                    url: rawUrl);

            MessageComponent rawComponents = componentBuilder.Build();

            string rawText = "點擊下方的按鈕，以開啟**臺 V 初配、歌回、新衣及重大發表日曆**！";

            await RespondAsync(text: rawText, components: rawComponents);
        }
        catch (Exception ex)
        {
            _logger.LogError("{Exception}", ex);

            Embed replyEmbed = new EmbedBuilder()
            {
                Title = "作業完成",
                Description = $"發生錯誤，錯誤訊息：{ex.Message}",
                Color = Color.Red
            }.Build();

            await RespondAsync(embed: replyEmbed);
        }
    }

    [SlashCommand("calendar", "接收臺 V 初配、歌回、新衣及重大發表日曆的事件資訊。")]
    [DefaultMemberPermissions(_permission)]
    public async Task GetCalendarAsync()
    {
        try
        {
            CustomComponent customComponent = CustomInteractionHandler
                .GetCalendarComponent();

            await RespondAsync(
                text: customComponent.Content,
                components: customComponent.Components);
        }
        catch (Exception ex)
        {
            _logger.LogError("{Exception}", ex);

            Embed replyEmbed = new EmbedBuilder()
            {
                Title = "作業完成",
                Description = $"發生錯誤，錯誤訊息：{ex.Message}",
                Color = Color.Red
            }.Build();

            await RespondAsync(embed: replyEmbed);
        }
    }

    [ComponentInteraction("menu_calendar")]
    public Task GetCalendarSelectedAsync(params string[] _)
    {
        // 留空，以免在 ComponentCommandExecuted 處會輸出錯誤。
        // 實際的判斷處裡邏輯位於 SelectMenuExecuted 內。
        return Task.CompletedTask;
    }

    [SlashCommand("activity", "匯入／刪除活動。")]
    [DefaultMemberPermissions(_permission)]
    public async Task ModerateActivityAsync()
    {
        try
        {
            CustomComponent customComponent = CustomInteractionHandler
                .GetModerateActivityComponent();

            await RespondAsync(
                text: customComponent.Content,
                components: customComponent.Components);
        }
        catch (Exception ex)
        {
            _logger.LogError("{Exception}", ex);

            Embed replyEmbed = new EmbedBuilder()
            {
                Title = "作業完成",
                Description = $"發生錯誤，錯誤訊息：{ex.Message}",
                Color = Color.Red
            }.Build();

            await RespondAsync(embed: replyEmbed);
        }
    }

    [ComponentInteraction("menu_activity")]
    public Task GetActivitySelectedAsync(params string[] _)
    {
        // 留空，以免在 ComponentCommandExecuted 處會輸出錯誤。
        // 實際的判斷處裡邏輯位於 SelectMenuExecuted 內。
        return Task.CompletedTask;
    }
}