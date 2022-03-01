using System.Collections.Generic;
using Humanizer;
using Slapper;

namespace WinTenDev.Zizi.Utils;

public static class MapperUtil
{
    public static T DictionaryMapper<T>(this Dictionary<string, object> dictionary)
    {
        return AutoMapper.Map<T>(dictionary);
    }

    public static Dictionary<string, object> ToDictionary(
        this object values,
        bool enumToString = true
    )
    {
        var dictionary = new Dictionary<string, object>();

        if (values == null) return dictionary;

        var properties = values.GetType()
            .GetProperties();

        foreach (var property in properties)
        {
            var value = property.GetValue(values, null) ?? string.Empty;

            if (property.PropertyType.IsEnum && enumToString)
            {
                value = value.ToString();
            }

            dictionary.Add(
                property.Name.Underscore(),
                value
            );
        }

        return dictionary;
    }
}