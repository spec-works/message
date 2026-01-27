using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Message
{
    /// <summary>
    /// Validator for message/rfc822 format according to RFC 5322 and MIME RFCs.
    /// </summary>
    public class MessageValidator
    {
        /// <summary>
        /// Validates a parsed message against RFC 5322 and MIME requirements.
        /// </summary>
        public ValidationResult Validate(MessageObject message)
        {
            var result = new ValidationResult();

            ValidateRequiredHeaders(message, result);
            ValidateOriginatorFields(message, result);
            ValidateDestinationFields(message, result);
            ValidateIdentificationFields(message, result);
            ValidateInformationalFields(message, result);
            ValidateDateField(message, result);
            ValidateMimeHeaders(message, result);

            if (message.IsMultipart)
            {
                ValidateMultipart(message, result);
            }

            return result;
        }

        /// <summary>
        /// Validates that required headers are present (RFC 5322 Section 3.6).
        /// </summary>
        private void ValidateRequiredHeaders(MessageObject message, ValidationResult result)
        {
            // From is required (RFC 5322 Section 3.6.2)
            if (string.IsNullOrEmpty(message.From))
            {
                result.AddError("Missing required 'From' header field (RFC 5322 Section 3.6.2)");
            }

            // Date is required (RFC 5322 Section 3.6.1)
            if (string.IsNullOrEmpty(message.DateRaw))
            {
                result.AddError("Missing required 'Date' header field (RFC 5322 Section 3.6.1)");
            }
        }

        /// <summary>
        /// Validates originator fields (RFC 5322 Section 3.6.2).
        /// </summary>
        private void ValidateOriginatorFields(MessageObject message, ValidationResult result)
        {
            // From must be present and valid
            var fromValue = message.From;
            if (!string.IsNullOrEmpty(fromValue))
            {
                var addresses = MailboxAddress.ParseList(fromValue);
                if (addresses.Count == 0)
                {
                    result.AddError("'From' field contains no valid addresses");
                }

                foreach (var addr in addresses)
                {
                    ValidateEmailAddress(addr.Address, "From", result);
                }

                // If From contains multiple addresses, Sender must be present
                if (addresses.Count > 1 && string.IsNullOrEmpty(message.Sender))
                {
                    result.AddError("'Sender' field is required when 'From' contains multiple addresses (RFC 5322 Section 3.6.2)");
                }
            }

            // Validate Sender if present
            var senderValue = message.Sender;
            if (!string.IsNullOrEmpty(senderValue))
            {
                var senderAddresses = MailboxAddress.ParseList(senderValue);
                if (senderAddresses.Count != 1)
                {
                    result.AddError("'Sender' field must contain exactly one address (RFC 5322 Section 3.6.2)");
                }
                else
                {
                    ValidateEmailAddress(senderAddresses[0].Address, "Sender", result);
                }
            }

            // Validate Reply-To if present
            var replyToValue = message.ReplyTo;
            if (!string.IsNullOrEmpty(replyToValue))
            {
                var addresses = MailboxAddress.ParseList(replyToValue);
                foreach (var addr in addresses)
                {
                    ValidateEmailAddress(addr.Address, "Reply-To", result);
                }
            }
        }

        /// <summary>
        /// Validates destination fields (RFC 5322 Section 3.6.3).
        /// </summary>
        private void ValidateDestinationFields(MessageObject message, ValidationResult result)
        {
            bool hasRecipient = false;

            // Validate To
            var toValue = message.To;
            if (!string.IsNullOrEmpty(toValue))
            {
                hasRecipient = true;
                var addresses = MailboxAddress.ParseList(toValue);
                foreach (var addr in addresses)
                {
                    ValidateEmailAddress(addr.Address, "To", result);
                }
            }

            // Validate Cc
            var ccValue = message.Cc;
            if (!string.IsNullOrEmpty(ccValue))
            {
                hasRecipient = true;
                var addresses = MailboxAddress.ParseList(ccValue);
                foreach (var addr in addresses)
                {
                    ValidateEmailAddress(addr.Address, "Cc", result);
                }
            }

            // Validate Bcc
            var bccValue = message.Bcc;
            if (!string.IsNullOrEmpty(bccValue))
            {
                hasRecipient = true;
                var addresses = MailboxAddress.ParseList(bccValue);
                foreach (var addr in addresses)
                {
                    ValidateEmailAddress(addr.Address, "Bcc", result);
                }
            }

            // At least one recipient is recommended (but not strictly required)
            if (!hasRecipient)
            {
                result.AddWarning("Message has no recipient fields (To, Cc, or Bcc)");
            }
        }

        /// <summary>
        /// Validates identification fields (RFC 5322 Section 3.6.4).
        /// </summary>
        private void ValidateIdentificationFields(MessageObject message, ValidationResult result)
        {
            // Message-ID is recommended
            var messageId = message.MessageId;
            if (string.IsNullOrEmpty(messageId))
            {
                result.AddWarning("Message-ID header is recommended (RFC 5322 Section 3.6.4)");
            }
            else
            {
                ValidateMessageId(messageId, "Message-ID", result);
            }

            // Validate In-Reply-To if present
            var inReplyTo = message.InReplyTo;
            if (!string.IsNullOrEmpty(inReplyTo))
            {
                // Can contain multiple message IDs
                var ids = ExtractMessageIds(inReplyTo);
                foreach (var id in ids)
                {
                    ValidateMessageId(id, "In-Reply-To", result);
                }
            }

            // Validate References if present
            var references = message.References;
            if (!string.IsNullOrEmpty(references))
            {
                var ids = ExtractMessageIds(references);
                foreach (var id in ids)
                {
                    ValidateMessageId(id, "References", result);
                }
            }
        }

        /// <summary>
        /// Validates informational fields (RFC 5322 Section 3.6.5).
        /// </summary>
        private void ValidateInformationalFields(MessageObject message, ValidationResult result)
        {
            // Subject can contain any text - just check for encoding issues
            var subject = message.Subject;
            if (subject != null && subject.Contains("=?") && subject.Contains("?="))
            {
                // Check for improperly decoded encoded-words
                if (Regex.IsMatch(subject, @"=\?[^?]+\?[BQ]\?[^?]+\?=", RegexOptions.IgnoreCase))
                {
                    result.AddWarning("Subject may contain undecoded RFC 2047 encoded-words");
                }
            }
        }

        /// <summary>
        /// Validates the Date field format (RFC 5322 Section 3.3).
        /// </summary>
        private void ValidateDateField(MessageObject message, ValidationResult result)
        {
            var dateRaw = message.DateRaw;
            if (string.IsNullOrEmpty(dateRaw)) return;

            // Try to parse the date
            if (message.Date == null)
            {
                // Try more flexible parsing
                if (!TryParseRfc5322Date(dateRaw, out _))
                {
                    result.AddWarning($"Date field has non-standard format: {dateRaw}");
                }
            }
            else
            {
                // Check for future dates (warning)
                var date = message.Date.Value;
                if (date > DateTimeOffset.UtcNow.AddHours(1))
                {
                    result.AddWarning($"Date field is in the future: {date}");
                }

                // Check for very old dates (warning)
                if (date < new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero))
                {
                    result.AddWarning($"Date field is before 1970: {date}");
                }
            }
        }

        /// <summary>
        /// Validates MIME headers (RFC 2045).
        /// </summary>
        private void ValidateMimeHeaders(MessageObject message, ValidationResult result)
        {
            // If MIME-Version is present, validate it
            var mimeVersion = message.MimeVersion;
            if (!string.IsNullOrEmpty(mimeVersion))
            {
                if (mimeVersion.Trim() != "1.0")
                {
                    result.AddWarning($"Non-standard MIME-Version: {mimeVersion} (expected 1.0)");
                }
            }

            // Validate Content-Type if present
            var contentType = message.ContentType;
            if (!string.IsNullOrEmpty(contentType))
            {
                var ct = message.ContentTypeHeader;
                if (ct != null)
                {
                    // Validate media type format
                    if (!Regex.IsMatch(ct.MediaType, @"^[a-zA-Z0-9!#$&\-^_\.+]+/[a-zA-Z0-9!#$&\-^_\.+]+$"))
                    {
                        result.AddWarning($"Content-Type has invalid media type format: {ct.MediaType}");
                    }

                    // For multipart, boundary is required
                    if (ct.MediaType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrEmpty(ct.Boundary))
                        {
                            result.AddError("Multipart Content-Type must have a boundary parameter (RFC 2046 Section 5.1)");
                        }
                    }
                }
            }

            // Validate Content-Transfer-Encoding if present
            var cte = message.ContentTransferEncoding;
            if (!string.IsNullOrEmpty(cte))
            {
                var validEncodings = new[] { "7bit", "8bit", "binary", "quoted-printable", "base64" };
                if (!validEncodings.Contains(cte.ToLowerInvariant()))
                {
                    result.AddWarning($"Non-standard Content-Transfer-Encoding: {cte}");
                }
            }
        }

        /// <summary>
        /// Validates multipart message structure.
        /// </summary>
        private void ValidateMultipart(MessageObject message, ValidationResult result)
        {
            if (message.Parts.Count == 0)
            {
                result.AddError("Multipart message contains no parts");
                return;
            }

            // Validate each part
            foreach (var part in message.Parts)
            {
                ValidateMimePart(part, result);
            }

            // Validate multipart/alternative structure
            var ct = message.ContentTypeHeader;
            if (ct?.MediaType.Equals("multipart/alternative", StringComparison.OrdinalIgnoreCase) == true)
            {
                // Parts should be alternative representations of the same content
                // Typically text/plain followed by text/html
                var textCount = message.Parts.Count(p =>
                    p.ContentTypeHeader?.MediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) == true);

                if (textCount < 2)
                {
                    result.AddWarning("multipart/alternative typically contains multiple text format alternatives");
                }
            }

            // Validate multipart/mixed structure
            if (ct?.MediaType.Equals("multipart/mixed", StringComparison.OrdinalIgnoreCase) == true)
            {
                // First part is typically the main message
                var firstPart = message.Parts.FirstOrDefault();
                if (firstPart?.IsAttachment == true)
                {
                    result.AddWarning("First part of multipart/mixed is typically the main message, not an attachment");
                }
            }
        }

        /// <summary>
        /// Validates a single MIME part.
        /// </summary>
        private void ValidateMimePart(MimePart part, ValidationResult result)
        {
            // Validate Content-Type if present
            var ct = part.ContentTypeHeader;
            if (ct != null)
            {
                // Validate charset for text types
                if (ct.MediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
                {
                    var charset = ct.Charset;
                    if (!string.IsNullOrEmpty(charset))
                    {
                        try
                        {
                            System.Text.Encoding.GetEncoding(charset);
                        }
                        catch
                        {
                            result.AddWarning($"Unknown charset in Content-Type: {charset}");
                        }
                    }
                }

                // Recursive validation for nested multipart
                if (part.IsMultipart)
                {
                    if (string.IsNullOrEmpty(ct.Boundary))
                    {
                        result.AddError("Nested multipart must have boundary parameter");
                    }

                    foreach (var nestedPart in part.Parts)
                    {
                        ValidateMimePart(nestedPart, result);
                    }
                }
            }

            // Validate Content-Transfer-Encoding
            var cte = part.ContentTransferEncoding;
            if (!string.IsNullOrEmpty(cte))
            {
                var validEncodings = new[] { "7bit", "8bit", "binary", "quoted-printable", "base64" };
                if (!validEncodings.Contains(cte.ToLowerInvariant()))
                {
                    result.AddWarning($"Non-standard Content-Transfer-Encoding in part: {cte}");
                }

                // base64 content should be valid
                if (cte.Equals("base64", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(part.Content))
                {
                    try
                    {
                        Convert.FromBase64String(part.Content.Replace("\r\n", "").Replace("\n", ""));
                    }
                    catch
                    {
                        result.AddWarning("Part has invalid base64 content");
                    }
                }
            }
        }

        /// <summary>
        /// Validates an email address format.
        /// </summary>
        private void ValidateEmailAddress(string address, string fieldName, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                result.AddWarning($"Empty email address in {fieldName}");
                return;
            }

            // Basic validation: must contain @ with local and domain parts
            var atIndex = address.LastIndexOf('@');
            if (atIndex <= 0 || atIndex >= address.Length - 1)
            {
                result.AddWarning($"Invalid email address format in {fieldName}: {address}");
                return;
            }

            var localPart = address.Substring(0, atIndex);
            var domain = address.Substring(atIndex + 1);

            // Local part validation (simplified)
            if (localPart.Length > 64)
            {
                result.AddWarning($"Local part exceeds 64 characters in {fieldName}: {address}");
            }

            // Domain validation (simplified)
            if (domain.Length > 255)
            {
                result.AddWarning($"Domain exceeds 255 characters in {fieldName}: {address}");
            }

            if (!domain.Contains('.') && !domain.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                result.AddWarning($"Domain has no dots in {fieldName}: {address}");
            }
        }

        /// <summary>
        /// Validates a Message-ID format (RFC 5322 Section 3.6.4).
        /// </summary>
        private void ValidateMessageId(string messageId, string fieldName, ValidationResult result)
        {
            // Message-ID should be in angle brackets: <unique@domain>
            messageId = messageId.Trim();

            if (!messageId.StartsWith("<") || !messageId.EndsWith(">"))
            {
                result.AddWarning($"{fieldName} should be in angle brackets: {messageId}");
                return;
            }

            var inner = messageId.Substring(1, messageId.Length - 2);
            if (!inner.Contains("@"))
            {
                result.AddWarning($"{fieldName} should contain @ symbol: {messageId}");
            }
        }

        /// <summary>
        /// Extracts message IDs from a space-separated list.
        /// </summary>
        private List<string> ExtractMessageIds(string value)
        {
            var result = new List<string>();
            var matches = Regex.Matches(value, @"<[^>]+>");
            foreach (Match match in matches)
            {
                result.Add(match.Value);
            }
            return result;
        }

        /// <summary>
        /// Attempts to parse an RFC 5322 date.
        /// </summary>
        private bool TryParseRfc5322Date(string value, out DateTimeOffset date)
        {
            date = default;

            // Common RFC 5322 date formats
            var formats = new[]
            {
                "ddd, dd MMM yyyy HH:mm:ss zzz",
                "ddd, d MMM yyyy HH:mm:ss zzz",
                "dd MMM yyyy HH:mm:ss zzz",
                "d MMM yyyy HH:mm:ss zzz",
                "ddd, dd MMM yyyy HH:mm:ss",
                "ddd, d MMM yyyy HH:mm:ss",
                "dd MMM yyyy HH:mm:ss",
                "d MMM yyyy HH:mm:ss"
            };

            foreach (var format in formats)
            {
                if (DateTimeOffset.TryParseExact(value.Trim(), format,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AllowWhiteSpaces, out date))
                {
                    return true;
                }
            }

            // Fall back to general parsing
            return DateTimeOffset.TryParse(value, out date);
        }
    }

    /// <summary>
    /// Result of validation operation.
    /// </summary>
    public class ValidationResult
    {
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();

        public bool IsValid => Errors.Count == 0;

        public void AddError(string error)
        {
            Errors.Add(error);
        }

        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }

        public string GetSummary()
        {
            var summary = $"Validation Result: {(IsValid ? "VALID" : "INVALID")}\n";
            summary += $"Errors: {Errors.Count}\n";
            summary += $"Warnings: {Warnings.Count}\n";

            if (Errors.Count > 0)
            {
                summary += "\nErrors:\n";
                foreach (var error in Errors)
                {
                    summary += $"  - {error}\n";
                }
            }

            if (Warnings.Count > 0)
            {
                summary += "\nWarnings:\n";
                foreach (var warning in Warnings)
                {
                    summary += $"  - {warning}\n";
                }
            }

            return summary;
        }
    }
}
