namespace DBotDDRefuge.Common.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// 判斷字串是否包含字串陣列的內容
    /// <para>來源：https://stackoverflow.com/a/195628 </para>
    /// </summary>
    /// <param name="rawString">字串</param>
    /// <param name="rawValues">字串陣列</param>
    /// <returns>布林值</returns>
    public static bool ContainsAny(this string rawString, params string[] rawValues)
    {
        if (!string.IsNullOrEmpty(rawString) || rawValues.Length > 0)
        {
            foreach (string rawValue in rawValues)
            {
                if (rawString.Contains(rawValue))
                {
                    return true;
                }
            }
        }

        return false;
    }
}