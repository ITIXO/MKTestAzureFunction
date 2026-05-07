using System.Text;
using iText.Kernel.Pdf;

namespace MKTestAzureFunction;

public sealed class IsdocAttachmentExtractor
{
    public string ExtractIsdocXml(Stream pdfStream)
    {
        using var reader = new PdfReader(pdfStream);
        using var pdfDocument = new PdfDocument(reader);

        foreach (var attachment in GetEmbeddedFiles(pdfDocument))
        {
            if (!LooksLikeIsdocAttachment(attachment.Name, attachment.Content))
            {
                continue;
            }

            return DecodeXml(attachment.Content);
        }

        throw new InvalidOperationException("No ISDOC XML attachment found in the PDF file.");
    }

    private static IEnumerable<(string Name, byte[] Content)> GetEmbeddedFiles(PdfDocument pdfDocument)
    {
        var nameTree = pdfDocument.GetCatalog().GetNameTree(PdfName.EmbeddedFiles);
        var files = nameTree?.GetNames();

        if (files is null)
        {
            yield break;
        }

        foreach (var entry in files)
        {
            var fileSpec = entry.Value as PdfDictionary
                ?? (entry.Value as PdfIndirectReference)?.GetRefersTo() as PdfDictionary;

            var embeddedFiles = fileSpec?.GetAsDictionary(PdfName.EF);
            var stream = embeddedFiles?.GetAsStream(PdfName.UF) ?? embeddedFiles?.GetAsStream(PdfName.F);

            if (stream is null)
            {
                continue;
            }

            var content = stream.GetBytes();
            if (content.Length == 0)
            {
                continue;
            }

            var fileName = entry.Key?.ToUnicodeString() ?? string.Empty;
            yield return (fileName, content);
        }
    }

    private static bool LooksLikeIsdocAttachment(string fileName, byte[] content)
    {
        if (fileName.EndsWith(".isdoc", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var payload = DecodeXml(content).TrimStart();
        return payload.StartsWith("<", StringComparison.Ordinal);
    }

    private static string DecodeXml(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }
}
