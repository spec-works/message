using System;
using System.Collections.Generic;
using System.Linq;

namespace Message
{
    /// <summary>
    /// Represents an Internet message (RFC 5322) with optional MIME content (RFC 2045-2049).
    /// This is the root document object model for message/rfc822 content.
    /// </summary>
    public class MessageObject
    {
        private readonly Dictionary<string, List<HeaderField>> _headers = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<MimePart> _parts = new();
        private string? _rawBody;

        /// <summary>
        /// Gets or sets all header fields in the message.
        /// </summary>
        public IReadOnlyDictionary<string, List<HeaderField>> Headers => _headers;

        /// <summary>
        /// Gets the MIME parts for multipart messages.
        /// </summary>
        public IReadOnlyList<MimePart> Parts => _parts;

        /// <summary>
        /// Gets or sets the raw body content (before MIME parsing).
        /// </summary>
        public string? RawBody
        {
            get => _rawBody;
            set => _rawBody = value;
        }

        #region Standard Header Properties (RFC 5322)

        /// <summary>
        /// The "From:" field (RFC 5322 Section 3.6.2) - Originator of the message.
        /// </summary>
        public string? From
        {
            get => GetHeaderValue("From");
            set => SetHeader("From", value);
        }

        /// <summary>
        /// The "Sender:" field (RFC 5322 Section 3.6.2) - Actual sender if different from From.
        /// </summary>
        public string? Sender
        {
            get => GetHeaderValue("Sender");
            set => SetHeader("Sender", value);
        }

        /// <summary>
        /// The "Reply-To:" field (RFC 5322 Section 3.6.2) - Reply address.
        /// </summary>
        public string? ReplyTo
        {
            get => GetHeaderValue("Reply-To");
            set => SetHeader("Reply-To", value);
        }

        /// <summary>
        /// The "To:" field (RFC 5322 Section 3.6.3) - Primary recipients.
        /// </summary>
        public string? To
        {
            get => GetHeaderValue("To");
            set => SetHeader("To", value);
        }

        /// <summary>
        /// The "Cc:" field (RFC 5322 Section 3.6.3) - Carbon copy recipients.
        /// </summary>
        public string? Cc
        {
            get => GetHeaderValue("Cc");
            set => SetHeader("Cc", value);
        }

        /// <summary>
        /// The "Bcc:" field (RFC 5322 Section 3.6.3) - Blind carbon copy recipients.
        /// </summary>
        public string? Bcc
        {
            get => GetHeaderValue("Bcc");
            set => SetHeader("Bcc", value);
        }

        /// <summary>
        /// The "Message-ID:" field (RFC 5322 Section 3.6.4) - Unique message identifier.
        /// </summary>
        public string? MessageId
        {
            get => GetHeaderValue("Message-ID");
            set => SetHeader("Message-ID", value);
        }

        /// <summary>
        /// The "In-Reply-To:" field (RFC 5322 Section 3.6.4) - Parent message ID.
        /// </summary>
        public string? InReplyTo
        {
            get => GetHeaderValue("In-Reply-To");
            set => SetHeader("In-Reply-To", value);
        }

        /// <summary>
        /// The "References:" field (RFC 5322 Section 3.6.4) - Thread references.
        /// </summary>
        public string? References
        {
            get => GetHeaderValue("References");
            set => SetHeader("References", value);
        }

        /// <summary>
        /// The "Subject:" field (RFC 5322 Section 3.6.5) - Message topic.
        /// </summary>
        public string? Subject
        {
            get => GetHeaderValue("Subject");
            set => SetHeader("Subject", value);
        }

        /// <summary>
        /// The "Comments:" field (RFC 5322 Section 3.6.5) - Additional comments.
        /// </summary>
        public string? Comments
        {
            get => GetHeaderValue("Comments");
            set => SetHeader("Comments", value);
        }

        /// <summary>
        /// The "Keywords:" field (RFC 5322 Section 3.6.5) - Message keywords.
        /// </summary>
        public string? Keywords
        {
            get => GetHeaderValue("Keywords");
            set => SetHeader("Keywords", value);
        }

        /// <summary>
        /// The "Date:" field (RFC 5322 Section 3.6.1) - Origination date.
        /// </summary>
        public DateTimeOffset? Date
        {
            get
            {
                var value = GetHeaderValue("Date");
                if (value == null) return null;
                return DateTimeOffset.TryParse(value, out var result) ? result : null;
            }
            set => SetHeader("Date", value?.ToString("ddd, dd MMM yyyy HH:mm:ss zzz"));
        }

        /// <summary>
        /// Gets or sets the raw Date header value.
        /// </summary>
        public string? DateRaw
        {
            get => GetHeaderValue("Date");
            set => SetHeader("Date", value);
        }

        #endregion

        #region MIME Header Properties (RFC 2045)

        /// <summary>
        /// The "MIME-Version:" header (RFC 2045 Section 4) - MIME version.
        /// </summary>
        public string? MimeVersion
        {
            get => GetHeaderValue("MIME-Version");
            set => SetHeader("MIME-Version", value);
        }

        /// <summary>
        /// The "Content-Type:" header (RFC 2045 Section 5) - Media type of the body.
        /// </summary>
        public string? ContentType
        {
            get => GetHeaderValue("Content-Type");
            set => SetHeader("Content-Type", value);
        }

        /// <summary>
        /// Gets the parsed Content-Type as a structured object.
        /// </summary>
        public ContentTypeHeader? ContentTypeHeader
        {
            get
            {
                var value = ContentType;
                return value != null ? ContentTypeHeader.Parse(value) : null;
            }
        }

        /// <summary>
        /// The "Content-Transfer-Encoding:" header (RFC 2045 Section 6).
        /// </summary>
        public string? ContentTransferEncoding
        {
            get => GetHeaderValue("Content-Transfer-Encoding");
            set => SetHeader("Content-Transfer-Encoding", value);
        }

        /// <summary>
        /// The "Content-ID:" header (RFC 2045 Section 7).
        /// </summary>
        public string? ContentId
        {
            get => GetHeaderValue("Content-ID");
            set => SetHeader("Content-ID", value);
        }

        /// <summary>
        /// The "Content-Description:" header (RFC 2045 Section 8).
        /// </summary>
        public string? ContentDescription
        {
            get => GetHeaderValue("Content-Description");
            set => SetHeader("Content-Description", value);
        }

        /// <summary>
        /// The "Content-Disposition:" header (RFC 2183).
        /// </summary>
        public string? ContentDisposition
        {
            get => GetHeaderValue("Content-Disposition");
            set => SetHeader("Content-Disposition", value);
        }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Returns true if this message has MIME content.
        /// </summary>
        public bool IsMime => !string.IsNullOrEmpty(MimeVersion);

        /// <summary>
        /// Returns true if this message is a multipart message.
        /// </summary>
        public bool IsMultipart
        {
            get
            {
                var ct = ContentTypeHeader;
                return ct?.MediaType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase) == true;
            }
        }

        /// <summary>
        /// Gets the multipart boundary from Content-Type, if present.
        /// </summary>
        public string? Boundary => ContentTypeHeader?.GetParameter("boundary");

        /// <summary>
        /// Gets the text body content (decoded if necessary).
        /// For multipart messages, returns the first text/plain part.
        /// </summary>
        public string? TextBody
        {
            get
            {
                if (IsMultipart)
                {
                    var textPart = _parts.FirstOrDefault(p =>
                        p.ContentTypeHeader?.MediaType.Equals("text/plain", StringComparison.OrdinalIgnoreCase) == true);
                    return textPart?.DecodedContent;
                }
                return DecodeBody(_rawBody, ContentTransferEncoding);
            }
        }

        /// <summary>
        /// Gets the HTML body content (decoded if necessary).
        /// For multipart messages, returns the first text/html part.
        /// </summary>
        public string? HtmlBody
        {
            get
            {
                if (IsMultipart)
                {
                    var htmlPart = _parts.FirstOrDefault(p =>
                        p.ContentTypeHeader?.MediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase) == true);
                    return htmlPart?.DecodedContent;
                }

                var ct = ContentTypeHeader;
                if (ct?.MediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return DecodeBody(_rawBody, ContentTransferEncoding);
                }
                return null;
            }
        }

        #endregion

        #region Header Management

        /// <summary>
        /// Adds a header field to the message.
        /// </summary>
        public void AddHeader(string name, string value)
        {
            var field = new HeaderField(name, value);
            if (!_headers.TryGetValue(name, out var list))
            {
                list = new List<HeaderField>();
                _headers[name] = list;
            }
            list.Add(field);
        }

        /// <summary>
        /// Sets a header field, replacing any existing values.
        /// </summary>
        public void SetHeader(string name, string? value)
        {
            if (value == null)
            {
                _headers.Remove(name);
            }
            else
            {
                _headers[name] = new List<HeaderField> { new HeaderField(name, value) };
            }
        }

        /// <summary>
        /// Gets the first value for a header field.
        /// </summary>
        public string? GetHeaderValue(string name)
        {
            return _headers.TryGetValue(name, out var list) ? list.FirstOrDefault()?.Value : null;
        }

        /// <summary>
        /// Gets all values for a header field.
        /// </summary>
        public IEnumerable<string> GetHeaderValues(string name)
        {
            return _headers.TryGetValue(name, out var list)
                ? list.Select(h => h.Value)
                : Enumerable.Empty<string>();
        }

        /// <summary>
        /// Gets all header field objects for a header name.
        /// </summary>
        public IEnumerable<HeaderField> GetHeaders(string name)
        {
            return _headers.TryGetValue(name, out var list)
                ? list
                : Enumerable.Empty<HeaderField>();
        }

        /// <summary>
        /// Gets all header fields in the message.
        /// </summary>
        public IEnumerable<HeaderField> GetAllHeaders()
        {
            return _headers.SelectMany(kvp => kvp.Value);
        }

        #endregion

        #region Part Management

        /// <summary>
        /// Adds a MIME part to the message.
        /// </summary>
        public void AddPart(MimePart part)
        {
            _parts.Add(part);
        }

        /// <summary>
        /// Sets the text body of the message.
        /// </summary>
        public void SetTextBody(string text, string charset = "utf-8")
        {
            if (IsMultipart)
            {
                var existingText = _parts.FirstOrDefault(p =>
                    p.ContentTypeHeader?.MediaType.Equals("text/plain", StringComparison.OrdinalIgnoreCase) == true);
                if (existingText != null)
                {
                    existingText.Content = text;
                }
                else
                {
                    AddPart(new MimePart
                    {
                        ContentType = $"text/plain; charset={charset}",
                        Content = text
                    });
                }
            }
            else
            {
                ContentType = $"text/plain; charset={charset}";
                _rawBody = text;
            }
        }

        /// <summary>
        /// Sets the HTML body of the message.
        /// </summary>
        public void SetHtmlBody(string html, string charset = "utf-8")
        {
            if (IsMultipart)
            {
                var existingHtml = _parts.FirstOrDefault(p =>
                    p.ContentTypeHeader?.MediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase) == true);
                if (existingHtml != null)
                {
                    existingHtml.Content = html;
                }
                else
                {
                    AddPart(new MimePart
                    {
                        ContentType = $"text/html; charset={charset}",
                        Content = html
                    });
                }
            }
            else
            {
                ContentType = $"text/html; charset={charset}";
                _rawBody = html;
            }
        }

        /// <summary>
        /// Adds an attachment to the message.
        /// </summary>
        public void AddAttachment(string filename, byte[] content, string contentType)
        {
            if (!IsMultipart)
            {
                // Convert to multipart
                MakeMultipart();
            }

            AddPart(new MimePart
            {
                ContentType = contentType,
                ContentDisposition = $"attachment; filename=\"{filename}\"",
                ContentTransferEncoding = "base64",
                Content = Convert.ToBase64String(content),
                IsAttachment = true,
                Filename = filename
            });
        }

        /// <summary>
        /// Converts this message to a multipart/mixed message.
        /// </summary>
        public void MakeMultipart(string subtype = "mixed")
        {
            if (IsMultipart) return;

            var boundary = GenerateBoundary();
            var originalBody = _rawBody;
            var originalContentType = ContentType ?? "text/plain";

            MimeVersion = "1.0";
            ContentType = $"multipart/{subtype}; boundary=\"{boundary}\"";
            _rawBody = null;

            if (!string.IsNullOrEmpty(originalBody))
            {
                AddPart(new MimePart
                {
                    ContentType = originalContentType,
                    Content = originalBody
                });
            }
        }

        private static string GenerateBoundary()
        {
            return $"----=_Part_{Guid.NewGuid():N}";
        }

        #endregion

        #region Decoding Helpers

        private static string? DecodeBody(string? body, string? encoding)
        {
            if (body == null) return null;
            if (string.IsNullOrEmpty(encoding)) return body;

            return encoding.ToLowerInvariant() switch
            {
                "base64" => DecodeBase64(body),
                "quoted-printable" => DecodeQuotedPrintable(body),
                _ => body
            };
        }

        private static string DecodeBase64(string encoded)
        {
            try
            {
                var bytes = Convert.FromBase64String(encoded.Replace("\r\n", "").Replace("\n", ""));
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return encoded;
            }
        }

        private static string DecodeQuotedPrintable(string encoded)
        {
            var result = new System.Text.StringBuilder();
            for (int i = 0; i < encoded.Length; i++)
            {
                if (encoded[i] == '=' && i + 2 < encoded.Length)
                {
                    if (encoded[i + 1] == '\r' && encoded[i + 2] == '\n')
                    {
                        // Soft line break
                        i += 2;
                        continue;
                    }
                    if (encoded[i + 1] == '\n')
                    {
                        // Soft line break (bare LF)
                        i += 1;
                        continue;
                    }

                    var hex = encoded.Substring(i + 1, 2);
                    if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int value))
                    {
                        result.Append((char)value);
                        i += 2;
                        continue;
                    }
                }
                result.Append(encoded[i]);
            }
            return result.ToString();
        }

        #endregion
    }

    /// <summary>
    /// Represents a single header field in a message.
    /// </summary>
    public class HeaderField
    {
        /// <summary>
        /// The header field name (e.g., "From", "Subject").
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The header field value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// The raw, unfolded line from the source message.
        /// </summary>
        public string? RawLine { get; set; }

        public HeaderField(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public override string ToString() => $"{Name}: {Value}";
    }

    /// <summary>
    /// Represents a parsed Content-Type header value.
    /// </summary>
    public class ContentTypeHeader
    {
        /// <summary>
        /// The media type (e.g., "text/plain", "multipart/mixed").
        /// </summary>
        public string MediaType { get; set; } = "text/plain";

        /// <summary>
        /// Parameters from the Content-Type header (e.g., charset, boundary).
        /// </summary>
        public Dictionary<string, string> Parameters { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets a parameter value by name.
        /// </summary>
        public string? GetParameter(string name)
        {
            return Parameters.TryGetValue(name, out var value) ? value : null;
        }

        /// <summary>
        /// The charset parameter, if present.
        /// </summary>
        public string? Charset => GetParameter("charset");

        /// <summary>
        /// The boundary parameter, if present (for multipart).
        /// </summary>
        public string? Boundary => GetParameter("boundary");

        /// <summary>
        /// Parses a Content-Type header value.
        /// </summary>
        public static ContentTypeHeader Parse(string value)
        {
            var result = new ContentTypeHeader();
            var parts = value.Split(';');

            if (parts.Length > 0)
            {
                result.MediaType = parts[0].Trim();
            }

            for (int i = 1; i < parts.Length; i++)
            {
                var param = parts[i].Trim();
                var eqIndex = param.IndexOf('=');
                if (eqIndex > 0)
                {
                    var paramName = param.Substring(0, eqIndex).Trim();
                    var paramValue = param.Substring(eqIndex + 1).Trim();

                    // Remove surrounding quotes
                    if (paramValue.StartsWith("\"") && paramValue.EndsWith("\"") && paramValue.Length >= 2)
                    {
                        paramValue = paramValue.Substring(1, paramValue.Length - 2);
                    }

                    result.Parameters[paramName] = paramValue;
                }
            }

            return result;
        }

        public override string ToString()
        {
            if (Parameters.Count == 0) return MediaType;

            var sb = new System.Text.StringBuilder(MediaType);
            foreach (var param in Parameters)
            {
                sb.Append($"; {param.Key}=\"{param.Value}\"");
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Represents a MIME part within a multipart message.
    /// </summary>
    public class MimePart
    {
        private readonly Dictionary<string, List<HeaderField>> _headers = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Headers for this MIME part.
        /// </summary>
        public IReadOnlyDictionary<string, List<HeaderField>> Headers => _headers;

        /// <summary>
        /// The raw content of this part (may be encoded).
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// Binary content (for attachments).
        /// </summary>
        public byte[]? BinaryContent { get; set; }

        /// <summary>
        /// Nested parts (for multipart within multipart).
        /// </summary>
        public List<MimePart> Parts { get; } = new();

        /// <summary>
        /// Whether this part is an attachment.
        /// </summary>
        public bool IsAttachment { get; set; }

        /// <summary>
        /// The filename for attachments.
        /// </summary>
        public string? Filename { get; set; }

        #region Header Properties

        public string? ContentType
        {
            get => GetHeaderValue("Content-Type");
            set => SetHeader("Content-Type", value);
        }

        public ContentTypeHeader? ContentTypeHeader
        {
            get
            {
                var value = ContentType;
                return value != null ? Message.ContentTypeHeader.Parse(value) : null;
            }
        }

        public string? ContentTransferEncoding
        {
            get => GetHeaderValue("Content-Transfer-Encoding");
            set => SetHeader("Content-Transfer-Encoding", value);
        }

        public string? ContentDisposition
        {
            get => GetHeaderValue("Content-Disposition");
            set => SetHeader("Content-Disposition", value);
        }

        public string? ContentId
        {
            get => GetHeaderValue("Content-ID");
            set => SetHeader("Content-ID", value);
        }

        public string? ContentDescription
        {
            get => GetHeaderValue("Content-Description");
            set => SetHeader("Content-Description", value);
        }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the decoded content as a string.
        /// </summary>
        public string? DecodedContent
        {
            get
            {
                if (Content == null) return null;
                var encoding = ContentTransferEncoding?.ToLowerInvariant();

                return encoding switch
                {
                    "base64" => DecodeBase64(Content),
                    "quoted-printable" => DecodeQuotedPrintable(Content),
                    _ => Content
                };
            }
        }

        /// <summary>
        /// Gets the decoded content as bytes.
        /// </summary>
        public byte[]? DecodedBinaryContent
        {
            get
            {
                if (BinaryContent != null) return BinaryContent;
                if (Content == null) return null;

                var encoding = ContentTransferEncoding?.ToLowerInvariant();
                if (encoding == "base64")
                {
                    try
                    {
                        return Convert.FromBase64String(Content.Replace("\r\n", "").Replace("\n", ""));
                    }
                    catch
                    {
                        return System.Text.Encoding.UTF8.GetBytes(Content);
                    }
                }

                return System.Text.Encoding.UTF8.GetBytes(DecodedContent ?? Content);
            }
        }

        /// <summary>
        /// Returns true if this part is itself multipart.
        /// </summary>
        public bool IsMultipart => ContentTypeHeader?.MediaType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase) == true;

        #endregion

        #region Header Management

        public void AddHeader(string name, string value)
        {
            var field = new HeaderField(name, value);
            if (!_headers.TryGetValue(name, out var list))
            {
                list = new List<HeaderField>();
                _headers[name] = list;
            }
            list.Add(field);
        }

        public void SetHeader(string name, string? value)
        {
            if (value == null)
            {
                _headers.Remove(name);
            }
            else
            {
                _headers[name] = new List<HeaderField> { new HeaderField(name, value) };
            }
        }

        public string? GetHeaderValue(string name)
        {
            return _headers.TryGetValue(name, out var list) ? list.FirstOrDefault()?.Value : null;
        }

        #endregion

        #region Decoding Helpers

        private static string DecodeBase64(string encoded)
        {
            try
            {
                var bytes = Convert.FromBase64String(encoded.Replace("\r\n", "").Replace("\n", ""));
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return encoded;
            }
        }

        private static string DecodeQuotedPrintable(string encoded)
        {
            var result = new System.Text.StringBuilder();
            for (int i = 0; i < encoded.Length; i++)
            {
                if (encoded[i] == '=' && i + 2 < encoded.Length)
                {
                    if (encoded[i + 1] == '\r' && i + 2 < encoded.Length && encoded[i + 2] == '\n')
                    {
                        i += 2;
                        continue;
                    }
                    if (encoded[i + 1] == '\n')
                    {
                        i += 1;
                        continue;
                    }

                    var hex = encoded.Substring(i + 1, 2);
                    if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int value))
                    {
                        result.Append((char)value);
                        i += 2;
                        continue;
                    }
                }
                result.Append(encoded[i]);
            }
            return result.ToString();
        }

        #endregion
    }

    /// <summary>
    /// Represents an email address with optional display name.
    /// </summary>
    public class MailboxAddress
    {
        /// <summary>
        /// The display name (e.g., "John Doe").
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// The email address (e.g., "john@example.com").
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// The local part of the address (before @).
        /// </summary>
        public string LocalPart => Address.Contains('@') ? Address.Split('@')[0] : Address;

        /// <summary>
        /// The domain part of the address (after @).
        /// </summary>
        public string Domain => Address.Contains('@') ? Address.Split('@')[1] : string.Empty;

        public MailboxAddress() { }

        public MailboxAddress(string address)
        {
            Address = address;
        }

        public MailboxAddress(string? displayName, string address)
        {
            DisplayName = displayName;
            Address = address;
        }

        /// <summary>
        /// Parses a mailbox address from a string like "John Doe &lt;john@example.com&gt;" or "john@example.com".
        /// </summary>
        public static MailboxAddress Parse(string value)
        {
            value = value.Trim();

            // Check for angle-bracket format: "Display Name <email@example.com>"
            var ltIndex = value.LastIndexOf('<');
            var gtIndex = value.LastIndexOf('>');

            if (ltIndex >= 0 && gtIndex > ltIndex)
            {
                var displayName = ltIndex > 0 ? value.Substring(0, ltIndex).Trim() : null;
                var address = value.Substring(ltIndex + 1, gtIndex - ltIndex - 1).Trim();

                // Remove surrounding quotes from display name
                if (displayName != null && displayName.StartsWith("\"") && displayName.EndsWith("\""))
                {
                    displayName = displayName.Substring(1, displayName.Length - 2);
                }

                return new MailboxAddress(displayName, address);
            }

            // Plain address
            return new MailboxAddress(value);
        }

        /// <summary>
        /// Parses a comma-separated list of mailbox addresses.
        /// </summary>
        public static List<MailboxAddress> ParseList(string value)
        {
            var result = new List<MailboxAddress>();
            var current = new System.Text.StringBuilder();
            int depth = 0;
            bool inQuotes = false;

            foreach (char c in value)
            {
                if (c == '"' && depth == 0)
                {
                    inQuotes = !inQuotes;
                    current.Append(c);
                }
                else if (c == '<' && !inQuotes)
                {
                    depth++;
                    current.Append(c);
                }
                else if (c == '>' && !inQuotes)
                {
                    depth--;
                    current.Append(c);
                }
                else if (c == ',' && depth == 0 && !inQuotes)
                {
                    var addr = current.ToString().Trim();
                    if (!string.IsNullOrEmpty(addr))
                    {
                        result.Add(Parse(addr));
                    }
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            var last = current.ToString().Trim();
            if (!string.IsNullOrEmpty(last))
            {
                result.Add(Parse(last));
            }

            return result;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(DisplayName))
            {
                return Address;
            }

            // Quote display name if it contains special characters
            if (DisplayName.Any(c => c == ',' || c == '<' || c == '>' || c == '"' || c == '(' || c == ')'))
            {
                return $"\"{DisplayName.Replace("\"", "\\\"")}\" <{Address}>";
            }

            return $"{DisplayName} <{Address}>";
        }
    }
}
