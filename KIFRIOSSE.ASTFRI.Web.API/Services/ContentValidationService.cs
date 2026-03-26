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