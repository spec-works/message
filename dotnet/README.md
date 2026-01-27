# Message.Net

A .NET library for parsing, validating, and serializing Internet message format (message/rfc822) data according to RFC 5322 and MIME (RFC 2045-2049).

## Installation

```bash
dotnet add package Message.Net
```

## Quick Start

### Parsing a Message

```csharp
using Message;

// Parse from string
var parser = new MessageParser();
var message = parser.Parse(emailText);

// Access headers
Console.WriteLine($"From: {message.From}");
Console.WriteLine($"To: {message.To}");
Console.WriteLine($"Subject: {message.Subject}");
Console.WriteLine($"Date: {message.Date}");

// Access body
if (message.IsMultipart)
{
    foreach (var part in message.Parts)
    {
        Console.WriteLine($"Part: {part.ContentType}");
    }
}
else
{
    Console.WriteLine($"Body: {message.TextBody}");
}
```

### Validating a Message

```csharp
var validator = new MessageValidator();
var result = validator.Validate(message);

if (result.IsValid)
{
    Console.WriteLine("Message is valid");
}
else
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}
```

### Creating a Message

```csharp
var message = new MessageObject
{
    From = "sender@example.com",
    To = "recipient@example.com",
    Subject = "Hello World",
    Date = DateTimeOffset.Now
};
message.SetTextBody("This is the message body.");

var serializer = new MessageSerializer();
var output = serializer.Serialize(message);
```

### Working with MIME

```csharp
// Create a multipart message
var message = new MessageObject
{
    From = "sender@example.com",
    To = "recipient@example.com",
    Subject = "Message with attachment"
};

// Add text part
message.AddPart(new MimePart
{
    ContentType = "text/plain",
    Content = "This is the plain text body"
});

// Add HTML part
message.AddPart(new MimePart
{
    ContentType = "text/html",
    Content = "<html><body><h1>Hello</h1></body></html>"
});

// Add attachment
message.AddAttachment("document.pdf", pdfBytes, "application/pdf");
```

## Features

- **Full RFC 5322 Support** - Headers, folding, structured fields
- **MIME Support** - Multipart messages, encodings, attachments
- **Content-Transfer-Encoding** - Base64, Quoted-Printable, 7bit, 8bit
- **Encoded Words (RFC 2047)** - Non-ASCII text in headers
- **Validation** - Comprehensive validation against specifications
- **Streaming** - Parse and serialize large messages efficiently

## API Reference

### MessageParser

- `Parse(string text)` - Parse message from string
- `ParseFile(string path)` - Parse message from file
- `ParseStream(Stream stream)` - Parse message from stream

### MessageValidator

- `Validate(MessageObject message)` - Validate message structure

### MessageSerializer

- `Serialize(MessageObject message)` - Serialize to string
- `SerializeToStream(MessageObject message, Stream stream)` - Serialize to stream

## Specifications

- RFC 5322 - Internet Message Format
- RFC 2045 - MIME Part One: Format of Internet Message Bodies
- RFC 2046 - MIME Part Two: Media Types
- RFC 2047 - MIME Part Three: Message Header Extensions for Non-ASCII Text

## License

MIT License
