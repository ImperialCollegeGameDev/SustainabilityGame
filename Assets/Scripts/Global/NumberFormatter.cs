public static class NumberFormatter
{
    private static readonly string[] suffixes =
        { "", "K", "M", "B", "T" };

    public static string Format(double value, bool space = false, int decimals = 1)
    {
        int index = 0;

        while (value >= 1000 && index < suffixes.Length - 1)
        {
            value /= 1000;
            index++;
        }

        string valueString = value.ToString($"0.{new string('#', decimals)}");
        if (space)
            valueString += " ";
        return valueString + suffixes[index];
    }

    public static string FormatMoney(double value, bool unit = false, int decimals = 1)
    {
        string formattedValue = Format(value * 1000000, unit, decimals);
        return formattedValue;
    }

    public static string FormatPower(double value, bool unit = true, int decimals = 1)
    {
        string formattedValue = Format(value * 1000, unit, decimals);
        if (unit)
            formattedValue += "W";
        return formattedValue;
    }
}