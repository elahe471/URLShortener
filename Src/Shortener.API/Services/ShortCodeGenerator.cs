namespace Shortener.API.Services;

public sealed class ShortCodeGenerator
    : IShortCodeGenerator
{
    private const string Alphabet =
        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    private const int Rounds = 8;
    private const int HalfBits = 26;
    private const int ShortCodeLength = 9;

    private const ulong HalfMask =
        (1UL << HalfBits) - 1;

    private const ulong MaxSequence =
        (1UL << 52) - 1;

    private readonly byte[] _secretKey;

    public ShortCodeGenerator(
        IOptions<ShortenerSettings> options)
    {
        _secretKey =
            Convert.FromBase64String(
                options.Value.SecretKey);
    }

    public string Generate(long sequence)
    {
        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequence),
                "Sequence must be greater than zero.");
        }

        var value = (ulong)sequence;

        if (value > MaxSequence)
        {
            throw new InvalidOperationException(
                "Shortener sequence capacity exceeded.");
        }

        var permuted = Permute(value);

        return EncodeBase62(permuted);
    }

    private ulong Permute(ulong value)
    {
        // Split the 52-bit value into two 26-bit halves.
        ulong left =
            (value >> HalfBits) & HalfMask;

        ulong right =
            value & HalfMask;

        // Apply the Feistel permutation.
        for (byte round = 0; round < Rounds; round++)
        {
            var nextLeft = right;

            var nextRight =
                (left ^ RoundFunction(right, round))
                & HalfMask;

            left = nextLeft;
            right = nextRight;
        }

        // Merge the two halves back into a 52-bit value.
        return (left << HalfBits) | right;
    }

    private ulong RoundFunction(
        ulong right,
        byte round)
    {
        Span<byte> input = stackalloc byte[9];

        BinaryPrimitives.WriteUInt64BigEndian(
            input[..8],
            right);

        input[8] = round;

        Span<byte> hash = stackalloc byte[32];

        HMACSHA256.HashData(
            _secretKey,
            input,
            hash);

        var value =
            BinaryPrimitives.ReadUInt32BigEndian(
                hash[..4]);

        return value & HalfMask;
    }

    private static string EncodeBase62(
        ulong value)
    {
        Span<char> buffer =
            stackalloc char[ShortCodeLength];

        for (var i = buffer.Length - 1; i >= 0; i--)
        {
            buffer[i] =
                Alphabet[(int)(value % 62)];

            value /= 62;
        }

        return new string(buffer);
    }
}