namespace Template.MobileApp.Markup;

using Fonts;

public static class AppIcons
{
    private const double MenuSize = 24d;

    //--------------------------------------------------------------------------------
    // Menu (Material / 24 / White)
    //--------------------------------------------------------------------------------

    public static readonly FontImageSource Apps = Create(MaterialIcons.Apps, MenuSize, Colors.White);
    public static readonly FontImageSource Info = Create(MaterialIcons.Info, MenuSize, Colors.White);
    public static readonly FontImageSource Lock = Create(MaterialIcons.Lock, MenuSize, Colors.White);
    public static readonly FontImageSource Policy = Create(MaterialIcons.Policy, MenuSize, Colors.White);
    public static readonly FontImageSource RestartAlt = Create(MaterialIcons.Restart_alt, MenuSize, Colors.White);
    public static readonly FontImageSource SystemUpdate = Create(MaterialIcons.System_update, MenuSize, Colors.White);
    public static readonly FontImageSource Tune = Create(MaterialIcons.Tune, MenuSize, Colors.White);

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static FontImageSource Create(string glyph, double size, Color color) =>
        new()
        {
            FontFamily = MaterialIcons.FontFamily,
            Glyph = glyph,
            Size = size,
            Color = color
        };
}
