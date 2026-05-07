# MKTestAzureFunction

Azure Function (`POST /api/extract-isdoc`) that accepts a PDF payload, extracts an embedded ISDOC/XML attachment, and returns the XML body as `application/xml`.

## Run locally

```bash
dotnet restore
dotnet build
func start
```

## Example request

Raw PDF body:

```bash
curl -X POST "http://localhost:7071/api/extract-isdoc" \
  -H "Content-Type: application/pdf" \
  --data-binary @invoice.pdf
```

Multipart upload:

```bash
curl -X POST "http://localhost:7071/api/extract-isdoc" \
  -F "file=@invoice.pdf"
```
