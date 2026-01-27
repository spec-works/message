# Message Documentation

Welcome to the Message documentation. This component provides libraries for parsing, validating, and serializing Internet message format (message/rfc822) data according to [RFC 5322](https://www.rfc-editor.org/rfc/rfc5322) and MIME ([RFC 2045](https://www.rfc-editor.org/rfc/rfc2045)-[2049](https://www.rfc-editor.org/rfc/rfc2049)).

## What is message/rfc822?

The `message/rfc822` media type represents Internet email messages. The format is defined by a family of specifications:

- **RFC 5322** - Internet Message Format (headers like From, To, Subject, Date)
- **RFC 2045-2049** - MIME (Multipurpose Internet Mail Extensions) for attachments, HTML, and non-ASCII content

This library provides complete support for:

- Email header parsing and generation
- MIME multipart message handling
- Content-Transfer-Encoding (Base64, Quoted-Printable)
- RFC 2047 encoded-words for non-ASCII in headers
- Message validation against RFC requirements

## Available Implementations

Choose the implementation that matches your technology stack:

### [.NET](dotnet/index.md)

Full-featured .NET library for RFC 5322 + MIME parsing and generation.

- **Package**: [SpecWorks.Message](https://www.nuget.org/packages/SpecWorks.Message) on NuGet
- **Target Frameworks**: .NET 10.0, .NET Standard 2.1
- **Language**: C# with nullable reference types
- [View .NET Documentation →](dotnet/index.md)

## Quick Start

### .NET

```bash
dotnet add package SpecWorks.Message
```

```csharp
using Message;

// Parse an email message
var parser = new MessageParser();
var message = parser.Parse(emailText);

Console.WriteLine($"From: {message.From}");
Console.WriteLine($"To: {message.To}");
Console.WriteLine($"Subject: {message.Subject}");
Console.WriteLine($"Body: {message.TextBody}");

// Handle multipart messages
if (message.IsMultipart)
{
    foreach (var part in message.Parts)
    {
        Console.WriteLine($"Part: {part.ContentType}");
    }
}
```

## Specification Compliance

All implementations follow these IETF specifications:

| Specification | Description | Status |
|---------------|-------------|--------|
| [RFC 5322](https://www.rfc-editor.org/rfc/rfc5322) | Internet Message Format | ✅ Implemented |
| [RFC 2045](https://www.rfc-editor.org/rfc/rfc2045) | MIME Part One: Format of Internet Message Bodies | ✅ Implemented |
| [RFC 2046](https://www.rfc-editor.org/rfc/rfc2046) | MIME Part Two: Media Types | ✅ Implemented |
| [RFC 2047](https://www.rfc-editor.org/rfc/rfc2047) | MIME Part Three: Non-ASCII Text in Headers | ✅ Implemented |
| [RFC 2822](https://www.rfc-editor.org/rfc/rfc2822) | Internet Message Format (obsoleted by 5322) | ✅ Compatible |
| [RFC 822](https://www.rfc-editor.org/rfc/rfc822) | Original message format | ✅ Compatible |

## Test Cases

All implementations are tested against shared test cases in the [testcases/](https://github.com/spec-works/message/tree/main/testcases) directory.

## Contributing

Contributions are welcome! See the [GitHub repository](https://github.com/spec-works/message) for:

- Issue tracking
- Pull request guidelines
- Development setup instructions

## License

All Message implementations are licensed under the [MIT License](https://github.com/spec-works/message/blob/main/LICENSE).

## Links

- **GitHub Repository**: [github.com/spec-works/message](https://github.com/spec-works/message)
- **RFC 5322 Specification**: [rfc-editor.org/rfc/rfc5322](https://www.rfc-editor.org/rfc/rfc5322)
- **SpecWorks Factory**: [spec-works.github.io](https://spec-works.github.io)
