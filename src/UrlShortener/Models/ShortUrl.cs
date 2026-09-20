using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace UrlShortener.Models
{
    [Index(nameof(Code), IsUnique = true)]
    public class ShortUrl
    {
        public long Id { get; set; }

        [MaxLength(11)]
        public string? Code { get; set; }

        [Required]
        [MaxLength(2048)]
        public string OriginalUrl { get; set; } = null!;

        public DateTimeOffset CreatedAt { get; set; }

        public int ClickCount { get; set; }
    }
}