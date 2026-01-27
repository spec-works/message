# message
[![Registry](https://img.shields.io/badge/Registry-SpecWorks-blue)](https://spec-works.github.io/registry/parts/message/)

Software component for parsing, validating, and serializing Internet message format (message/rfc822) data.

## Specification

Implements the Internet Message Format and MIME specifications:

- **[RFC 822](https://www.rfc-editor.org/rfc/rfc822.html)** - Standard for ARPA Internet Text Messages (original, obsoleted by RFC 2822)
- **[RFC 2822](https://www.rfc-editor.org/rfc/rfc2822.html)** - Internet Message Format (obsoletes RFC 822)
- **[RFC 5322](https://www.rfc-editor.org/rfc/rfc5322.html)** - Internet Message Format (current standard, obsoletes RFC 2822)
- **[RFC 2045](https://www.rfc-editor.org/rfc/rfc2045.html)** - MIME Part One: Format of Internet Message Bodies
- **[RFC 2046](https://www.rfc-editor.org/rfc/rfc2046.html)** - MIME Part Two: Media Types
- **[RFC 2047](https://www.rfc-editor.org/rfc/rfc2047.html)** - MIME Part Three: Message Header Extensions for Non-ASCII Text
- **[RFC 2048](https://www.rfc-editor.org/rfc/rfc2048.html)** - MIME Part Four: Registration Procedures
- **[RFC 2049](https://www.rfc-editor.org/rfc/rfc2049.html)** - MIME Part Five: Conformance Criteria and Examples

See [specs.json](specs.json) for complete specification references.

## Architecture

The library is organized around the following concepts:

1. **Headers** - RFC 822/2822/5322 header fields (From, To, Subject, Date, etc.)
2. **Body** - The message body, which may be simple text or MIME-structured
3. **MIME Parts** - Multipart messages with boundaries, content types, and encodings
4. **Content-Transfer-Encoding** - Base64, Quoted-Printable, 7bit, 8bit, binary

## Implementations

- **[.NET Library](dotnet/README.md)** - ![.NET Test](https://github.com/spec-works/message/workflows/.NET%20Test/badge.svg)

## Test Cases

Shared, language-independent test cases are in [testcases/](testcases/).

## License

MIT License
