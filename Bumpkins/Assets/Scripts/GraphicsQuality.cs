public static class GraphicsQuality
{
    public static bool IsHD { get; set; } = true;
    public static string SpritePath => IsHD ? "Sprites-HD" : "Sprites";
}
