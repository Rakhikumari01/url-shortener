using System.Text;

namespace UrlShortener.Services
{
    public static class Base62Encoder
    {
        private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        private static readonly int Radix = Alphabet.Length;

        public static string Encode(long value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Value must be non-negative.");
            if (value == 0) return "0";

            var sb = new StringBuilder();
            while (value > 0)
            {
                var rem = (int)(value % Radix);
                sb.Insert(0, Alphabet[rem]);
                value /= Radix;
            }

            return sb.ToString();
        }

        public static long Decode(string code)
        {
            if (string.IsNullOrEmpty(code)) throw new ArgumentException("Code must not be null or empty.", nameof(code));
            if (code.Length > 11) throw new ArgumentOutOfRangeException(nameof(code), "Code length exceeds maximum of 11.");

            long result = 0;
            foreach (var ch in code)
            {
                var idx = Alphabet.IndexOf(ch);
                if (idx < 0) throw new ArgumentException("Code contains invalid characters.", nameof(code));
                result = result * Radix + idx;
            }

            return result;
        }
    }
}