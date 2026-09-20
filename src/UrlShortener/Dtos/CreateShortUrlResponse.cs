namespace UrlShortener.Dtos
{
    public class CreateShortUrlResponse
    {
        public string Code { get; set; } = null!;
        public string ShortUrl { get; set; } = null!;
        public string OriginalUrl { get; set; } = null!;
    }
}