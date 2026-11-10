using System.Buffers.Binary;

namespace VbeSort.Infrastructure;

/// <summary>
/// Nuskaito PNG paveikslelio matmenis is IHDR antrastes (pirmi 24 baitai), neiskoduojant viso failo.
/// Matmenys naudojami width/height atributams, kad narsykle zinotu tikslias proporcijas
/// (jokio istempimo ir jokio turinio "sokinejimo" kraunantis).
/// </summary>
public static class PngDimensionReader
{
    private static ReadOnlySpan<byte> Signature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static (int Width, int Height)? TryRead(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return TryRead(stream);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static (int Width, int Height)? TryRead(Stream stream)
    {
        Span<byte> header = stackalloc byte[24];
        if (stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) < header.Length) return null;
        if (!header[..8].SequenceEqual(Signature)) return null;

        // 8-11: chunk ilgis, 12-15: "IHDR"
        if (header[12] != (byte)'I' || header[13] != (byte)'H' || header[14] != (byte)'D' || header[15] != (byte)'R')
            return null;

        var width = BinaryPrimitives.ReadInt32BigEndian(header[16..20]);
        var height = BinaryPrimitives.ReadInt32BigEndian(header[20..24]);
        return width > 0 && height > 0 ? (width, height) : null;
    }
}
