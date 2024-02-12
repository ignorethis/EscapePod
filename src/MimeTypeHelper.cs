using System.Runtime.CompilerServices;

namespace EscapePod;

public static class MimeTypeHelper
{
    public static string GetFileExtensionFromMimeType(string mimeType)
    {
        return mimeType switch
        {
            "audio/mpeg" => "mp3",
            "audio/x-m4a" => "m4a",
            "audio/x-wav" => "wav",
            "audio/x-aiff" => "aif",
            "audio/x-pn-realaudio" => "ra",
            "audio/x-ms-wma" => "wma",
            "audio/midi" => "mid",
            _ => throw new SwitchExpressionException(mimeType)
        };
    }
}