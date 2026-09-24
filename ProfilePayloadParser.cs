using System.Text.Json;

namespace FusionLedger.Windows;

/// <summary>
/// The small, renderer-independent profile contract consumed by the native title bar.
/// </summary>
public sealed record ProfileSnapshot(string? Username, byte[]? AvatarPng)
{
    public static ProfileSnapshot Empty { get; } = new(null, null);
}

/// <summary>
/// Pure validation and parsing for the restricted /api/me response.
/// It deliberately accepts no image format other than a bounded PNG data URI.
/// </summary>
public static class ProfilePayloadParser
{
    public const int MaxResponseBytes = 256 * 1024;
    public const int MaxAvatarBytes = 32 * 1024;
    public const int MaxUsernameLength = 128;
    private const string PngDataPrefix = "data:image/png;base64,";

    public static bool IsMeGet(Uri? uri, string? method)
    {
        return uri is not null
            && string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)
            && uri.Scheme == Uri.UriSchemeHttps
            && string.Equals(uri.Host, "fusion-ledger.desase0175.workers.dev", StringComparison.OrdinalIgnoreCase)
            && uri.AbsolutePath == "/api/me"
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment)
            && (uri.IsDefaultPort || uri.Port == 443);
    }

    public static bool IsSessionInvalidatingPost(Uri? uri, string? method)
    {
        return uri is not null
            && string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase)
            && uri.Scheme == Uri.UriSchemeHttps
            && string.Equals(uri.Host, "fusion-ledger.desase0175.workers.dev", StringComparison.OrdinalIgnoreCase)
            && (uri.AbsolutePath == "/api/logout" || uri.AbsolutePath == "/api/password")
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment)
            && (uri.IsDefaultPort || uri.Port == 443);
    }

    public static ProfileSnapshot? Parse(int statusCode, ReadOnlyMemory<byte> utf8Json)
    {
        if (statusCode == 401)
        {
            return ProfileSnapshot.Empty;
        }

        if (statusCode != 200 || utf8Json.Length == 0 || utf8Json.Length > MaxResponseBytes)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(utf8Json, new JsonDocumentOptions
            {
                MaxDepth = 16,
                CommentHandling = JsonCommentHandling.Disallow,
                AllowTrailingCommas = false
            });

            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("user", out var user))
            {
                return null;
            }

            if (user.ValueKind == JsonValueKind.Null)
            {
                return ProfileSnapshot.Empty;
            }

            if (user.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            string? username = null;
            if (user.TryGetProperty("username", out var usernameValue)
                && usernameValue.ValueKind == JsonValueKind.String)
            {
                username = usernameValue.GetString()?.Trim();
                if (string.IsNullOrEmpty(username) || username.Length > MaxUsernameLength)
                {
                    username = null;
                }
            }

            byte[]? avatar = null;
            if (user.TryGetProperty("avatar", out var avatarValue)
                && avatarValue.ValueKind == JsonValueKind.String)
            {
                avatar = ParsePngDataUri(avatarValue.GetString());
            }

            return new ProfileSnapshot(username, avatar);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static byte[]? ParsePngDataUri(string? value)
    {
        if (value is null || !value.StartsWith(PngDataPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var encoded = value[PngDataPrefix.Length..];
        if (encoded.Length == 0 || encoded.Length > ((MaxAvatarBytes + 2) / 3) * 4)
        {
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(encoded);
            if (bytes.Length > MaxAvatarBytes || !HasPngSignature(bytes))
            {
                return null;
            }

            return bytes;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static bool HasPngSignature(ReadOnlySpan<byte> bytes)
    {
        ReadOnlySpan<byte> signature = [137, 80, 78, 71, 13, 10, 26, 10];
        return bytes.Length >= signature.Length && bytes[..signature.Length].SequenceEqual(signature);
    }
}
