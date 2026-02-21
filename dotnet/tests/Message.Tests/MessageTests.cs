using System;
using System.Linq;
using Xunit;

namespace Message.Tests
{
    public class MessageParserTests
    {
        private readonly MessageParser _parser = new();

        [Fact]
        public void Parse_SimpleMessage_ReturnsMessageObject()
        {
            var input = @"From: sender@example.com
To: recipient@example.com
Subject: Test message
Date: Mon, 27 Jan 2026 10:00:00 +0000

This is the message body.";

            var message = _parser.Parse(input);

            Assert.Equal("sender@example.com", message.From);
            Assert.Equal("recipient@example.com", message.To);
            Assert.Equal("Test message", message.Subject);
            Assert.Equal("This is the message body.", message.RawBody);
        }

        [Fact]
        public void Parse_FoldedHeader_UnfoldsCorrectly()
        {
            var input = @"From: sender@example.com
To: recipient@example.com
Subject: This is a very long subject line that has been
 folded across multiple lines for readability
Date: Mon, 27 Jan 2026 10:00:00 +0000

Body";

            var message = _parser.Parse(input);

            Assert.Equal("This is a very long subject line that has been folded across multiple lines for readability", message.Subject);
        }

        [Fact]
        public void Parse_MimeMessage_ParsesContentType()
        {
            var input = @"From: sender@example.com
To: recipient@example.com
Subject: MIME Test
Date: Mon, 27 Jan 2026 10:00:00 +0000
MIME-Version: 1.0
Content-Type: text/plain; charset=utf-8

Hello, World!";

            var message = _parser.Parse(input);

            Assert.Equal("1.0", message.MimeVersion);
            Assert.NotNull(message.ContentTypeHeader);
            Assert.Equal("text/plain", message.ContentTypeHeader.MediaType);
            Assert.Equal("utf-8", message.ContentTypeHeader.Charset);
        }

        [Fact]
        public void Parse_MultipartMessage_ParsesParts()
        {
            var input = @"From: sender@example.com
To: recipient@example.com
Subject: Multipart Test
Date: Mon, 27 Jan 2026 10:00:00 +0000
MIME-Version: 1.0
Content-Type: multipart/alternative; boundary=""boundary123""

--boundary123
Content-Type: text/plain

Plain text version
--boundary123
Content-Type: text/html

<html><body>HTML version</body></html>
--boundary123--";

            var message = _parser.Parse(input);

            Assert.True(message.IsMultipart);
            Assert.Equal(2, message.Parts.Count);
            Assert.Equal("text/plain", message.Parts[0].ContentType);
            Assert.Equal("text/html", message.Parts[1].ContentType);
            Assert.Contains("Plain text version", message.Parts[0].Content);
            Assert.Contains("HTML version", message.Parts[1].Content);
        }

        [Fact]
        public void Parse_EncodedWord_DecodesCorrectly()
        {
            var input = @"From: sender@example.com
To: recipient@example.com
Subject: =?UTF-8?B?SGVsbG8gV29ybGQ=?=
Date: Mon, 27 Jan 2026 10:00:00 +0000

Body";

            var message = _parser.Parse(input);

            Assert.Equal("Hello World", message.Subject);
        }

        [Fact]
        public void Parse_Base64Body_AvailableForDecoding()
        {
            var input = @"From: sender@example.com
To: recipient@example.com
Subject: Base64 Test
Date: Mon, 27 Jan 2026 10:00:00 +0000
MIME-Version: 1.0
Content-Type: text/plain
Content-Transfer-Encoding: base64

SGVsbG8gV29ybGQ=";

            var message = _parser.Parse(input);

            Assert.Equal("base64", message.ContentTransferEncoding);
            Assert.Equal("Hello World", message.TextBody);
        }

        [Fact]
        public void Parse_QuotedPrintableBody_AvailableForDecoding()
        {
            var input = @"From: sender@example.com
To: recipient@example.com
Subject: QP Test
Date: Mon, 27 Jan 2026 10:00:00 +0000
MIME-Version: 1.0
Content-Type: text/plain
Content-Transfer-Encoding: quoted-printable

Hello=20World";

            var message = _parser.Parse(input);

            Assert.Equal("quoted-printable", message.ContentTransferEncoding);
            Assert.Equal("Hello World", message.TextBody);
        }

        [Fact]
        public void Parse_EmptyMessage_ThrowsException()
        {
            Assert.Throws<ParseException>(() => _parser.Parse(""));
            Assert.Throws<ParseException>(() => _parser.Parse(null!));
        }

        [Fact]
        public void Parse_InvalidHeaderLine_ThrowsException()
        {
            var input = @"This is not a valid header
From: sender@example.com

Body";

            Assert.Throws<ParseException>(() => _parser.Parse(input));
        }

        [Fact]
        public void Parse_MultipleToAddresses_ParsesAll()
        {
            var input = @"From: sender@example.com
To: alice@example.com, bob@example.com
Subject: Multiple recipients
Date: Mon, 27 Jan 2026 10:00:00 +0000

Body";

            var message = _parser.Parse(input);
            var addresses = MailboxAddress.ParseList(message.To!);

            Assert.Equal(2, addresses.Count);
            Assert.Equal("alice@example.com", addresses[0].Address);
            Assert.Equal("bob@example.com", addresses[1].Address);
        }

        [Fact]
        public void Parse_AddressWithDisplayName_ParsesCorrectly()
        {
            var input = @"From: ""John Doe"" <john@example.com>
To: recipient@example.com
Subject: Display name test
Date: Mon, 27 Jan 2026 10:00:00 +0000

Body";

            var message = _parser.Parse(input);
            var from = MailboxAddress.Parse(message.From!);

            Assert.Equal("John Doe", from.DisplayName);
            Assert.Equal("john@example.com", from.Address);
        }
    }

    public class MessageValidatorTests
    {
        private readonly MessageParser _parser = new();
        private readonly MessageValidator _validator = new();

        [Fact]
        public void Validate_ValidMessage_ReturnsNoErrors()
        {
            var input = @"From: sender@example.com
To: recipient@example.com
Subject: Valid message
Date: Mon, 27 Jan 2026 10:00:00 +0000
Message-ID: <unique-id@example.com>

Body";

            var message = _parser.Parse(input);
            var result = _validator.Validate(message);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_MissingFrom_ReturnsError()
        {
            var message = new MessageObject
            {
                To = "recipient@example.com",
                Subject = "Test",
                DateRaw = "Mon, 27 Jan 2026 10:00:00 +0000"
            };

            var result = _validator.Validate(message);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("From"));
        }

        [Fact]
        public void Validate_MissingDate_ReturnsError()
        {
            var message = new MessageObject
            {
                From = "sender@example.com",
                To = "recipient@example.com",
                Subject = "Test"
            };

            var result = _validator.Validate(message);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Date"));
        }

        [Fact]
        public void Validate_MissingMessageId_ReturnsWarning()
        {
            var input = @"From: sender@example.com
To: recipient@example.com
Subject: No message ID
Date: Mon, 27 Jan 2026 10:00:00 +0000

Body";

            var message = _parser.Parse(input);
            var result = _validator.Validate(message);

            Assert.True(result.IsValid);
            Assert.Contains(result.Warnings, w => w.Contains("Message-ID"));
        }

        [Fact]
        public void Validate_MultipleFromAddresses_RequiresSender()
        {
            var input = @"From: alice@example.com, bob@example.com
To: recipient@example.com
Subject: Multiple from
Date: Mon, 27 Jan 2026 10:00:00 +0000

Body";

            var message = _parser.Parse(input);
            var result = _validator.Validate(message);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Sender") && e.Contains("multiple"));
        }

        [Fact]
        public void Validate_MultipartWithoutBoundary_ReturnsError()
        {
            var message = new MessageObject
            {
                From = "sender@example.com",
                To = "recipient@example.com",
                DateRaw = "Mon, 27 Jan 2026 10:00:00 +0000",
                MimeVersion = "1.0",
                ContentType = "multipart/mixed"
            };

            var result = _validator.Validate(message);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("boundary"));
        }

        [Fact]
        public void Validate_InvalidEmailFormat_ReturnsWarning()
        {
            var input = @"From: invalid-email
To: recipient@example.com
Subject: Invalid from
Date: Mon, 27 Jan 2026 10:00:00 +0000

Body";

            var message = _parser.Parse(input);
            var result = _validator.Validate(message);

            Assert.Contains(result.Warnings, w => w.Contains("Invalid email"));
        }
    }

    public class MessageSerializerTests
    {
        private readonly MessageSerializer _serializer = new();

        [Fact]
        public void Serialize_SimpleMessage_ProducesValidOutput()
        {
            var message = new MessageObject
            {
                From = "sender@example.com",
                To = "recipient@example.com",
                Subject = "Test message",
                Date = DateTimeOffset.Parse("2026-01-27T10:00:00+00:00")
            };
            message.SetTextBody("This is the body.");

            var output = _serializer.Serialize(message);

            Assert.Contains("From: sender@example.com", output);
            Assert.Contains("To: recipient@example.com", output);
            Assert.Contains("Subject: Test message", output);
            Assert.Contains("This is the body.", output);
            Assert.Contains("\r\n\r\n", output); // Blank line before body
        }

        [Fact]
        public void Serialize_MultipartMessage_IncludesBoundary()
        {
            var message = new MessageObject
            {
                From = "sender@example.com",
                To = "recipient@example.com",
                Subject = "Multipart test"
            };
            message.MakeMultipart();
            message.AddPart(new MimePart
            {
                ContentType = "text/plain",
                Content = "Plain text"
            });
            message.AddPart(new MimePart
            {
                ContentType = "text/html",
                Content = "<html>HTML</html>"
            });

            var output = _serializer.Serialize(message);

            Assert.Contains("multipart/mixed", output);
            Assert.Contains("boundary=", output);
            Assert.Contains("text/plain", output);
            Assert.Contains("text/html", output);
            Assert.Contains("Plain text", output);
            Assert.Contains("<html>HTML</html>", output);
        }

        [Fact]
        public void Serialize_ThenParse_RoundTrips()
        {
            var original = new MessageObject
            {
                From = "sender@example.com",
                To = "recipient@example.com",
                Subject = "Round trip test",
                Date = DateTimeOffset.Parse("2026-01-27T10:00:00+00:00"),
                MessageId = "<test-id@example.com>"
            };
            original.MimeVersion = "1.0";
            original.SetTextBody("Hello, World!");

            var serialized = _serializer.Serialize(original);
            var parser = new MessageParser();
            var parsed = parser.Parse(serialized);

            Assert.Equal(original.From, parsed.From);
            Assert.Equal(original.To, parsed.To);
            Assert.Equal(original.Subject, parsed.Subject);
            Assert.Equal(original.MessageId, parsed.MessageId);
            Assert.Equal("Hello, World!", parsed.TextBody);
        }
    }

    public class MailboxAddressTests
    {
        [Fact]
        public void Parse_SimpleAddress_ParsesCorrectly()
        {
            var addr = MailboxAddress.Parse("user@example.com");

            Assert.Equal("user@example.com", addr.Address);
            Assert.Null(addr.DisplayName);
            Assert.Equal("user", addr.LocalPart);
            Assert.Equal("example.com", addr.Domain);
        }

        [Fact]
        public void Parse_AddressWithDisplayName_ParsesCorrectly()
        {
            var addr = MailboxAddress.Parse("John Doe <john@example.com>");

            Assert.Equal("john@example.com", addr.Address);
            Assert.Equal("John Doe", addr.DisplayName);
        }

        [Fact]
        public void Parse_QuotedDisplayName_RemovesQuotes()
        {
            var addr = MailboxAddress.Parse("\"Doe, John\" <john@example.com>");

            Assert.Equal("john@example.com", addr.Address);
            Assert.Equal("Doe, John", addr.DisplayName);
        }

        [Fact]
        public void ParseList_MultipleAddresses_ParsesAll()
        {
            var addresses = MailboxAddress.ParseList("alice@example.com, \"Bob\" <bob@example.com>, charlie@example.com");

            Assert.Equal(3, addresses.Count);
            Assert.Equal("alice@example.com", addresses[0].Address);
            Assert.Equal("bob@example.com", addresses[1].Address);
            Assert.Equal("Bob", addresses[1].DisplayName);
            Assert.Equal("charlie@example.com", addresses[2].Address);
        }

        [Fact]
        public void ToString_SimpleAddress_ReturnsAddress()
        {
            var addr = new MailboxAddress("user@example.com");

            Assert.Equal("user@example.com", addr.ToString());
        }

        [Fact]
        public void ToString_WithDisplayName_ReturnsFormatted()
        {
            var addr = new MailboxAddress("John Doe", "john@example.com");

            Assert.Equal("John Doe <john@example.com>", addr.ToString());
        }
    }

    public class ContentTypeHeaderTests
    {
        [Fact]
        public void Parse_SimpleType_ReturnsMediaType()
        {
            var ct = ContentTypeHeader.Parse("text/plain");

            Assert.Equal("text/plain", ct.MediaType);
            Assert.Empty(ct.Parameters);
        }

        [Fact]
        public void Parse_WithCharset_ExtractsParameter()
        {
            var ct = ContentTypeHeader.Parse("text/plain; charset=utf-8");

            Assert.Equal("text/plain", ct.MediaType);
            Assert.Equal("utf-8", ct.Charset);
        }

        [Fact]
        public void Parse_WithBoundary_ExtractsParameter()
        {
            var ct = ContentTypeHeader.Parse("multipart/mixed; boundary=\"----=_Part_1\"");

            Assert.Equal("multipart/mixed", ct.MediaType);
            Assert.Equal("----=_Part_1", ct.Boundary);
        }

        [Fact]
        public void Parse_MultipleParameters_ExtractsAll()
        {
            var ct = ContentTypeHeader.Parse("text/html; charset=utf-8; name=\"test.html\"");

            Assert.Equal("text/html", ct.MediaType);
            Assert.Equal("utf-8", ct.Charset);
            Assert.Equal("test.html", ct.GetParameter("name"));
        }
    }

    public class AssemblyTests
    {
        [Fact]
        public void Assembly_IsStrongNamed()
        {
            var assembly = typeof(MessageParser).Assembly;
            var publicKeyToken = assembly.GetName().GetPublicKeyToken();
            
            Assert.NotNull(publicKeyToken);
            Assert.NotEmpty(publicKeyToken);
            Assert.True(publicKeyToken.Length > 0, "Assembly should be strong-named");
        }
    }
}
