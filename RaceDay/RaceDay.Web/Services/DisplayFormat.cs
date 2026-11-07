namespace RaceDay.Web.Services;

// Small helpers so every page shows times and positions the same way
public static class DisplayFormat
{
    // 00:52:35
    public static string Time(TimeSpan time) => time.ToString(@"hh\:mm\:ss");

    // 1st, 2nd, 3rd, 4th ... 11th, 12th, 13th ... 21st
    public static string Ordinal(int number)
    {
        int lastTwoDigits = number % 100;
        if (lastTwoDigits is >= 11 and <= 13)
        {
            return number + "th";
        }

        return (number % 10) switch
        {
            1 => number + "st",
            2 => number + "nd",
            3 => number + "rd",
            _ => number + "th"
        };
    }
}