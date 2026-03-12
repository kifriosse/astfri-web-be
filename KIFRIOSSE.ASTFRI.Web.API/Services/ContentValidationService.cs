using System.Text;

namespace KIFRIOSSE.ASTFRI.Web.API.Services
{
    public interface IContentValidationService
    {
        ContentValidationResult ValidateBase64Utf8Text(string inputText);
        ContentValidationResult ValidateText(string inputText);
    }

    public sealed class ContentValidationService : IContentValidationService
    {
        private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        private static readonly byte[][] KnownBinarySignatures =
        [
            [0x4D, 0x5A],
            [0x7F, 0x45, 0x4C, 0x46],
            [0x50, 0x4B, 0x03, 0x04],
            [0x89, 0x50, 0x4E, 0x47],
            [0x25, 0x50, 0x44, 0x46]
        ];

        public ContentValidationResult ValidateBase64Utf8Text(string inputText)
        {
            byte[] decodedBytes;

            try
            {
                decodedBytes = Convert.FromBase64String(inputText);
            }
            catch (FormatException)
            {
                return ContentValidationResult.Invalid("InputText must be valid base64-encoded UTF-8 text.");
            }

            if (decodedBytes.Length == 0)
            {
                return ContentValidationResult.Invalid("InputText cannot decode to an empty payload.");
            }

            if (HasKnownBinarySignature(decodedBytes))
            {
                return ContentValidationResult.Invalid("InputText contains binary content, which is not allowed.");
            }

            string decodedText;

            try
            {
                decodedText = StrictUtf8.GetString(decodedBytes);
            }
            catch (DecoderFallbackException)
            {
                return ContentValidationResult.Invalid("InputText must decode to valid UTF-8 text.");
            }

            if (ContainsDisallowedControlCharacters(decodedText))
            {
                return ContentValidationResult.Invalid("InputText contains non-text control characters and appears to be binary content.");
            }

            return ContentValidationResult.Valid();
        }

        public ContentValidationResult ValidateText(string inputText)
        {
            if (string.IsNullOrEmpty(inputText))
            {
                return ContentValidationResult.Invalid("Input cannot be null or empty.");
            }

            if (ContainsDisallowedControlCharacters(inputText))
            {
                return ContentValidationResult.Invalid("Input contains non-text control characters and appears to be binary content.");
            }

            return ContentValidationResult.Valid();
        }

        private static bool HasKnownBinarySignature(ReadOnlySpan<byte> decodedBytes)
        {
            foreach (var signature in KnownBinarySignatures)
            {
                if (decodedBytes.Length < signature.Length)
                {
                    continue;
                }

                if (decodedBytes[..signature.Length].SequenceEqual(signature))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsDisallowedControlCharacters(string decodedText)
        {
            foreach (var character in decodedText)
            {
                if (char.IsControl(character) && character is not '\r' and not '\n' and not '\t')
                {
                    return true;
                }
            }

            return false;
        }
    }

    public readonly record struct ContentValidationResult(bool IsValid, string? ErrorMessage)
    {
        public static ContentValidationResult Valid()
        {
            return new ContentValidationResult(true, null);
        }

        public static ContentValidationResult Invalid(string errorMessage)
        {
            return new ContentValidationResult(false, errorMessage);
        }
    }
}