using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Filespec;
using MKTestAzureFunction;

namespace MKTestAzureFunction.Tests;

public class IsdocAttachmentExtractorTests
{
    private readonly IsdocAttachmentExtractor _extractor = new();

    [Fact]
    public void ExtractIsdocXml_ReturnsEmbeddedIsdocAttachment()
    {
        const string xml = "<isdoc><invoice id=\"A1\" /></isdoc>";
        using var pdf = CreatePdfWithAttachment("invoice.isdoc", xml);

        var extracted = _extractor.ExtractIsdocXml(pdf);

        Assert.Equal(xml, extracted);
    }

    [Fact]
    public void ExtractIsdocXml_ThrowsWhenIsdocAttachmentIsMissing()
    {
        using var pdf = CreatePdfWithAttachment("notes.txt", "example");

        var act = () => _extractor.ExtractIsdocXml(pdf);

        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Equal("No ISDOC XML attachment found in the PDF file.", exception.Message);
    }

    private static MemoryStream CreatePdfWithAttachment(string name, string content)
    {
        var stream = new MemoryStream();

        using (var writer = new PdfWriter(stream))
        {
            writer.SetCloseStream(false);
            using var document = new PdfDocument(writer);
            document.AddNewPage();
            var attachment = PdfFileSpec.CreateEmbeddedFileSpec(
                document,
                System.Text.Encoding.UTF8.GetBytes(content),
                name,
                name,
                null,
                null,
                PdfName.Data);

            document.AddFileAttachment(name, attachment);
        }

        stream.Position = 0;
        return stream;
    }
}
