using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace FusionLedger.Windows;

/// <summary>
/// Shows a validated PNG avatar in a PersonPicture. A picture that is out of the tree when the theme changes comes
/// back with only the initials, so the image is decoded again every time the picture is loaded or its theme changes.
/// </summary>
internal static class AvatarImage
{
    public static void Attach(PersonPicture picture, byte[] png, int decodePixelWidth)
    {
        picture.Loaded += (_, _) => _ = ApplyAsync(picture, png, decodePixelWidth);
        picture.ActualThemeChanged += (_, _) => _ = ApplyAsync(picture, png, decodePixelWidth);
        if (picture.IsLoaded) _ = ApplyAsync(picture, png, decodePixelWidth);
    }

    private static async Task ApplyAsync(PersonPicture picture, byte[] png, int decodePixelWidth)
    {
        try
        {
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(png.AsBuffer());
            stream.Seek(0);
            var image = new BitmapImage { DecodePixelWidth = decodePixelWidth };
            await image.SetSourceAsync(stream);
            picture.ProfilePicture = image;
        }
        catch
        {
            // The initials fallback stays in place.
        }
    }
}
