# Message for .NET

Full-featured .NET library for parsing, validating, and serializing Internet message format (message/rfc822) data according to [RFC 5322](https://www.rfc-editor.org/rfc/rfc5322) and MIME.

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package SpecWorks.Message
```

Or using Package Manager Console:

```powershell
Install-Package SpecWorks.Message
```

## Features

- ✅ **RFC 5322 Compliant** - Full implementation of Internet Message Format
- ✅ **MIME Support** - Multipart messages, attachments, encoded content
- ✅ **Type-Safe API** - Strong typing with nullable reference types
- ✅ **Parse and Generate** - Read existing messages and create new ones
- ✅ **Content Decoding** - Base64, Quoted-Printable, RFC 2047 encoded-words
- ✅ **Validation** - Comprehensive validation against RFC requirements
- ✅ **Multi-Target** - Supports .NET 10.0 and .NET Standard 2.1

## Quick Start

### Parsing a Message

```csharp
using Message;

string emailText = @"From: sender@example.com
To: recipient@example.com
Subject: Hello World
Date: Mon, 27 Jan 2026 10:00:00 +0000
MIME-Version: 1.0
Content-Type: text/plain; charset=utf-8

This is the message body.";

var parser = new MessageParser();
var message = parser.Parse(emailText);

Console.WriteLine($"From: {message.From}");
Console.WriteLine($"To: {message.To}");
Console.WriteLine($"Subject: {message.Subject}");
Console.WriteLine($"Date: {message.Date}");
Console.WriteLine($"Body: {message.TextBody}");
```

### Creating a Message

```csharp
using Message;

var message = new MessageObject
{
    From = "sender@example.com",
    To = "recipient@example.com",
    Subject = "Hello from SpecWorks.Message",
    Date = DateTimeOffset.Now
};
message.MimeVersion = "1.0";
message.SetTextBody("This is the message body.");

var serializer = new MessageSerializer();
string emailText = serializer.Serialize(message);
Console.WriteLine(emailText);
```

### Working with Multipart Messages

```csharp
using Message;

var message = new MessageObject
{
    From = "sender@example.com",
    To = "recipient@example.com",
    Subject = "Message with attachment"
};

// Make it multipart
message.MakeMultipart();

// Add text body
message.AddPart(new MimePart
{
    ContentType = "text/plain; charset=utf-8",
    Content = "This is the plain text body."
});

// Add HTML body
message.AddPart(new MimePart
{
    ContentType = "text/html; charset=utf-8",
    Content = "<html><body><h1>Hello!</h1></body></html>"
});

// Add attachment
byte[] fileContent = File.ReadAllBytes("document.pdf");
message.AddAttachment("document.pdf", fileContent, "application/pdf");
```

### Validating a Message

```csharp
using Message;

var validator = new MessageValidator();
var result = validator.Validate(message);

if (result.IsValid)
{
    Console.WriteLine("Message is valid!");
}
else
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}

foreach (var warning in result.Warnings)
{
    Console.WriteLine($"Warning: {warning}");
}
```

### Working with Email Addresses

```csharp
using Message;

// Parse address with display name
var addr = MailboxAddress.Parse("\"John Doe\" <john@example.com>");
Console.WriteLine($"Name: {addr.DisplayName}");  // John Doe
Console.WriteLine($"Email: {addr.Address}");     // john@example.com

// Parse multiple addresses
var addresses = MailboxAddress.ParseList("alice@example.com, Bob <bob@example.com>");
foreach (var a in addresses)
{
    Console.WriteLine($"{a.DisplayName ?? "(no name)"}: {a.Address}");
}
```

## API Reference

Browse the complete API documentation:

- [API Reference](api/Message.html) - Detailed API documentation

## Core Classes

| Class | Description |
|-------|-------------|
| `MessageObject` | The main message document object model |
| `MessageParser` | Parses RFC 5322/MIME messages from text |
| `MessageSerializer` | Serializes messages to RFC 5322/MIME format |
| `MessageValidator` | Validates messages against RFC requirements |
| `MimePart` | Represents a MIME part in multipart messages |
| `MailboxAddress` | Represents an email address with optional display name |
| `ContentTypeHeader` | Parsed Content-Type header with parameters |
| `HeaderField` | Individual header field name/value pair |

## Supported Headers

### RFC 5322 Headers

| Header | Property | Description |
|--------|----------|-------------|
| From | `From` | Message originator |
| Sender | `Sender` | Actual sender (if different from From) |
| Reply-To | `ReplyTo` | Reply address |
| To | `To` | Primary recipients |
| Cc | `Cc` | Carbon copy recipients |
| Bcc | `Bcc` | Blind carbon copy recipients |
| Subject | `Subject` | Message topic |
| Date | `Date` | Origination date |
| Message-ID | `MessageId` | Unique message identifier |
| In-Reply-To | `InReplyTo` | Parent message ID |
| References | `References` | Thread references |

### MIME Headers (RFC 2045)

| Header | Property | Description |
|--------|----------|-------------|
| MIME-Version | `MimeVersion` | MIME version (1.0) |
| Content-Type | `ContentType` | Media type of body |
| Content-Transfer-Encoding | `ContentTransferEncoding` | Body encoding |
| Content-ID | `ContentId` | Content identifier |
| Content-Description | `ContentDescription` | Content description |
| Content-Disposition | `ContentDisposition` | Attachment info |

## Content-Transfer-Encoding Support

| Encoding | Status | Description |
|----------|--------|-------------|
| 7bit | ✅ | ASCII text (default) |
| 8bit | ✅ | 8-bit text |
| binary | ✅ | Binary data |
| quoted-printable | ✅ | Encoded text with special chars |
| base64 | ✅ | Binary-to-text encoding |

## Testing

Run tests:

```bash
cd dotnet
dotnet test
```

## Requirements

- .NET 10.0 or .NET Standard 2.1 compatible runtime
- C# 10.0 or later

## Source Code

View the source code on [GitHub](https://github.com/spec-works/message/tree/main/dotnet).

## License

MIT License - see [LICENSE](https://github.com/spec-works/message/blob/main/LICENSE) for details.
