using System;
using System.Collections.Generic;

namespace Memory;

public static class Utils
{
    public static byte[] ParseSig(string sig, out byte[] mask)
    {
        sig = sig.Replace('*', '?').Trim();
        while (sig.EndsWith(" ?") || sig.EndsWith(" ??"))
        {
            if (sig.EndsWith(" ??"))
            {
                sig = sig[..^3];
            }
            if (sig.EndsWith(" ?"))
            {
                sig = sig[..^2];
            }
        }
        var stringByteArray = sig.Split(' ');
        var sigPattern = new byte[stringByteArray.Length];
        mask = new byte[stringByteArray.Length];

        for (var i = 0; i < stringByteArray.Length; i++)
        {
            var ba = stringByteArray[i];

            if (ba == "??" || (ba.Length == 1 && ba == "?"))
            {
                mask[i] = 0x00;
                stringByteArray[i] = "0x00";
            }
            else if (char.IsLetterOrDigit(ba[0]) && ba[1] == '?')
            {
                mask[i] = 0xF0;
                stringByteArray[i] = ba[0] + "0";
            }
            else if (char.IsLetterOrDigit(ba[1]) && ba[0] == '?')
            {
                mask[i] = 0x0F;
                stringByteArray[i] = "0" + ba[1];
            }
            else
            {
                mask[i] = 0xFF;
            }
        }

        for (var i = 0; i < stringByteArray.Length; i++)
        {
            const int hexBase = 16;
            sigPattern[i] = (byte)(Convert.ToByte(stringByteArray[i], hexBase) & mask[i]);
        }
        
        return sigPattern;
    }
    
    public static byte[] StringToBytes(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            const string exception = "Hex cannot be null, empty, or whitespace";
            throw new ArgumentException(exception);
        }
        
        hex = hex.ToLower();
        hex = hex.Replace(" ", "");

        var startIndex = hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
        var length = (hex.Length - startIndex) / 2;
        var bytesArr = new byte[length];

        for (int i = startIndex, x = 0; i < hex.Length; i += 2, x++)
        {
            var left = hex[i];
            var right = hex[i + 1];
            bytesArr[x] = (byte)((HexMap[left] << 4) | HexMap[right]);
        }

        return bytesArr;
    }

    private static readonly Dictionary<char, byte> HexMap = new()
    {
        { 'a', 0xA }, { 'b', 0xB }, { 'c', 0xC }, { 'd', 0xD },
        { 'e', 0xE }, { 'f', 0xF }, { '0', 0x0 }, { '1', 0x1 },
        { '2', 0x2 }, { '3', 0x3 }, { '4', 0x4 }, { '5', 0x5 },
        { '6', 0x6 }, { '7', 0x7 }, { '8', 0x8 }, { '9', 0x9 }
    };
}