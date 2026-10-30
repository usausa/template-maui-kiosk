namespace Template.MobileApp.Components;

public readonly record struct SystemUpdateWindow(TimeOnly Start, TimeOnly End)
{
    private const string TimeFormat = "HH:mm";

    public static SystemUpdateWindow Default { get; } = new(new TimeOnly(2, 0), new TimeOnly(4, 0));

    public static bool TryParse(string? value, out SystemUpdateWindow window)
    {
        var times = value?.Split('-');
        if ((times is { Length: 2 }) &&
            TimeOnly.TryParseExact(times[0], TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) &&
            TimeOnly.TryParseExact(times[1], TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
        {
            window = new SystemUpdateWindow(start, end);
            return true;
        }

        window = default;
        return false;
    }

    public override string ToString() =>
        $"{Start.ToString(TimeFormat, CultureInfo.InvariantCulture)}-{End.ToString(TimeFormat, CultureInfo.InvariantCulture)}";
}
