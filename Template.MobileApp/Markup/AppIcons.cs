namespace Template.MobileApp.Markup;

using Fonts;

public static class AppIcons
{
    private const double MenuSize = 24d;

    //--------------------------------------------------------------------------------
    // Menu (Material / 24 / White)
    //--------------------------------------------------------------------------------

    public static readonly FontImageSource AdminPanelSettings = Create(MaterialIcons.Admin_panel_settings, MenuSize, Colors.White);
    public static readonly FontImageSource Info = Create(MaterialIcons.Info, MenuSize, Colors.White);

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
