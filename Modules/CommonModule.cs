using DBotDDRefuge.Common;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DBotDDRefuge.Modules;

public class CommonModule : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly ILogger<CommonModule> _logger;

    /// <summary>
    /// 手動控制斜線命令的權限
    /// </summary>
    private const GuildPermission _permission = GuildPermission.UseApplicationCommands;

    public CommonModule(ILogger<CommonModule> logger)
    {
        _logger = logger;
    }

    [SlashCommand("ping", "接收一個乓。")]
    [DefaultMemberPermissions(_permission)]
    public async Task GetPongAsync()
    {
        Embed replyEmbed = new EmbedBuilder()
        {
            Title = "乒",
            Description = "乓。",
            Color = Color.Green
        }.Build();

        await RespondAsync(embed: replyEmbed);
    }

    [SlashCommand("time", "接收目前時間。")]
    [DefaultMemberPermissions(_permission)]
    public async Task GetTimeAsync()
    {
        Embed replyEmbed = new EmbedBuilder()
        {
            Title = "目前時間",
            Description = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
            Color = Color.Green
        }.Build();


        await RespondAsync(embed: replyEmbed);
    }

    [SlashCommand("omikuji", "求御神籤。")]
    [DefaultMemberPermissions(_permission)]
    public async Task GetOmikujiAsync([Summary("詢問內容")] string message = "今日運勢？")
    {
        EmbedBuilder embedBuilder;

        string rawDescription = CustomFunction.GetOmikuji();

        embedBuilder = new EmbedBuilder()
        {
            Title = message,
            Description = rawDescription,
            Color = Color.Gold
        };

        if (rawDescription == "請再求一次籤！")
        {
            embedBuilder.Title = "神明沒有聽到你詢問的內容呢";
            embedBuilder.Color = Color.Gold;
        }

        Embed replyEmbed = embedBuilder.Build();

        await RespondAsync(embed: replyEmbed);
    }

    [SlashCommand("send-to", "傳送網址連結至指定的文字頻道。")]
    [DefaultMemberPermissions(_permission)]
    public async Task SensToAsync(
        [Summary("文字頻道")] ITextChannel channel,
        [Summary("網址連結")] string url)
    {
        string rawPattern = @"^(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:/?#[\]@!\$&'\(\)\*\+,;=.]+$";

        Embed replyEmbed;

        try
        {
            if (Regex.IsMatch(url, rawPattern) &&
                Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri? uri))
            {
                string rawUri = uri?.ToString() ?? string.Empty;

                // 只搜尋最近的 100 則訊息。
                IEnumerable<IMessage> messages = await channel.GetMessagesAsync().FlattenAsync();

                if (!messages.Any(n => n.Content.Contains(rawUri)))
                {
                    await channel.SendMessageAsync(rawUri);

                    replyEmbed = new EmbedBuilder()
                    {
                        Title = "作業完成",
                        Description = $"已將輸入的網址連結「<{rawUri}>」發送指定的文字頻道「**{channel.Name}**」。",
                        Color = Color.Green
                    }.Build();
                }
                else
                {
                    replyEmbed = new EmbedBuilder()
                    {
                        Title = "作業完成",
                        Description = $"在文字頻道「**{channel.Name}**」內已存在「<{rawUri}>」。",
                        Color = Color.Gold
                    }.Build();
                }
            }
            else
            {
                replyEmbed = new EmbedBuilder()
                {
                    Title = "作業完成",
                    Description = $"輸入的網址連結「<{url}>」為無效的網址連結。",
                    Color = Color.Red
                }.Build();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("{Exception}", ex);

            replyEmbed = new EmbedBuilder()
            {
                Title = "作業完成",
                Description = $"網址連結傳送失敗，錯誤訊息：{ex.Message}",
                Color = Color.Red
            }.Build();
        }

        await RespondAsync(embed: replyEmbed);
    }
}