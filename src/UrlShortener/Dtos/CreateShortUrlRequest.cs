using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Dtos
{
    public class CreateShortUrlRequest
    {
        [Required]
        [MaxLength(2048)]
        public string? Url { get; set; }
    }
}