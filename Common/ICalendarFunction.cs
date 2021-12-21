using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using DBotDDRefuge.Common.POCO;
using GetCachable;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RandomUserAgent;
using System.ComponentModel;

namespace DBotDDRefuge.Common;

/// <summary>
/// iCalendar 函式
/// </summary>
public class ICalendarFunction
{
    /// <summary>
    /// IConfigurationRoot
    /// </summary>
    private static readonly IConfigurationRoot config = CustomFunction.GetConfiguration();

    /// <summary>
    /// 資料來源的網址
    /// </summary>
    private static readonly string rawUrl = config["dataSource:iCalendar:url"];

    /// <summary>
    /// 過濾資料用關鍵字
    /// </summary>
    public static readonly string[] keywordsSet = config["dataSource:iCalendar:filterKeywords"]
        .Split(",", StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// 查詢類型
    /// </summary>
    public enum QueryType
    {
        [Description("現在")]
        opt_upcoming,
        [Description("本日")]
        opt_today,
        [Description("明日")]
        opt_tomorrow,
        [Description("昨天")]
        opt_yesterday,
        [Description("本週")]
        opt_this_week,
        [Description("下週")]
        opt_next_week,
        [Description("上週")]
        opt_last_week,
        [Description("本月")]
        opt_this_month,
        [Description("下個月")]
        opt_next_month,
        [Description("上個月")]
        opt_last_month
    }

    /// <summary>
    /// 下載並解析 iCalendar 檔案
    /// </summary>
    /// <param name="httpClientFactory">IHttpClientFactory</param>
    /// <param name="logger">ILogger</param>
    /// <returns>Task&lt;List&lt;CustomICalendarEvent&gt;&gt;</returns>
    public static async Task<List<CustomICalendarEvent>> DownloadiCalendarFile(
        IHttpClientFactory httpClientFactory, ILogger logger)
    {
        List<CustomICalendarEvent> listOutputData = new();

        string rawCacheMinutes = config["dataSource:iCalendar:cacheMinutes"];

        int CacheMinutes = int.TryParse(rawCacheMinutes, out int tempCacheMinutes) ?
            tempCacheMinutes : 10;

        string rawResponseBody = await BetterCacheManager.GetCachableData(rawUrl, async () =>
        {
            logger.LogInformation("本次快取 {CacheMinutes} 分鐘內有效。", CacheMinutes);

            HttpClient httpClient = httpClientFactory.CreateClient();

            // 注意，有可能會取到無效的 User-Agent。
            string rawUserAgent = RandomUa.RandomUserAgent;

            logger.LogInformation("本次的 User-Agent：{UserAgent}", rawUserAgent);

            if (httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(rawUserAgent))
            {
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(rawUserAgent);
            }

            httpClient.DefaultRequestHeaders.Add("Referer", rawUrl);

            HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(rawUrl);

            httpResponseMessage.EnsureSuccessStatusCode();

            return await httpResponseMessage.Content.ReadAsStringAsync();
        }, CacheMinutes);

        Calendar rawCalendar = Calendar.Load(rawResponseBody);

        foreach (CalendarEvent calendarEvent in rawCalendar.Events)
        {
            string rawInputDesc = calendarEvent.Description,
                   rawOutoutDesc = string.Empty;

            HtmlParser htmlParser = new();

            IHtmlDocument htmlDocument = htmlParser.ParseDocument(rawInputDesc);

            List<IHtmlAnchorElement> listAnchorSet = htmlDocument.QuerySelectorAll("a")
                .OfType<IHtmlAnchorElement>()
                .ToList();

            foreach (IHtmlAnchorElement htmlAnchorElement in listAnchorSet)
            {
                string rawText = string.Empty, rawHref = string.Empty;

                if (!string.IsNullOrEmpty(htmlAnchorElement.Text))
                {
                    rawText = $"{htmlAnchorElement.Text}：";
                }

                if (!string.IsNullOrEmpty(htmlAnchorElement.Href))
                {
                    rawHref = $"{htmlAnchorElement.Href}";
                }

                if (!string.IsNullOrEmpty(htmlAnchorElement.Href) ||
                    !string.IsNullOrEmpty(htmlAnchorElement.Text))
                {
                    rawOutoutDesc += $"{rawText}{rawHref}{Environment.NewLine}";
                }
            }

            // 當找不到連結時，改為整個 Description 的內容輸出。
            if (listAnchorSet.Count <= 0)
            {
                rawOutoutDesc = $"{rawInputDesc.TrimStart()}{Environment.NewLine}";
            }

            listOutputData.Add(new CustomICalendarEvent()
            {
                Summary = calendarEvent.Summary.TrimStart().TrimEnd(),
                Description = rawOutoutDesc,
                StartTime = calendarEvent.Start.ToTimeZone("Asia/Taipei"),
                EndTime = calendarEvent.End.ToTimeZone("Asia/Taipei")
            });
        }

        return listOutputData;
    }
}