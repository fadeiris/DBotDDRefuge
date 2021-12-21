using Discord;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace DBotDDRefuge.Common;

public partial class CustomFunction
{
    /// <summary>
    /// 取得 IConfigurationRoot
    /// </summary>
    /// <returns>IConfigurationRoot</returns>
    public static IConfigurationRoot GetConfiguration()
    {
        return new ConfigurationBuilder()
           .SetBasePath(AppContext.BaseDirectory)
           .AddJsonFile(
               "appsettings.json",
               optional: false,
               reloadOnChange: true)
           .Build();
    }

    /// <summary>
    /// 御神籤
    /// <para>來源：https://www.sinyijapan.com/tw/news/meetjapan/news001696 </para>
    /// </summary>
    /// <returns>字串</returns>
    public static string GetOmikuji()
    {
        // 通常：大吉、中吉、小吉、末吉、凶
        // 七層：大吉、中吉、小吉、吉、末吉、凶、大凶
        // 十二層：大吉、中吉、小吉、吉、半吉、末吉、末小吉、凶、小凶、半凶、末凶、大凶

        int randomNumber = RandomNumberGenerator.GetInt32(0, 13);

        return randomNumber switch
        {
            1 => "大吉",
            2 => "中吉",
            3 => "小吉",
            4 => "吉",
            5 => "半吉",
            6 => "末吉",
            7 => "末小吉",
            8 => "凶",
            9 => "小凶",
            10 => "半凶",
            11 => "末凶",
            12 => "大凶",
            _ => "請再求一次籤！"
        };
    }

    /// <summary>
    /// 從字串中取得第一個網址
    /// <para>來源 https://social.msdn.microsoft.com/Forums/vstudio/en-US/af82ce78-6aa7-43cc-8a10-cdacd9b93728/find-url-from-a-text-file-in-c?forum=csharpgeneral </para>
    /// </summary>
    /// <param name="value">字串</param>
    /// <returns>字串</returns>
    public static string GetFirstUrl(string value)
    {
        string rawUrl = string.Empty;
        string rawPattern = @"(http|ftp|https):\/\/([\w\-_]+(?:(?:\.[\w\-_]+)+))([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?";

        foreach (Match item in Regex.Matches(value, rawPattern))
        {
            rawUrl = item.Value;

            // 取第一個就停止。
            break;
        }

        return rawUrl;
    }

    /// <summary>
    /// 將 LogMessage 使用 ILogger 輸出
    /// </summary>
    /// <param name="logger">ILogger</param>
    /// <param name="logMessage">LogMessage</param>
    public static void WriteLog(ILogger logger, LogMessage logMessage)
    {
        switch (logMessage.Severity)
        {
            case LogSeverity.Debug:
                logger.LogDebug("{LogMessage}", logMessage);
                break;
            case LogSeverity.Error:
                logger.LogError("{LogMessage}", logMessage);
                break;
            case LogSeverity.Warning:
                logger.LogWarning("{LogMessage}", logMessage);
                break;
            case LogSeverity.Info:
                logger.LogInformation("{LogMessage}", logMessage);
                break;
            case LogSeverity.Critical:
                logger.LogCritical("{LogMessage}", logMessage);
                break;
            case LogSeverity.Verbose:
                logger.LogTrace("{LogMessage}", logMessage);
                break;
            default:
                logger.LogWarning("不支援的 Severity，訊息內容：{LogMessage}", logMessage);
                break;
        }
    }
}