using System.Net;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace MKTestAzureFunction;

public sealed class ExtractIsdocFunction(IsdocAttachmentExtractor extractor, ILogger<ExtractIsdocFunction> logger)
{
    private readonly IsdocAttachmentExtractor _extractor = extractor;
    private readonly ILogger<ExtractIsdocFunction> _logger = logger;

    [Function(nameof(ExtractIsdocFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "extract-isdoc")] HttpRequestData request)
    {
        byte[] pdfContent;

        try
        {
            pdfContent = await ReadPdfContentAsync(request);
        }
        catch (InvalidOperationException ex)
        {
            return await CreateTextResponseAsync(request, HttpStatusCode.BadRequest, ex.Message);
        }

        if (pdfContent.Length == 0)
        {
            return await CreateTextResponseAsync(request, HttpStatusCode.BadRequest, "PDF content is empty.");
        }

        try
        {
            using var pdfStream = new MemoryStream(pdfContent);
            var xml = _extractor.ExtractIsdocXml(pdfStream);

            var response = request.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/xml; charset=utf-8");
            await response.WriteStringAsync(xml);
            return response;
        }
        catch (InvalidOperationException ex)
        {
            return await CreateTextResponseAsync(request, HttpStatusCode.NotFound, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process PDF input.");
            return await CreateTextResponseAsync(request, HttpStatusCode.BadRequest, "Invalid PDF input.");
        }
    }

    private static async Task<byte[]> ReadPdfContentAsync(HttpRequestData request)
    {
        var contentType = request.Headers.TryGetValues("Content-Type", out var values)
            ? values.FirstOrDefault()
            : null;

        if (string.IsNullOrWhiteSpace(contentType) ||
            !contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
        {
            return await ReadToByteArrayAsync(request.Body);
        }

        if (!MediaTypeHeaderValue.TryParse(contentType, out var mediaType))
        {
            throw new InvalidOperationException("Invalid Content-Type header.");
        }

        var boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;
        if (string.IsNullOrWhiteSpace(boundary))
        {
            throw new InvalidOperationException("Missing multipart boundary.");
        }

        var reader = new MultipartReader(boundary, request.Body);
        MultipartSection? section;

        while ((section = await reader.ReadNextSectionAsync()) is not null)
        {
            if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition) ||
                !disposition.DispositionType.Equals("form-data", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (StringSegment.IsNullOrEmpty(disposition.FileName) && StringSegment.IsNullOrEmpty(disposition.FileNameStar))
            {
                continue;
            }

            return await ReadToByteArrayAsync(section.Body);
        }

        throw new InvalidOperationException("No file part found in multipart payload.");
    }

    private static async Task<byte[]> ReadToByteArrayAsync(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    private static async Task<HttpResponseData> CreateTextResponseAsync(
        HttpRequestData request,
        HttpStatusCode statusCode,
        string message)
    {
        var response = request.CreateResponse(statusCode);
        await response.WriteStringAsync(message);
        return response;
    }
}
