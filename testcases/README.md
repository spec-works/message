# Internet Message Format Test Cases

Shared, language-independent test cases for the message component.

## Format

Each test case is a JSON file containing an `input` field with a raw RFC 5322
message string and fields describing the expected parse result.

### Test Record Structure

```json
{
  "name": "test-identifier",
  "description": "Human-readable description with RFC reference",
  "input": "From: ...\r\n\r\nBody",
  "expectedHeaders": { "From": "...", "To": "..." },
  "expectedBody": "...",
  "isMultipart": false,
  "isValid": true
}
```

### Optional Fields

| Field | Description |
|-------|-------------|
| `expectedDecodedSubject` | Subject after RFC 2047 encoded-word decoding |
| `expectedRawBody` | Raw body before content-transfer-encoding decoding |
| `expectedDecodedBody` | Body after base64 or quoted-printable decoding |
| `expectedContentType` | Parsed Content-Type with `mediaType`, `charset`, `parameters` |
| `expectedRecipientCount` | Number of addresses in To header |
| `expectedRecipients` | Array of individual email addresses |
| `expectedFromAddress` | Parsed From with `displayName` and `address` |
| `expectedToAddress` | Parsed To with `displayName` and `address` |
| `expectedPartCount` | Number of MIME parts in multipart message |
| `expectedParts` | Array describing each MIME part |
| `expectedError` | Description of expected validation error (negative tests) |

## Positive Tests

### Basic Messages

| File | RFC | Description |
|------|-----|-------------|
| `simple-message.json` | 5322 | Minimal text email with required headers |
| `folded-headers.json` | 5322 §2.2.3 | Multi-line header folding/unfolding |
| `cc-bcc-reply-to.json` | 5322 §3.6.3 | Cc, Bcc, and Reply-To address headers |
| `multiple-from-with-sender.json` | 5322 §3.6.2 | Multiple From addresses with required Sender |

### Address Handling

| File | RFC | Description |
|------|-----|-------------|
| `multiple-recipients.json` | 5322 §3.4 | Comma-separated To addresses |
| `display-names.json` | 5322 §3.4 | Addresses with quoted display names |

### MIME (RFC 2045-2049)

| File | RFC | Description |
|------|-----|-------------|
| `mime-headers.json` | 2045 | MIME-Version and Content-Type with charset |
| `content-type-parameters.json` | 2045 §5.1 | Multiple Content-Type parameters |
| `encoded-word-subject.json` | 2047 | Base64 encoded-word in Subject |
| `base64-body.json` | 2045 §6.8 | Base64 Content-Transfer-Encoding |
| `quoted-printable-body.json` | 2045 §6.7 | Quoted-printable encoding with UTF-8 |

### Multipart Messages

| File | RFC | Description |
|------|-----|-------------|
| `multipart-message.json` | 2046 §5.1.4 | Multipart/alternative with text and HTML |
| `multipart-mixed.json` | 2046 §5.1.3 | Multipart/mixed with text body and attachment |
| `nested-multipart.json` | 2046 §5.1 | Nested multipart/mixed containing multipart/alternative |

## Negative Tests

Files in `negative/` are invalid messages that validators SHOULD reject.

| File | RFC | Description |
|------|-----|-------------|
| `missing-from.json` | 5322 §3.6 | Missing required From header |
| `missing-date.json` | 5322 §3.6 | Missing required Date header |
| `invalid-header-format.json` | 5322 §2.2 | Malformed header line (no colon separator) |
| `multipart-no-boundary.json` | 2046 §5.1.1 | Multipart Content-Type without boundary parameter |
| `empty-message.json` | 5322 | Empty input |
| `multiple-from-no-sender.json` | 5322 §3.6.2 | Multiple From without required Sender |

## Usage

These test cases are designed to be language-agnostic and can be used to
validate Internet message parsers across multiple implementations.

### Current Usage
- **.NET**: `dotnet/tests/Message.Tests/MessageTests.cs`

## Reference

- RFC 5322: Internet Message Format
- RFC 2045: MIME Part One: Format of Internet Message Bodies
- RFC 2046: MIME Part Two: Media Types
- RFC 2047: MIME Part Three: Message Header Extensions for Non-ASCII Text
- RFC 2049: MIME Part Five: Conformance Criteria and Examples
