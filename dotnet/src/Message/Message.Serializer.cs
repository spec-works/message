using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Message
{
    /// <summary>
    /// Serializer for message/rfc822 format (RFC 5322 with MIME support).
    /// </summary>
    public class MessageSerializer
    {
        private const int MaxLineLength = 76; // RFC 5322 recommends 78, we use 76 for safety
        private const string Crlf = "\r\n";

        /// <summary>
        /// Serializes a message to RFC 5322/MIME format.
        /// </summary>
        public string Serialize(MessageObject message)
        {
            var sb = new StringBuilder();

            // Serialize headers
            SerializeHeaders(message, sb);

            // Blank line between headers and body
            sb.Append(Crlf);

            // Serialize body
            SerializeBody(message, sb);

            return sb.ToString();
        }

        /// <summary>
        /// Serializes a message to a stream.
        /// </summary>
        public void SerializeToStream(MessageObject message, Stream stream)
        {
            var text = Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Serializes a message to a file.
        /// </summary>
        public void SerializeToFile(MessageObject message, string filePath)
        {
            var text = Serialize(message);
            File.WriteAllText(filePath, text);
        }

        /// <summary>
        /// Serializes all headers.
        /// </summary>
        private void SerializeHeaders(MessageObject message, StringBuilder sb)
        {
            // Standard header order for readability
            var orderedHeaderNames = new[]
            {
                "Date", "From", "Sender", "Reply-To", "To", "Cc", "Bcc",
                "Message-ID", "In-Reply-To", "References", "Subject",
                "MIME-Version", "Content-Type", "Content-Transfer-Encoding",
                "Content-Disposition", "Content-ID", "Content-Description"
            };

            var serializedHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Serialize headers in preferred order
            foreach (var name in orderedHeaderNames)
            {
                if (message.Headers.TryGetValue(name, out var headers))
                {
                    foreach (var header in headers)
                    {
                        SerializeHeader(header, sb);
                    }
                    serializedHeaders.Add(name);
                }
            }

            // Serialize remaining headers
            foreach (var kvp in message.Headers)
            {
                if (!serializedHeaders.Contains(kvp.Key))
                {
                    foreach (var header in kvp.Value)
                    {
                        SerializeHeader(header, sb);
                    }
                }
            }
        }

        /// <summary>
        /// Serializes a single header with folding.
        /// </summary>
        private void SerializeHeader(HeaderField header, StringBuilder sb)
        {
            var name = header.Name;
            var value = header.Value;

            // Encode non-ASCII characters in header value
            value = EncodeHeaderValue(value, name);

            var line = $"{name}: {value}";

            // Fold long lines (RFC 5322 Section 2.2.3)
            if (line.Length > MaxLineLength)
            {
                sb.Append(FoldHeader(line));
            }
            else
            {
                sb.Append(line);
            }
            sb.Append(Crlf);
        }

        /// <summary>
        /// Folds a long header line at appropriate break points.
        /// </summary>
        private string FoldHeader(string line)
        {
            var result = new StringBuilder();
            var colonIndex = line.IndexOf(':');
            var currentPos = 0;
            var lineStart = 0;

            while (currentPos < line.Length)
            {
                var remaining = line.Length - lineStart;
                var maxLen = lineStart == 0 ? MaxLineLength : MaxLineLength - 1; // Account for folding whitespace

                if (remaining <= maxLen)
                {
                    // Rest of line fits
                    if (lineStart > 0) result.Append(" ");
                    result.Append(line.Substring(lineStart));
                    break;
                }

                // Find a break point
                var breakPoint = FindBreakPoint(line, lineStart, lineStart + maxLen);
                
                if (lineStart > 0) result.Append(" ");
                result.Append(line.Substring(lineStart, breakPoint - lineStart));
                result.Append(Crlf);

                lineStart = breakPoint;
                // Skip whitespace at start of continuation
                while (lineStart < line.Length && (line[lineStart] == ' ' || line[lineStart] == '\t'))
                {
                    lineStart++;
                }
                currentPos = lineStart;
            }

            return result.ToString();
        }

        /// <summary>
        /// Finds an appropriate break point for header folding.
        /// </summary>
        private int FindBreakPoint(string line, int start, int maxEnd)
        {
            // Look for whitespace to break at
            for (int i = maxEnd; i > start; i--)
            {
                if (line[i] == ' ' || line[i] == '\t')
                {
                    return i;
                }
            }

            // Look for punctuation
            for (int i = maxEnd; i > start; i--)
            {
                if (line[i] == ',' || line[i] == ';')
                {
                    return i + 1;
                }
            }

            // Force break at max
            return maxEnd;
        }

        /// <summary>
        /// Encodes header value with RFC 2047 encoded-words if needed.
        /// </summary>
        private string EncodeHeaderValue(string value, string headerName)
        {
            // Check if encoding is needed (non-ASCII characters)
            bool needsEncoding = value.Any(c => c > 127);
            if (!needsEncoding) return value;

            // For structured headers (addresses), only encode the display name
            if (IsAddressHeader(headerName))
            {
                return EncodeAddressHeader(value);
            }

            // For unstructured headers, encode the entire value
            return EncodeText(value);
        }

        /// <summary>
        /// Checks if a header is an address header.
        /// </summary>
        private bool IsAddressHeader(string name)
        {
            var addressHeaders = new[] { "From", "Sender", "Reply-To", "To", "Cc", "Bcc" };
            return addressHeaders.Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Encodes an address header value.
        /// </summary>
        private string EncodeAddressHeader(string value)
        {
            var addresses = MailboxAddress.ParseList(value);
            var encoded = addresses.Select(addr =>
            {
                if (!string.IsNullOrEmpty(addr.DisplayName) && addr.DisplayName.Any(c => c > 127))
                {
                    var encodedName = EncodeText(addr.DisplayName);
                    return $"{encodedName} <{addr.Address}>";
                }
                return addr.ToString();
            });
            return string.Join(", ", encoded);
        }

        /// <summary>
        /// Encodes text using RFC 2047 encoded-word (Base64).
        /// </summary>
        private string EncodeText(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var base64 = Convert.ToBase64String(bytes);
            return $"=?UTF-8?B?{base64}?=";
        }

        /// <summary>
        /// Serializes the message body.
        /// </summary>
        private void SerializeBody(MessageObject message, StringBuilder sb)
        {
            if (message.IsMultipart)
            {
                SerializeMultipartBody(message, sb);
            }
            else if (!string.IsNullOrEmpty(message.RawBody))
            {
                var body = EncodeBody(message.RawBody, message.ContentTransferEncoding);
                sb.Append(body);
            }
        }

        /// <summary>
        /// Serializes a multipart message body.
        /// </summary>
        private void SerializeMultipartBody(MessageObject message, StringBuilder sb)
        {
            var boundary = message.Boundary;
            if (string.IsNullOrEmpty(boundary))
            {
                throw new InvalidOperationException("Multipart message must have a boundary");
            }

            // Preamble (optional, usually empty)
            sb.Append("This is a multi-part message in MIME format.");
            sb.Append(Crlf);
            sb.Append(Crlf);

            // Serialize each part
            foreach (var part in message.Parts)
            {
                sb.Append("--");
                sb.Append(boundary);
                sb.Append(Crlf);

                SerializeMimePart(part, sb);
            }

            // Closing boundary
            sb.Append("--");
            sb.Append(boundary);
            sb.Append("--");
            sb.Append(Crlf);
        }

        /// <summary>
        /// Serializes a single MIME part.
        /// </summary>
        private void SerializeMimePart(MimePart part, StringBuilder sb)
        {
            // Serialize part headers
            foreach (var kvp in part.Headers)
            {
                foreach (var header in kvp.Value)
                {
                    SerializeHeader(header, sb);
                }
            }

            // Blank line between headers and content
            sb.Append(Crlf);

            // Serialize content
            if (part.IsMultipart && part.Parts.Count > 0)
            {
                // Nested multipart
                var boundary = part.ContentTypeHeader?.Boundary;
                if (!string.IsNullOrEmpty(boundary))
                {
                    foreach (var nestedPart in part.Parts)
                    {
                        sb.Append("--");
                        sb.Append(boundary);
                        sb.Append(Crlf);
                        SerializeMimePart(nestedPart, sb);
                    }
                    sb.Append("--");
                    sb.Append(boundary);
                    sb.Append("--");
                    sb.Append(Crlf);
                }
            }
            else if (!string.IsNullOrEmpty(part.Content))
            {
                var content = EncodeBody(part.Content, part.ContentTransferEncoding);
                sb.Append(content);
            }
            else if (part.BinaryContent != null)
            {
                var base64 = Convert.ToBase64String(part.BinaryContent);
                sb.Append(WrapBase64(base64));
            }

            sb.Append(Crlf);
        }

        /// <summary>
        /// Encodes body content based on Content-Transfer-Encoding.
        /// </summary>
        private string EncodeBody(string body, string? encoding)
        {
            if (string.IsNullOrEmpty(encoding)) return body;

            return encoding.ToLowerInvariant() switch
            {
                "base64" => WrapBase64(Convert.ToBase64String(Encoding.UTF8.GetBytes(body))),
                "quoted-printable" => EncodeQuotedPrintable(body),
                _ => body
            };
        }

        /// <summary>
        /// Wraps Base64 content at 76 characters per line.
        /// </summary>
        private string WrapBase64(string base64)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < base64.Length; i += 76)
            {
                var len = Math.Min(76, base64.Length - i);
                sb.Append(base64.Substring(i, len));
                if (i + len < base64.Length)
                {
                    sb.Append(Crlf);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Encodes content as Quoted-Printable.
        /// </summary>
        private string EncodeQuotedPrintable(string text)
        {
            var sb = new StringBuilder();
            var lineLen = 0;

            foreach (char c in text)
            {
                string encoded;

                if (c == '\r' || c == '\n')
                {
                    // Hard line breaks
                    sb.Append(c);
                    if (c == '\n') lineLen = 0;
                    continue;
                }
                else if (c >= 33 && c <= 126 && c != '=')
                {
                    // Printable ASCII (except =)
                    encoded = c.ToString();
                }
                else if (c == ' ' || c == '\t')
                {
                    // Whitespace - encode if at end of line
                    encoded = c.ToString();
                }
                else
                {
                    // Encode as =XX
                    encoded = $"={((int)c):X2}";
                }

                // Check if we need a soft line break
                if (lineLen + encoded.Length > 75)
                {
                    sb.Append("=");
                    sb.Append(Crlf);
                    lineLen = 0;
                }

                sb.Append(encoded);
                lineLen += encoded.Length;
            }

            return sb.ToString();
        }
    }
}
