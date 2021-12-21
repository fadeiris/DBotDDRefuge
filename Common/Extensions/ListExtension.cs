namespace DBotDDRefuge.Common.Extensions;

public static class ListExtension
{
    /// <summary>
    /// 拆分 List
    /// <para>來源：https://stackoverflow.com/a/49410128 </para>
    /// </summary>
    /// <typeparam name="T">類型</typeparam>
    /// <param name="source">資料源</param>
    /// <param name="itemsPerSet">拆分依據數值</param>
    /// <returns>拆分好的 List</returns>
    public static IEnumerable<IEnumerable<T>> SplitIntoSets<T>(
        this IEnumerable<T> source,
        int itemsPerSet)
    {
        var sourceList = source as List<T> ?? source.ToList();

        for (var index = 0; index < sourceList.Count; index += itemsPerSet)
        {
            yield return sourceList.Skip(index).Take(itemsPerSet);
        }
    }
}