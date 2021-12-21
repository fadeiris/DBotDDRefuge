using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;

namespace DBotDDRefuge.Modules;

public class ModeratorModule : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly ILogger<ModeratorModule> _logger;

    /// <summary>
    /// 手動控制斜線命令的權限
    /// </summary>
    private const GuildPermission _permission = GuildPermission.UseApplicationCommands;

    public ModeratorModule(ILogger<ModeratorModule> logger)
    {
        _logger = logger;
    }

    [SlashCommand("delele", "刪除訊息。")]
    [DefaultMemberPermissions(_permission)]
    public async Task DeleteAsync(
        [Summary("限制數量")] int limit = 5,
        [Summary("顯示訊息")] bool showMessage = true)
    {
        Embed replyEmbed;

        try
        {
            #region 預處理 limit 值

            // 當 limit 小於等於 0 時，設為 1。
            if (limit <= 0)
            {
                limit = 1;
            }

            // 當 limit 大於於 100 時，設為 100。
            if (limit > 100)
            {
                limit = 100;
            }

            #endregion

            await RespondAsync(text: "正在執行訊息刪除作業。");

            ITextChannel? textChannel = Context.Channel as ITextChannel;

            IEnumerable<IMessage> messages = await textChannel
                !.GetMessagesAsync(limit: limit)
                .FlattenAsync();

            string rawDescription = string.Empty;

            try
            {
                await textChannel.DeleteMessagesAsync(messages);

                rawDescription = $"已成功刪除 {messages.Count()} 則訊息。";
            }
            catch (Exception ex1)
            {
                _logger.LogError("批次刪除訊息失敗，錯誤訊息：{Exception}", ex1);

                // 改為使用單一刪除作為備援手段。
                int successCount = 0;

                foreach (IMessage singleMessage in messages)
                {
                    try
                    {
                        await textChannel.DeleteMessageAsync(singleMessage);

                        successCount++;
                    }
                    catch (Exception ex2)
                    {
                        _logger.LogError("單一則訊息刪除失敗，錯誤訊息：{Exception}", ex2);
                    }
                }

                rawDescription = $"已成功刪除 {successCount}／{messages.Count()} 則訊息，" +
                    $"共 {messages.Count() - successCount} 則訊息刪除失敗。";
            }

            replyEmbed = new EmbedBuilder()
            {
                Title = "作業完成",
                Description = rawDescription,
                Color = Color.Green
            }.Build();
        }
        catch (Exception ex)
        {
            _logger.LogError("{Exception}", ex);

            replyEmbed = new EmbedBuilder()
            {
                Title = "作業完成",
                Description = $"訊息刪除失敗，錯誤訊息：{ex.Message}",
                Color = Color.Red
            }.Build();
        }

        if (showMessage)
        {
            await Context.Channel.SendMessageAsync(embed: replyEmbed);
        }
        else
        {
            _logger.LogInformation("{Information}", replyEmbed.Description);
        }
    }
}