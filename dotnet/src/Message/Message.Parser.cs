using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Message
{
    /// <summary>
    /// Parser for message/rfc822 format (RFC 5322 with MIME support from RFC 2045-2049).
    /// </summary>
    public class MessageParser
    {
        /// <summary>
        /// Parses a message from a string.
        /// </summary>
        public MessageObject Parse(string messageText)
        {
            if (string.IsNullOrEmpty(messageText))
            {
                throw new ParseException("Message text cannot be null or empty");
            }

            var message = new MessageObject();

            // Split into headers and body at the first blank line
            var (headerSection, bodySection) = SplitHeadersAndBody(messageText);

            // Parse headers
            ParseHeaders(headerSection, message);

            // Parse body (including MIME if applicable)
            if (!string.IsNullOrEmpty(bodySection))
            {
                ParseBody(bodySection, message);
            }

            return message;
        }

        /// <summary>
        /// Parses a message from a file.
        /// </summary>
        public MessageObject ParseFile(string filePath)
        {
            var content = File.ReadAllText(filePath);
            return Parse(content);
        }

        /// <summary>
        /// Parses a message from a stream.
        /// </summary>
        public MessageObject ParseStream(Stream stream)
        {
            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            return Parse(content);
        }

        /// <summary>
        /// Splits the message into header section and body section.
        /// The boundary is a blank line (CRLF CRLF or LF LF).
        /// </summary>
        private (string headers, string body) SplitHeadersAndBody(string messageText)
        {
            // Look for blank line (RFC 5322 Section 2.1)
            // Try CRLF first, then LF
            int blankLineIndex = messageText.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (blankLineIndex >= 0)
            {
                return (
                    messageText.Substring(0, blankLineIndex),
                    messageText.Substring(blankLineIndex + 4)
                );
            }

            blankLineIndex = messageText.IndexOf("\n\n", StringComparison.Ordinal);
            if (blankLineIndex >= 0)
            {
                return (
                    messageText.Substring(0, blankLineIndex),
                    messageText.Substring(blankLineIndex + 2)
                );
            }

            // No body - entire message is headers
            return (messageText, string.Empty);
        }

        /// <summary>
        /// Parses the header section into header fields.
        /// Handles header folding (RFC 5322 Section 2.2.3).
        /// </summary>
        private void ParseHeaders(string headerSection, MessageObject message)
        {
            var unfoldedLines = UnfoldHeaders(headerSection);

            foreach (var line in unfoldedLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var colonIndex = line.IndexOf(':');
                if (colonIndex <= 0)
                {
                    throw new ParseException($"Invalid header line (missing colon): {line}");
                }

                var name = line.Substring(0, colonIndex).Trim();
                var value = line.Substring(colonIndex + 1).Trim();

                // Validate header name (RFC 5322 Section 2.2)
                if (!IsValidHeaderName(name))
                {
                    throw new ParseException($"Invalid header field name: {name}");
                }

                // Decode RFC 2047 encoded words if present
                value = DecodeEncodedWords(value);

                message.AddHeader(name, value);
            }
        }

        /// <summary>
        /// Unfolds header lines (RFC 5322 Section 2.2.3).
        /// Lines beginning with whitespace are continuations of the previous line.
        /// </summary>
        private List<string> UnfoldHeaders(string headerSection)
        {
            var result = new List<string>();
            var lines = headerSection.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var currentLine = new StringBuilder();

            foreach (var line in lines)
            {
                if (line.Length > 0 && (line[0] == ' ' || line[0] == '\t'))
                {
                    // Continuation line - append with single space
                    if (currentLine.Length > 0)
                    {
                        currentLine.Append(' ');
                        currentLine.Append(line.TrimStart());
                    }
                }
                else
                {
                    // New header line
                    if (currentLine.Length > 0)
                    {
                        result.Add(currentLine.ToString());
                    }
                    currentLine = new StringBuilder(line);
                }
            }

            if (currentLine.Length > 0)
            {
                result.Add(currentLine.ToString());
            }

            return result;
        }

        /// <summary>
        /// Validates a header field name per RFC 5322 Section 2.2.
        /// Field names must be printable US-ASCII characters except colon.
        /// </summary>
        private bool IsValidHeaderName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            foreach (char c in name)
            {
                // Printable US-ASCII (33-126) except colon (58)
                if (c < 33 || c > 126 || c == ':')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Decodes RFC 2047 encoded-word syntax in header values.
        /// Format: =?charset?encoding?encoded_text?=
        /// </summary>
        private string DecodeEncodedWords(string value)
        {
            if (!value.Contains("=?")) return value;

            var result = new StringBuilder();
            int i = 0;

            while (i < value.Length)
            {
                var start = value.IndexOf("=?", i, StringComparison.Ordinal);
                if (start < 0)
                {
                    result.Append(value.Substring(i));
                    break;
                }

                // Append text before encoded word
                if (start > i)
                {
                    result.Append(value.Substring(i, start - i));
                }

                var end = value.IndexOf("?=", start + 2, StringComparison.Ordinal);
                if (end < 0)
                {
                    result.Append(value.Substring(i));
                    break;
                }

                var encodedWord = value.Substring(start, end - start + 2);
                var decoded = DecodeEncodedWord(encodedWord);
                result.Append(decoded);

                i = end + 2;

                // Skip whitespace between consecutive encoded words (RFC 2047 Section 6.2)
                while (i < value.Length && (value[i] == ' ' || value[i] == '\t'))
                {
                    if (i + 2 < value.Length && value.Substring(i + 1).TrimStart().StartsWith("=?"))
                    {
                        i++;
                        while (i < value.Length && (value[i] == ' ' || value[i] == '\t'))
                        {
                            i++;
                        }
                        break;
                    }
                    break;
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Decodes a single RFC 2047 encoded-word.
        /// </summary>
        private string DecodeEncodedWord(string encodedWord)
        {
            // Format: =?charset?encoding?encoded_text?=
            if (!encodedWord.StartsWith("=?") || !encodedWord.EndsWith("?="))
            {
                return encodedWord;
            }

            var inner = encodedWord.Substring(2, encodedWord.Length - 4);
            var parts = inner.Split('?');
            if (parts.Length != 3)
            {
                return encodedWord;
            }

            var charset = parts[0];
            var encoding = parts[1].ToUpperInvariant();
            var encodedText = parts[2];

            try
            {
                Encoding textEncoding;
                try
                {
                    textEncoding = Encoding.GetEncoding(charset);
                }
                catch
                {
                    textEncoding = Encoding.UTF8;
                }

                byte[] bytes;
                if (encoding == "B")
                {
                    // Base64
                    bytes = Convert.FromBase64String(encodedText);
                }
                else if (encoding == "Q")
                {
                    // Quoted-Printable (with underscores as spaces)
                    bytes = DecodeQEncoding(encodedText);
                }
                else
                {
                    return encodedWord;
                }

                return textEncoding.GetString(bytes);
            }
            catch
            {
                return encodedWord;
            }
        }

        /// <summary>
        /// Decodes Q-encoding (RFC 2047 quoted-printable variant).
        /// </summary>
        private byte[] DecodeQEncoding(string encoded)
        {
            var result = new List<byte>();

            for (int i = 0; i < encoded.Length; i++)
            {
                if (encoded[i] == '_')
                {
                    // Underscore represents space
                    result.Add((byte)' ');
                }
                else if (encoded[i] == '=' && i + 2 < encoded.Length)
                {
                    var hex = encoded.Substring(i + 1, 2);
                    if (byte.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out byte b))
                    {
                        result.Add(b);
                        i += 2;
                    }
                    else
                    {
                        result.Add((byte)encoded[i]);
                    }
                }
                else
                {
                    result.Add((byte)encoded[i]);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Parses the message body, handling MIME multipart if applicable.
        /// </summary>
        private void ParseBody(string bodySection, MessageObject message)
        {
            message.RawBody = bodySection;

            // Check if this is a multipart message
            if (message.IsMultipart && !string.IsNullOrEmpty(message.Boundary))
            {
                ParseMultipartBody(bodySection, message.Boundary, message);
            }
        }

        /// <summary>
        /// Parses a multipart body into MIME parts.
        /// </summary>
        private void ParseMultipartBody(string body, string boundary, MessageObject message)
        {
            var parts = SplitMultipartBody(body, boundary);

            foreach (var partText in parts)
            {
                if (string.IsNullOrWhiteSpace(partText)) continue;

                var part = ParseMimePart(partText);
                message.AddPart(part);
            }
        }

        /// <summary>
        /// Splits a multipart body into individual parts.
        /// </summary>
        private List<string> SplitMultipartBody(string body, string boundary)
        {
            var parts = new List<string>();
            var delimiter = "--" + boundary;
            var closeDelimiter = delimiter + "--";

            // Find each part between delimiters
            int start = body.IndexOf(delimiter, StringComparison.Ordinal);
            if (start < 0) return parts;

            start = SkipToNextLine(body, start + delimiter.Length);

            while (start < body.Length)
            {
                int end = body.IndexOf(delimiter, start, StringComparison.Ordinal);
                if (end < 0)
                {
                    break;
                }

                // Extract part content (trim trailing CRLF before delimiter)
                var partContent = body.Substring(start, end - start);
                if (partContent.EndsWith("\r\n"))
                {
                    partContent = partContent.Substring(0, partContent.Length - 2);
                }
                else if (partContent.EndsWith("\n"))
                {
                    partContent = partContent.Substring(0, partContent.Length - 1);
                }

                parts.Add(partContent);

                // Check for closing delimiter
                if (body.Substring(end).StartsWith(closeDelimiter, StringComparison.Ordinal))
                {
                    break;
                }

                start = SkipToNextLine(body, end + delimiter.Length);
            }

            return parts;
        }

        /// <summary>
        /// Skips to the beginning of the next line.
        /// </summary>
        private int SkipToNextLine(string text, int position)
        {
            while (position < text.Length)
            {
                if (text[position] == '\n')
                {
                    return position + 1;
                }
                position++;
            }
            return position;
        }

        /// <summary>
        /// Parses a single MIME part.
        /// </summary>
        private MimePart ParseMimePart(string partText)
        {
            var part = new MimePart();

            // Split headers and content
            var (headerSection, contentSection) = SplitHeadersAndBody(partText);

            // Parse part headers
            var headerLines = UnfoldHeaders(headerSection);
            foreach (var line in headerLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    var name = line.Substring(0, colonIndex).Trim();
                    var value = line.Substring(colonIndex + 1).Trim();
                    value = DecodeEncodedWords(value);
                    part.AddHeader(name, value);
                }
            }

            part.Content = contentSection;

            // Check for nested multipart
            if (part.IsMultipart)
            {
                var boundary = part.ContentTypeHeader?.Boundary;
                if (!string.IsNullOrEmpty(boundary))
                {
                    var nestedParts = SplitMultipartBody(contentSection, boundary);
                    foreach (var nestedText in nestedParts)
                    {
                        if (!string.IsNullOrWhiteSpace(nestedText))
                        {
                            part.Parts.Add(ParseMimePart(nestedText));
                        }
                    }
                }
            }

            // Detect attachments
            var disposition = part.ContentDisposition;
            if (disposition != null)
            {
                part.IsAttachment = disposition.StartsWith("attachment", StringComparison.OrdinalIgnoreCase);
                var filenameMatch = System.Text.RegularExpressions.Regex.Match(
                    disposition, @"filename\s*=\s*""?([^"";\s]+)""?", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (filenameMatch.Success)
                {
                    part.Filename = filenameMatch.Groups[1].Value;
                }
            }

            // Also check Content-Type name parameter for filename
            if (string.IsNullOrEmpty(part.Filename))
            {
                part.Filename = part.ContentTypeHeader?.GetParameter("name");
            }

            return part;
        }
    }

    /// <summary>
    /// Exception thrown when parsing fails.
    /// </summary>
    public class ParseException : Exception
    {
        public ParseException(string message) : base(message) { }
        public ParseException(string message, Exception innerException) : base(message, innerException) { }
    }
}
