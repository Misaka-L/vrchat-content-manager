using System.Text.RegularExpressions;
using AssetsTools.NET;

namespace VRChatContentPublisher.BundleProcessCore.Utils;

public static partial class FileTypeDetector
{
    // https://github.com/nesrak1/UABEA/blob/e06aaf58366124010b3f86a86756f826f4fe0848/UABEAvalonia/Logic/FileTypeDetector.cs#L18C1-L67C6
    public static DetectedFileType DetectFileType(AssetsFileReader reader, long startAddress)
    {
        reader.BigEndian = true;

        if (reader.BaseStream.Length < 0x20)
        {
            return DetectedFileType.Unknown;
        }

        reader.Position = startAddress;
        var possibleBundleHeader = reader.ReadStringLength(7);
        reader.Position = startAddress + 0x08;
        var possibleFormat = reader.ReadInt32();

        reader.Position = startAddress + (possibleFormat >= 0x16 ? 0x30 : 0x14);

        var possibleVersion = "";
        char curChar;
        while (reader.Position < reader.BaseStream.Length && (curChar = (char)reader.ReadByte()) != 0x00)
        {
            possibleVersion += curChar;
            if (possibleVersion.Length > 0xFF)
            {
                break;
            }
        }

        var emptyVersion = EmptyVersionRegex().Replace(possibleVersion, "");
        var fullVersion = FullVersionRegex().Replace(possibleVersion, "");

        if (possibleBundleHeader == "UnityFS")
        {
            return DetectedFileType.BundleFile;
        }
        else if (possibleFormat < 0xFF && emptyVersion.Length == 0 && fullVersion.Length >= 5)
        {
            return DetectedFileType.AssetsFile;
        }

        return DetectedFileType.Unknown;
    }

    [GeneratedRegex(@"[a-zA-Z0-9\.\n\-]")]
    private static partial Regex EmptyVersionRegex();

    [GeneratedRegex(@"[^a-zA-Z0-9\.\n\-]")]
    private static partial Regex FullVersionRegex();
}

public enum DetectedFileType
{
    Unknown,
    AssetsFile,
    BundleFile
}