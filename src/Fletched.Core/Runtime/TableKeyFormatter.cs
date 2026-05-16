using System;
using System.Globalization;

namespace Fletched.Core.Runtime;

/// <summary>
/// Canonical formatter used when constructing deterministic table-key call fragments.
/// </summary>
public static class TableKeyFormatter
{
    public static string Format(object? value)
    {
        return value switch
        {
            null => "null",
            string text => "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"",
            char character => "'" + character.ToString() + "'",
            bool boolean => boolean ? "true" : "false",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? "null",
            _ => value.ToString() ?? "null",
        };
    }
}
