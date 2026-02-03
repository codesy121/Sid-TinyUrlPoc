using System.Numerics;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using TinyUrl.Api.Domain;

namespace TinyUrl.Api.Infrastructure.Services;

public sealed class Base62CodeGenerator : ICodeGenerator
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private readonly int _shortLength;

    public Base62CodeGenerator(IConfiguration config)
    {
        // Read configured short code length; default to 8 when not present or invalid.
        _shortLength = config?.GetValue("Base62:ShortCodeLength", 8) ?? 8;
        if (_shortLength <= 0) _shortLength = 8;
    }

    public string Generate(int length)
    {
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
        // Special-case: for the configured short length, derive bits from a GUID and
        // encode to base62. This gives a compact UUID-derived ID.
        if (length == _shortLength)
        {
            // Determine how many bytes from the GUID are needed to cover the
            // entropy for `length` base62 characters.
            var bitsNeeded = length * Math.Log2(Alphabet.Length);
            var bytesNeeded = (int)Math.Ceiling(bitsNeeded / 8.0);
            if (bytesNeeded < 1) bytesNeeded = 1;
            if (bytesNeeded > 16) bytesNeeded = 16; // GUID has 16 bytes

            var guidBytes = Guid.NewGuid().ToByteArray();
            var taken = new byte[bytesNeeded + 1]; // extra 0 byte to force positive BigInteger
            Array.Copy(guidBytes, 0, taken, 0, bytesNeeded);

            var value = new BigInteger(taken); // little-endian unsigned value

            var chars = new char[length];
            for (var i = length - 1; i >= 0; i--)
            {
                var rem = (int)(value % Alphabet.Length);
                chars[i] = Alphabet[rem];
                value /= Alphabet.Length;
            }

            return new string(chars);
        }

        // Fallback: use RandomNumberGenerator.GetInt32 for each index to avoid modulo bias
        // and avoid stackalloc/Span usage.
        var result = new char[length];
        for (var i = 0; i < length; i++)
        {
            var idx = RandomNumberGenerator.GetInt32(Alphabet.Length);
            result[i] = Alphabet[idx];
        }

        return new string(result);
    }
}
