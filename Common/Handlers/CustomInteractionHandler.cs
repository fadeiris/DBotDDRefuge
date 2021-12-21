using DBotDDRefuge.Common.Extensions;
using DBotDDRefuge.Common.POCO;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace DBotDDRefuge.Common.Handlers;

/// <summary>
/// 自定義 Interaction 的 Handler
/// </summary>
public partial class CustomInteractionHandler
{
    /// <summary>
    /// 處理 iCalendar 事件
    /// </summary>
    /// <param name="httpClientFactory">IHttpClientFactory</param>
    /// <param name="logger">ILogger</param>
    /// <param name="socketMessageComponent">SocketMessageComponent</param>
    /// <param name="queryType">查詢類型</param>
    public static async void GetICalendarEventAsync(
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        SocketMessageComponent socketMessageComponent,
        string queryType)
    {
        ISocketMessageChannel socketMessageChannel = socketMessageComponent.Channel;

        try
        {
            // 更新原訊息的內容。
            CustomComponent customComponent = GetCalendarComponent();

            await socketMessageComponent.UpdateAsync(n =>
            {
                n.Content = customComponent.Content;
                n.Components = customComponent.Components;
            });

            List<CustomICalendarEvent> dataSet = await ICalendarFunction
                .DownloadiCalendarFile(httpClientFactory, logger);

            // 來源：https://stackoverflow.com/a/658362
            DayOfWeek weekStart = DayOfWeek.Sunday;

            DateTime dtDate = DateTime.Now;
            DateTime dtWeekStart = dtDate.AddDays(-7);
            DateTime dtWeekEnd = dtDate.AddDays(-1);

            string rawTypeText = string.Empty;

            switch (queryType)
            {
                case nameof(ICalendarFunction.QueryType.opt_upcoming):
                    rawTypeText = ICalendarFunction.QueryType.opt_upcoming.GetDescription();

                    dataSet = dataSet.Where(n => n.StartTime?.AsSystemLocal.Date == DateTime.Now.Date &&
                            n.StartTime.AsSystemLocal >= DateTime.Now)
                        .OrderBy(n => n.StartTime)
                        .ToList();

                    break;
                case nameof(ICalendarFunction.QueryType.opt_today):
                    rawTypeText = ICalendarFunction.QueryType.opt_today.GetDescription();

                    dataSet = dataSet.Where(n => n.StartTime?.AsSystemLocal.Date == DateTime.Now.Date)
                        .OrderBy(n => n.StartTime)
                        .ToList();

                    break;
                case nameof(ICalendarFunction.QueryType.opt_tomorrow):
                    rawTypeText = ICalendarFunction.QueryType.opt_tomorrow.GetDescription();

                    dataSet = dataSet.Where(n => n.StartTime?.AsSystemLocal.Date ==
                            DateTime.Now.AddDays(1).Date)
                        .OrderBy(n => n.StartTime)
                        .ToList();

                    break;
                case nameof(ICalendarFunction.QueryType.opt_yesterday):
                    rawTypeText = ICalendarFunction.QueryType.opt_yesterday.GetDescription();

                    dataSet = dataSet.Where(n => n.StartTime?.AsSystemLocal.Date ==
                            DateTime.Now.AddDays(-1).Date)
                        .OrderBy(n => n.StartTime)
                        .ToList();

                    break;
                case nameof(ICalendarFunction.QueryType.opt_this_week):
                    rawTypeText = ICalendarFunction.QueryType.opt_this_week.GetDescription();

                    while (dtDate.DayOfWeek != weekStart)
                    {
                        dtDate = dtDate.AddDays(1);
                    }

                    dtWeekStart = dtDate.AddDays(-7);
                    dtWeekEnd = dtDate.AddDays(-1);

                    dataSet = dataSet.Where(n => n.StartTime?.AsSystemLocal.Year == dtDate.Year &&
                            n.StartTime.AsSystemLocal.Month == dtDate.Month &&
                            n.StartTime.AsSystemLocal.Date >= dtWeekStart.Date &&
                            n.StartTime.AsSystemLocal.Date <= dtWeekEnd.Date)
                        .OrderBy(n => n.StartTime)
                        .ToList();

                    break;
                case nameof(ICalendarFunction.QueryType.opt_next_week):
                    rawTypeText = ICalendarFunction.QueryType.opt_next_week.GetDescription();

                    while (dtDate.DayOfWeek != weekStart)
                    {
                        dtDate = dtDate.AddDays(2);
                    }

                    dtWeekStart = dtDate.AddDays(-7);
                    dtWeekEnd = dtDate.AddDays(-1);

                    dataSet = dataSet.Where(n => n.StartTime?.AsSystemLocal.Year == dtDate.Year &&
                            n.StartTime.AsSystemLocal.Month == dtDate.Month &&
                            n.StartTime.AsSystemLocal.Date >= dtWeekStart.Date &&
                            n.StartTime.AsSystemLocal.Date <= dtWeekEnd.Date)
                        .OrderBy(n => n.StartTime)
                        .ToList();

                    break;
                case nameof(ICalendarFunction.QueryType.opt_last_week):
                    rawTypeText = ICalendarFunction.QueryType.opt_last_week.GetDescription();

                    while (dtDate.DayOfWeek != weekStart)
                    {
                        dtDate = dtDate.AddDays(-1);
                    }

                    dtWeekStart = dtDate.AddDays(-7);
                    dtWeekEnd = dtDate.AddDays(-1);

                    dataSet = dataSet.Where(n => n.StartTime?.AsSystemLocal.Year == dtDate.Year &&
                            n.StartTime.AsSystemLocal.Month == dtDate.Month &&
                            n.StartTime.AsSystemLocal.Date >= dtWeekStart.Date &&
                            n.StartTime.AsSystemLocal.Date <= dtWeekEnd.Date)
                        .OrderBy(n => n.StartTime)
                        .ToList();

                    break;
                case nameof(ICalendarFunction.QueryType.opt_this_month):
                    rawTypeText = ICalendarFunction.QueryType.opt_this_month.GetDescription();

                    dataSet = dataSet.Where(n => n.StartTime?.AsSystemLocal.Year == dtDate.Year &&
                            n.StartTime.AsSystemLocal.Month == dtDate.Month)
                        .OrderBy(n => n.StartTime)
                        .ToList();

                    break;
                case nameof(ICalendarFunction.QueryType.opt_next_month):
                    rawTypeText = ICalendarFunction.QueryType.opt_next_month.GetDescription();

                    dtDate = DateTime.Now.AddMonths(1);

                    dataSet = dataSet.Where(n => n.StartTime?.AsSystemLocal.Year == dtDate.Year &&
                        n.StartTime.AsSystemLocal.Month == dtDate.Month)
                      .OrderBy(n => n.StartTime)
                      .ToList();

                    break;
                case nameof(ICalendarFunction.QueryType.opt_last_month):
                    rawTypeText = ICalendarFunction.QueryType.opt_last_month.GetDescription();

                    dtDate = DateTime.Now.AddMonths(-1);

                    dataSet = dataSet.Where(n => n.StartTime?.AsSystemLocal.Year == dtDate.Year &&
                            n.StartTime.AsSystemLocal.Month == dtDate.Month)
                        .OrderBy(n => n.StartTime)
                        .ToList();

                    break;
                default:
                    dataSet.Clear();

                    break;
            };

            if (dataSet.Count > 0)
            {
                dataSet = dataSet.Where(n => (n.Summary != null &&
                    n.Summary.ContainsAny(ICalendarFunction.keywordsSet)) ||
                    (n.Description != null
                    && n.Description.ContainsAny(ICalendarFunction.keywordsSet)))
                    .ToList();

                // RestUserMessage 的 text 最多只能 2000 個字，所以需要拆分資料。
                IEnumerable<IEnumerable<CustomICalendarEvent>> splitedDataSet = dataSet.SplitIntoSets(5);

                Embed[] embeds = new Embed[splitedDataSet.Count()];

                int cycleCount = 0;

                foreach (IEnumerable<CustomICalendarEvent> rawDataSet in splitedDataSet)
                {
                    string rawContent = string.Empty;

                    foreach (CustomICalendarEvent iCalendarEvent in rawDataSet)
                    {
                        string rawTempContent = string.Empty;

                        rawTempContent += $"{iCalendarEvent.Summary}{Environment.NewLine}";
                        rawTempContent += $"開始時間：{iCalendarEvent.StartTime}{Environment.NewLine}";
                        rawTempContent += iCalendarEvent.Description?.TrimEnd(Environment.NewLine.ToCharArray());
                        rawTempContent += Environment.NewLine;
                        rawTempContent += Environment.NewLine;

                        rawContent += rawTempContent;
                    }

                    embeds[cycleCount] = new EmbedBuilder()
                    {
                        Title = $"{rawTypeText}日曆事件資訊 - {cycleCount + 1}／{embeds.Length}",
                        // Description 最多只能 4096 個字。
                        Description = rawContent,
                        Color = Color.Green
                    }.Build();

                    cycleCount++;
                }

                // RestUserMessage 的 embeds 最多只能有 10 個 Embed。
                IEnumerable<IEnumerable<Embed>> splitedEmbedSet = embeds.SplitIntoSets(10);

                foreach (IEnumerable<Embed> rawEmbedSet in splitedEmbedSet)
                {
                    await socketMessageChannel.SendMessageAsync(embeds: rawEmbedSet.ToArray());
                }
            }
            else
            {
                await socketMessageChannel.SendMessageAsync(embed: new EmbedBuilder()
                {
                    Title = "作業完成",
                    Description = $"目前無{rawTypeText}日曆事件資訊。",
                    Color = Color.Gold
                }.Build());
            }
        }
        catch (Exception ex)
        {
            string rawDescription = $"發生錯誤，錯誤訊息：{ex.Message}";

            logger.LogError("{ErrorMessage}", ex);

            await socketMessageChannel.SendMessageAsync(embed: new EmbedBuilder()
            {
                Title = "作業完成",
                Description = rawDescription,
                Color = Color.Red
            }.Build());
        }
    }

    /// <summary>
    /// 處理活動事件
    /// </summary>
    /// <param name="httpClientFactory">IHttpClientFactory</param>
    /// <param name="logger">ILogger</param>
    /// <param name="discordShardedClient">DiscordShardedClient</param>
    /// <param name="socketMessageComponent">SocketMessageComponent</param>
    /// <param name="actionType">作動類型</param>
    public static async void GetActivityEventAsync(
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        DiscordShardedClient discordShardedClient,
        SocketMessageComponent socketMessageComponent,
        string actionType)
    {
        ISocketMessageChannel socketMessageChannel = socketMessageComponent.Channel;

        try
        {
            // 更新原訊息的內容。
            CustomComponent customComponent = GetModerateActivityComponent();

            await socketMessageComponent.UpdateAsync(n =>
            {
                n.Content = customComponent.Content;
                n.Components = customComponent.Components;
            });

            if (socketMessageComponent.Channel is SocketGuildChannel socketGuildChannel)
            {
                DiscordSocketClient discordSocketClient = discordShardedClient
                    .GetShardFor(socketGuildChannel.Guild);

                RestApplication restApplication = await discordSocketClient.GetApplicationInfoAsync();

                IReadOnlyCollection<RestGuildEvent> guildEvents = await socketGuildChannel
                    .Guild.GetEventsAsync();

                // 排除正在進行中的活動。
                IEnumerable<RestGuildEvent> filteredEvents = guildEvents
                    .Where(n => n.CreatorId == restApplication?.Id &&
                    n.Status != GuildScheduledEventStatus.Active);

                switch (actionType)
                {
                    case "opt_import":
                        List<CustomICalendarEvent> dataSet = await ICalendarFunction
                            .DownloadiCalendarFile(httpClientFactory, logger);

                        dataSet = dataSet.Where(n => n.StartTime?.AsSystemLocal > DateTime.Now &&
                                ((n.Summary != null &&
                                n.Summary.ContainsAny(ICalendarFunction.keywordsSet)) ||
                                (n.Description != null &&
                                n.Description.ContainsAny(ICalendarFunction.keywordsSet))))
                            .OrderBy(n => n.StartTime)
                            .ToList();

                        // 一個伺服器最多只能有 100 個活動。
                        int leftCount = 100 - guildEvents.Count,
                            insertCount = 0,
                            updateCount = 0,
                            failedCount = 0;

                        foreach (CustomICalendarEvent iCalendarEvent in dataSet)
                        {
                            string rawSummary = iCalendarEvent.Summary
                                ?.TrimStart().TrimEnd() ??
                                string.Empty;

                            RestGuildEvent? restGuildEvent = filteredEvents
                                .FirstOrDefault(n => n.Name == rawSummary);

                            if (restGuildEvent != null)
                            {
                                try
                                {
                                    // 編輯活動。
                                    // 更新原則：當值不一致時才會更新該欄位。
                                    await restGuildEvent.ModifyAsync(n =>
                                    {
                                        // Name 最多只能 100 個字。
                                        string? rawName = iCalendarEvent.Summary;

                                        if (!string.IsNullOrEmpty(rawName))
                                        {
                                            if (rawName.Length > 100)
                                            {
                                                rawName = rawName[..98] + "……";
                                            }
                                        }

                                        if (!string.IsNullOrEmpty(rawName) &&
                                            n.Name.GetValueOrDefault() != rawName)
                                        {
                                            n.Name = rawName;
                                        }

                                        // Description 最多只能 1000 個字。
                                        string? rawDescription = iCalendarEvent.Description;

                                        if (!string.IsNullOrEmpty(rawDescription))
                                        {
                                            if (rawDescription.Length > 1000)
                                            {
                                                rawDescription = rawDescription[..998] + "……";
                                            }
                                        }

                                        if (!string.IsNullOrEmpty(rawDescription) &&
                                            n.Description.GetValueOrDefault() != rawDescription)
                                        {
                                            n.Description = rawDescription;

                                            // 取 Description 內容中的第一個網址當作地點。
                                            string rawUrl = CustomFunction.GetFirstUrl(rawDescription);

                                            // Location 最多只能 100 個字。
                                            if (!string.IsNullOrEmpty(rawUrl) && rawUrl.Length <= 100)
                                            {
                                                if (n.Location.GetValueOrDefault() != rawUrl)
                                                {
                                                    n.Location = rawUrl;
                                                }
                                            }
                                            else
                                            {
                                                n.Location = "無地點";
                                            }
                                        }

                                        DateTimeOffset? startTime = iCalendarEvent.StartTime?.AsDateTimeOffset;

                                        if (startTime != null && 
                                            n.StartTime.GetValueOrDefault() != startTime)
                                        {
                                            n.StartTime = startTime.Value;
                                        }

                                        DateTimeOffset? endTime = iCalendarEvent.EndTime?.AsDateTimeOffset;

                                        if (endTime != null && 
                                            n.EndTime.GetValueOrDefault() != endTime)
                                        {
                                            n.EndTime = endTime.Value;
                                        }
                                    });

                                    updateCount++;
                                }
                                catch (Exception ex1)
                                {
                                    logger.LogError("{Exception}", ex1);

                                    failedCount++;
                                }
                            }
                            else
                            {
                                // 判斷剩餘活動數量是否足夠。
                                if (leftCount > 0)
                                {
                                    try
                                    {
                                        // 排除 Summary 跟 StartTime 為 null 的日歷事件資料。
                                        if (iCalendarEvent.Summary != null && iCalendarEvent.StartTime != null)
                                        {
                                            string? rawName = iCalendarEvent.Summary, 
                                                    rawDescription = iCalendarEvent.Description, 
                                                    rawLocation = string.Empty, 
                                                    rawUrl = string.Empty;

                                            DateTimeOffset startTime = iCalendarEvent.StartTime.AsDateTimeOffset;
                                            DateTimeOffset? endTime = iCalendarEvent.EndTime?.AsDateTimeOffset;

                                            if (!string.IsNullOrEmpty(rawName))
                                            {
                                                // Name 最多只能 100 個字。
                                                if (rawName.Length > 100)
                                                {
                                                    rawName = rawName[0..98] + "……";
                                                }
                                            }

                                            if (!string.IsNullOrEmpty(rawDescription))
                                            {
                                                // Description 最多只能 1000 個字。
                                                if (rawDescription.Length > 1000)
                                                {
                                                    rawDescription = rawDescription[0..998] + "……";
                                                }

                                                // 取 Description 內容中的第一個網址當作地點。
                                                rawUrl = CustomFunction.GetFirstUrl(rawDescription);

                                                // Location 最多只能 100 個字。
                                                if (!string.IsNullOrEmpty(rawUrl) && rawUrl.Length <= 100)
                                                {
                                                    rawLocation = rawUrl;
                                                }
                                                else
                                                {
                                                    rawLocation = "無地點";
                                                }
                                            }
                                            else
                                            {
                                                // 因 rawDescription 為空白或是空值，
                                                // 故手動指定 rawLocation，以避免發生錯誤。
                                                //
                                                // 當 RestGuildEvent 的 type 為 GuildScheduledEventType.External 時，
                                                // location 不能為 null。
                                                rawLocation = "無地點";
                                            }

                                            // 新增活動。
                                            await socketGuildChannel.Guild.CreateEventAsync(
                                                name: rawName,
                                                startTime: startTime,
                                                type: GuildScheduledEventType.External,
                                                privacyLevel: GuildScheduledEventPrivacyLevel.Private,
                                                description: rawDescription,
                                                endTime: endTime,
                                                location: rawLocation);

                                            leftCount--;

                                            insertCount++;
                                        }
                                        else
                                        {
                                            logger.LogWarning(
                                                "活動新增失敗，此日曆事件「EventName」沒有名稱或開始時間。",
                                                iCalendarEvent.Summary);

                                            failedCount++;
                                        }
                                    }
                                    catch (Exception ex2)
                                    {
                                        logger.LogError("{Exception}", ex2);

                                        failedCount++;
                                    }
                                }
                                else
                                {
                                    logger.LogWarning("此伺服器活動數量已達上限 100 個，無法再新增新活動。");

                                    failedCount++;
                                }
                            }
                        }

                        string rawDescription1 = $"已成功新增 {insertCount} 個活動、更新 {updateCount} 個活動" +
                            $"；共 {failedCount} 個活動新增／更新失敗。";

                        await socketMessageChannel.SendMessageAsync(embed: new EmbedBuilder()
                        {
                            Title = "作業完成",
                            Description = rawDescription1,
                            Color = Color.Green
                        }.Build());

                        break;
                    case "opt_clear":
                        int successCount = 0;

                        foreach (RestGuildEvent rawRestGuildEvent in filteredEvents)
                        {
                            try
                            {
                                await rawRestGuildEvent.DeleteAsync();

                                successCount++;
                            }
                            catch (Exception ex2)
                            {
                                logger.LogError("單一活動刪除失敗，錯誤訊息：{Exception}", ex2);
                            }
                        }

                        string rawDescription2 = $"已成功刪除 {successCount}／{filteredEvents.Count()} 個活動，" +
                            $"共 {filteredEvents.Count() - successCount} 個活動刪除失敗。";

                        await socketMessageChannel.SendMessageAsync(embed: new EmbedBuilder()
                        {
                            Title = "作業完成",
                            Description = rawDescription2,
                            Color = Color.Green
                        }.Build());

                        break;
                    default:
                        break;
                }
            }
            else
            {
                logger.LogError("socketMessageComponent.Channel 不是 SocketGuildChannel。");
            }
        }
        catch (Exception ex1)
        {
            string rawDescription = $"發生錯誤，錯誤訊息：{ex1.Message}";

            logger.LogError("{ErrorMessage}", ex1);

            await socketMessageChannel.SendMessageAsync(embed: new EmbedBuilder()
            {
                Title = "作業完成",
                Description = rawDescription,
                Color = Color.Red
            }.Build());
        }
    }
 
    /// <summary>
    /// 產生 Calendar 的 CustomComponent
    /// </summary>
    /// <returns>CustomComponent</returns>
    public static CustomComponent GetCalendarComponent()
    {
        SelectMenuBuilder selectMenuBuilder = new SelectMenuBuilder()
            .WithPlaceholder("請選擇")
            .WithCustomId("menu_calendar")
            .WithMinValues(1)
            .WithMaxValues(1)
            .AddOption(
                "現在",
                nameof(ICalendarFunction.QueryType.opt_upcoming),
                "接收現在的日歷事件資訊。")
            .AddOption(
                "本日",
                nameof(ICalendarFunction.QueryType.opt_today),
                "接收本日的日歷事件資訊。")
            .AddOption(
                "明日",
                nameof(ICalendarFunction.QueryType.opt_tomorrow),
                "接收明日的日歷事件資訊。")
            .AddOption(
                "昨天",
                nameof(ICalendarFunction.QueryType.opt_yesterday),
                "接收昨天的日歷事件資訊。")
            .AddOption(
                "本週",
                nameof(ICalendarFunction.QueryType.opt_this_week),
                "接收本週的日歷事件資訊。")
            .AddOption(
                "下週",
                nameof(ICalendarFunction.QueryType.opt_next_week),
                "接收下週的日歷事件資訊。")
            .AddOption(
                "上週",
                nameof(ICalendarFunction.QueryType.opt_last_week),
                "接收上週的日歷事件資訊。")
            .AddOption(
                "本月",
                nameof(ICalendarFunction.QueryType.opt_this_month),
                "接收本月的日歷事件資訊。")
            .AddOption(
                "下個月",
                nameof(ICalendarFunction.QueryType.opt_next_month),
                "接收下個月的日歷事件資訊。")
            .AddOption(
                "上個月",
                nameof(ICalendarFunction.QueryType.opt_last_month),
                "接收上個月的日歷事件資訊。");

        ComponentBuilder componentBuilder = new ComponentBuilder()
            .WithSelectMenu(selectMenuBuilder);

        MessageComponent rawComponents = componentBuilder.Build();

        string rawContent = $"**臺 V 初配、歌回、新衣及重大發表日曆**{Environment.NewLine}" +
            $"{Environment.NewLine}※僅會篩選出與**初配信**、**歌回**、**週年**以及**新衣裝**等有關的日曆事件。" +
            $"{Environment.NewLine}" +
            $"{Environment.NewLine}請選擇您欲查詢的時間區間：";

        return new CustomComponent()
        {
            Content = rawContent,
            Components = rawComponents
        };
    }

    /// <summary>
    /// 產生 ModerateActivity 的 CustomComponent
    /// </summary>
    /// <returns>CustomComponent</returns>
    public static CustomComponent GetModerateActivityComponent()
    {
        SelectMenuBuilder selectMenuBuilder = new SelectMenuBuilder()
            .WithPlaceholder("請選擇")
            .WithCustomId("menu_activity")
            .WithMinValues(1)
            .WithMaxValues(1)
            .AddOption("匯入活動", "opt_import", "將臺 V 初配、歌回、新衣及重大發表日曆的事件匯入活動。")
            .AddOption("刪除活動", "opt_clear", "刪除由本機器人所建立的活動。");

        ComponentBuilder componentBuilder = new ComponentBuilder()
            .WithSelectMenu(selectMenuBuilder);

        MessageComponent rawComponents = componentBuilder.Build();

        string rawContent = $"**活動管理**{Environment.NewLine}" +
            $"{Environment.NewLine}※僅會匯入與**初配信**、**歌回**、**週年**以及**新衣裝**等有關的日曆事件。" +
            $"{Environment.NewLine}" +
            $"{Environment.NewLine}請選擇您要執行的操作：";

        return new CustomComponent()
        {
            Content = rawContent,
            Components = rawComponents
        };
    }
}