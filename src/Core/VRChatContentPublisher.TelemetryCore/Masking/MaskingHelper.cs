using System.Security.Cryptography;
using System.Text;

namespace VRChatContentPublisher.TelemetryCore.Masking;

public static class MaskingHelper
{
    private const string Salt = "6584fec2-e58d-457a-aa84-1576f316bad9";

    public static string Hash(string input)
    {
        var saltedInput = Salt + input;
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(saltedInput));

        return Convert.ToHexStringLower(hashBytes).Substring(0, 8);
    }
}