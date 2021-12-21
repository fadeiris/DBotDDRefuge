using System.ComponentModel;
using System.Reflection;

namespace DBotDDRefuge.Common.Extensions;

public static class EnumExtension
{
    /// <summary>
    /// 取得列舉 Description Attribute 的值
    /// <para>來源：https://marcus116.blogspot.com/2018/12/how-to-get-enum-description-attribute.html </para>
    /// </summary>
    /// <param name="source">T</param>
    /// <returns>字串</returns>
    public static string GetDescription<T>(this T source)
    {
        string? rawOutput = source?.ToString() ?? string.Empty;

        FieldInfo? fileInfo = source?.GetType().GetField(rawOutput);

        DescriptionAttribute[]? attributes = fileInfo
            ?.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];

        if (attributes?.Length > 0)
        {
            return attributes[0].Description;
        }
        else
        {
            return rawOutput;
        }
    }
}