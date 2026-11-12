using System.Buffers.Binary;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace VbeSort.Services;

/// <summary>
/// Gryno C# (be išorinių priklausomybių, veikia tiek Windows, tiek Linux Docker konteineryje)
/// A4 PDF generatorius, kuris iš pasirinktų metų ir sesijos VBE užduočių PNG paveikslėlių
/// suformuoja pilną egzamino užduočių PDF dokumentą.
/// </summary>
public class ExamPdfService
{
    private readonly ExerciseService _exercises;
    private readonly IMemoryCache _cache;
    private readonly string _basePath;

    public ExamPdfService(ExerciseService exercises, IMemoryCache cache, IConfiguration config)
    {
        _exercises = exercises;
        _cache = cache;
        _basePath = config["VbeConfig:BasePath"] ?? Directory.GetCurrentDirectory();
    }

    public byte[]? BuildSessionExamPdf(string year, string session)
    {
        var sessionExercises = _exercises.GetExercisesByExamSession(year, session);
        if (sessionExercises.Count == 0) return null;

        var cacheKey = $"exampdf:{year}:{session}".ToLowerInvariant();
        if (_cache.TryGetValue(cacheKey, out byte[]? cached) && cached is not null)
        {
            return cached;
        }

        var imageItems = new List<(int ExNumber, byte[] ZlibRgbBytes, int Width, int Height)>();

        foreach (var ex in sessionExercises)
        {
            foreach (var relPath in ex.QuestionImages)
            {
                var fullPath = Path.Combine(_basePath, relPath.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(fullPath)) continue;

                var decoded = TryDecodePngToZlibRgb(fullPath);
                if (decoded is not null)
                {
                    imageItems.Add((ex.Number, decoded.Value.ZlibRgb, decoded.Value.Width, decoded.Value.Height));
                }
            }
        }

        if (imageItems.Count == 0) return null;

        var pdfBytes = WritePdfDocument(year, session, imageItems);
        _cache.Set(cacheKey, pdfBytes, TimeSpan.FromHours(6));
        return pdfBytes;
    }

    private static (byte[] ZlibRgb, int Width, int Height)? TryDecodePngToZlibRgb(string path)
    {
        try
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length < 33) return null;

            var width = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));
            var height = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));
            var bitDepth = bytes[24];
            var colorType = bytes[25];
            var interlace = bytes[28];

            if (width <= 0 || height <= 0 || bitDepth != 8 || interlace != 0) return null;

            byte[]? palette = null;
            using var idatStream = new MemoryStream();

            var pos = 8;
            while (pos + 12 <= bytes.Length)
            {
                var len = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(pos, 4));
                var type = Encoding.ASCII.GetString(bytes, pos + 4, 4);
                var dataStart = pos + 8;
                if (len < 0 || dataStart + len + 4 > bytes.Length) break;

                if (type == "PLTE")
                {
                    palette = bytes.AsSpan(dataStart, len).ToArray();
                }
                else if (type == "IDAT")
                {
                    idatStream.Write(bytes, dataStart, len);
                }
                else if (type == "IEND")
                {
                    break;
                }

                pos = dataStart + len + 4;
            }

            var bpp = colorType switch
            {
                0 => 1, // Grayscale
                2 => 3, // RGB
                3 => 1, // Indexed Palette
                4 => 2, // Grayscale + Alpha
                6 => 4, // RGBA
                _ => 0
            };
            if (bpp == 0) return null;

            idatStream.Position = 0;
            var stride = width * bpp;
            var rawScanlines = new byte[(stride + 1) * height];

            using (var zlib = new ZLibStream(idatStream, CompressionMode.Decompress))
            {
                var totalRead = 0;
                while (totalRead < rawScanlines.Length)
                {
                    var r = zlib.Read(rawScanlines, totalRead, rawScanlines.Length - totalRead);
                    if (r <= 0) break;
                    totalRead += r;
                }
            }

            var unfiltered = new byte[stride * height];
            for (var y = 0; y < height; y++)
            {
                var filterType = rawScanlines[y * (stride + 1)];
                var srcRowOffset = y * (stride + 1) + 1;
                var dstRowOffset = y * stride;
                var prevRowOffset = (y - 1) * stride;

                for (var x = 0; x < stride; x++)
                {
                    var raw = rawScanlines[srcRowOffset + x];
                    var a = x >= bpp ? unfiltered[dstRowOffset + x - bpp] : (byte)0;
                    var b = y > 0 ? unfiltered[prevRowOffset + x] : (byte)0;
                    var c = (x >= bpp && y > 0) ? unfiltered[prevRowOffset + x - bpp] : (byte)0;

                    unfiltered[dstRowOffset + x] = (byte)((raw + filterType switch
                    {
                        1 => a,
                        2 => b,
                        3 => (a + b) / 2,
                        4 => PaethPredictor(a, b, c),
                        _ => 0
                    }) & 0xFF);
                }
            }

            var rgb = new byte[width * height * 3];
            for (var p = 0; p < width * height; p++)
            {
                var sIdx = p * bpp;
                var dIdx = p * 3;

                switch (colorType)
                {
                    case 2: // RGB
                        rgb[dIdx] = unfiltered[sIdx];
                        rgb[dIdx + 1] = unfiltered[sIdx + 1];
                        rgb[dIdx + 2] = unfiltered[sIdx + 2];
                        break;
                    case 6: // RGBA (ant balto fono)
                    {
                        var alpha = unfiltered[sIdx + 3];
                        rgb[dIdx] = CompositeWhite(unfiltered[sIdx], alpha);
                        rgb[dIdx + 1] = CompositeWhite(unfiltered[sIdx + 1], alpha);
                        rgb[dIdx + 2] = CompositeWhite(unfiltered[sIdx + 2], alpha);
                        break;
                    }
                    case 0: // Grayscale
                        rgb[dIdx] = rgb[dIdx + 1] = rgb[dIdx + 2] = unfiltered[sIdx];
                        break;
                    case 4: // Grayscale + Alpha
                    {
                        var g = CompositeWhite(unfiltered[sIdx], unfiltered[sIdx + 1]);
                        rgb[dIdx] = rgb[dIdx + 1] = rgb[dIdx + 2] = g;
                        break;
                    }
                    case 3: // Palette
                    {
                        var palIdx = unfiltered[sIdx] * 3;
                        if (palette is not null && palIdx + 2 < palette.Length)
                        {
                            rgb[dIdx] = palette[palIdx];
                            rgb[dIdx + 1] = palette[palIdx + 1];
                            rgb[dIdx + 2] = palette[palIdx + 2];
                        }
                        break;
                    }
                }
            }

            using var outMs = new MemoryStream();
            using (var zlibOut = new ZLibStream(outMs, CompressionLevel.Fastest, leaveOpen: true))
            {
                zlibOut.Write(rgb, 0, rgb.Length);
            }

            return (outMs.ToArray(), width, height);
        }
        catch
        {
            return null;
        }
    }

    private static byte CompositeWhite(byte channel, byte alpha) =>
        (byte)(((channel * alpha) + (255 * (255 - alpha))) / 255);

    private static int PaethPredictor(int a, int b, int c)
    {
        var p = a + b - c;
        var pa = Math.Abs(p - a);
        var pb = Math.Abs(p - b);
        var pc = Math.Abs(p - c);
        if (pa <= pb && pa <= pc) return a;
        return pb <= pc ? b : c;
    }

    private static byte[] WritePdfDocument(
        string year,
        string session,
        List<(int ExNumber, byte[] ZlibRgbBytes, int Width, int Height)> items)
    {
        const double pageW = 595.0;
        const double pageH = 842.0;
        const double margin = 36.0;
        const double maxContentW = pageW - (margin * 2);
        const double topStart = pageH - margin - 28.0;
        const double bottomLimit = margin + 24.0;

        var pages = new List<List<(int ImgIndex, int ExNumber, double X, double Y, double DrawW, double DrawH)>>();
        var currentPage = new List<(int ImgIndex, int ExNumber, double X, double Y, double DrawW, double DrawH)>();
        var cursorY = topStart;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var scale = Math.Min(1.0, maxContentW / Math.Max(1, item.Width));
            var drawW = Math.Round(item.Width * scale * 0.72, 2);
            var drawH = Math.Round(item.Height * scale * 0.72, 2);

            if (drawW > maxContentW)
            {
                var ratio = maxContentW / drawW;
                drawW = maxContentW;
                drawH = Math.Round(drawH * ratio, 2);
            }

            const double maxSinglePageH = pageH - (margin * 2) - 48.0;
            if (drawH > maxSinglePageH)
            {
                var ratio = maxSinglePageH / drawH;
                drawH = maxSinglePageH;
                drawW = Math.Round(drawW * ratio, 2);
            }

            if (cursorY - drawH - 22.0 < bottomLimit && currentPage.Count > 0)
            {
                pages.Add(currentPage);
                currentPage = new List<(int, int, double, double, double, double)>();
                cursorY = topStart;
            }

            cursorY -= 16.0;
            var imgY = cursorY - drawH;
            var imgX = margin;

            currentPage.Add((i, item.ExNumber, imgX, imgY, drawW, drawH));
            cursorY = imgY - 14.0;
        }

        if (currentPage.Count > 0)
        {
            pages.Add(currentPage);
        }

        var fontObjId = 3;
        var firstImgObjId = 4;
        var firstPageObjId = firstImgObjId + items.Count;

        using var ms = new MemoryStream();
        var offsets = new Dictionary<int, long>();

        void WriteAscii(string s) => ms.Write(Encoding.ASCII.GetBytes(s));

        WriteAscii("%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");

        offsets[1] = ms.Position;
        WriteAscii("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        offsets[fontObjId] = ms.Position;
        WriteAscii("3 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");

        for (var i = 0; i < items.Count; i++)
        {
            var objId = firstImgObjId + i;
            var img = items[i];
            offsets[objId] = ms.Position;
            WriteAscii(string.Create(CultureInfo.InvariantCulture,
                $"{objId} 0 obj\n<< /Type /XObject /Subtype /Image /Width {img.Width} /Height {img.Height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /FlateDecode /Length {img.ZlibRgbBytes.Length} >>\nstream\n"));
            ms.Write(img.ZlibRgbBytes);
            WriteAscii("\nendstream\nendobj\n");
        }

        var pageObjIds = new List<int>();
        var sessionTitle = session == "pagr" ? "Pagrindine sesija" : "Pakartotine sesija";

        for (var pIdx = 0; pIdx < pages.Count; pIdx++)
        {
            var pageObjId = firstPageObjId + (pIdx * 2);
            var contentObjId = pageObjId + 1;
            pageObjIds.Add(pageObjId);

            var pageItems = pages[pIdx];
            var sb = new StringBuilder();

            sb.AppendFormat(CultureInfo.InvariantCulture,
                "BT /F1 10 Tf {0:F1} {1:F1} Td (Matematikos VBE - {2} m. {3} | Puslapis {4} / {5}) Tj ET\n",
                margin, pageH - margin - 10.0, year, sessionTitle, pIdx + 1, pages.Count);

            var xObjRefs = new StringBuilder();
            foreach (var placement in pageItems)
            {
                var imgObjId = firstImgObjId + placement.ImgIndex;
                var imgName = $"/Im{placement.ImgIndex}";
                xObjRefs.Append($"{imgName} {imgObjId} 0 R ");

                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "BT /F1 9 Tf {0:F1} {1:F1} Td (Uzduotis #{2}) Tj ET\n",
                    placement.X, placement.Y + placement.DrawH + 4.0, placement.ExNumber);

                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "q {0:F2} 0 0 {1:F2} {2:F2} {3:F2} cm {4} Do Q\n",
                    placement.DrawW, placement.DrawH, placement.X, placement.Y, imgName);
            }

            var contentBytes = Encoding.ASCII.GetBytes(sb.ToString());

            offsets[pageObjId] = ms.Position;
            WriteAscii(string.Create(CultureInfo.InvariantCulture,
                $"{pageObjId} 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {pageW:F0} {pageH:F0}] /Resources << /Font << /F1 {fontObjId} 0 R >> /XObject << {xObjRefs}>> >> /Contents {contentObjId} 0 R >>\nendobj\n"));

            offsets[contentObjId] = ms.Position;
            WriteAscii(string.Create(CultureInfo.InvariantCulture,
                $"{contentObjId} 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n"));
            ms.Write(contentBytes);
            WriteAscii("\nendstream\nendobj\n");
        }

        offsets[2] = ms.Position;
        var kids = string.Join(' ', pageObjIds.Select(id => $"{id} 0 R"));
        WriteAscii($"2 0 obj\n<< /Type /Pages /Kids [ {kids} ] /Count {pageObjIds.Count} >>\nendobj\n");

        var totalObjs = firstPageObjId + (pages.Count * 2);
        var xrefPos = ms.Position;
        WriteAscii($"xref\n0 {totalObjs}\n0000000000 65535 f \n");
        for (var id = 1; id < totalObjs; id++)
        {
            var off = offsets.GetValueOrDefault(id, 0L);
            WriteAscii($"{off:D10} 00000 n \n");
        }

        WriteAscii($"trailer\n<< /Size {totalObjs} /Root 1 0 R >>\nstartxref\n{xrefPos}\n%%EOF\n");
        return ms.ToArray();
    }
}
