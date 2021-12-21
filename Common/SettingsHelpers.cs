using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DBotDDRefuge.Common;

/// <summary>
/// SettingsHelpers
/// <para>來源：https://stackoverflow.com/a/60436834 </para>
/// </summary>
public static class SettingsHelpers
{
    /// <summary>
    /// 設定檔案的檔案名稱（含副檔名）
    /// </summary>
    private static readonly string FileName = "config.json";

    public static void AddOrUpdateAppSetting<T>(string sectionPathKey, T value)
    {
        try
        {
            string rawFilePath = Path.Combine(AppContext.BaseDirectory, FileName);
            string rawJson = File.ReadAllText(rawFilePath);

            dynamic? jsonObj = JsonConvert.DeserializeObject(rawJson);

            SetValueRecursively(sectionPathKey, jsonObj, value);

            string rawOutputJson = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);

            File.WriteAllText(rawFilePath, rawOutputJson);
        }
        catch (Exception ex)
        {
            Console.WriteLine("{0} 更新失敗，錯誤訊息：{1}", FileName, ex.ToString());
        }
    }

    private static void SetValueRecursively<T>(string sectionPathKey, dynamic jsonObj, T value)
    {
        // Split the string at the first ':' character.
        string[] remainingSections = sectionPathKey.Split(":", 2);

        string rawCurrentSection = remainingSections[0];

        if (remainingSections.Length > 1)
        {
            // Continue with the procress, moving down the tree.
            string rawNextSection = remainingSections[1];

            jsonObj[rawCurrentSection] ??= new JObject();

            SetValueRecursively(rawNextSection, jsonObj[rawCurrentSection], value);
        }
        else
        {
            // We've got to the end of the tree, set the value.
            jsonObj[rawCurrentSection] = value;
        }
    }
}