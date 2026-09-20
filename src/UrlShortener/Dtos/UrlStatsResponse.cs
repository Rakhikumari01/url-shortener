namespace UrlShortener.Dtos
{
    public class UrlStatsResponse
    {
        public string Code { get; set; } = null!;
        public string OriginalUrl { get; set; } = null!;
        public DateTimeOffset CreatedAt { get; set; }
        public int ClickCount { get; set; }
    }
}
