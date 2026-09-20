using UrlShortener.Dtos;
using UrlShortener.Models;

namespace UrlShortener.Services
{
    public interface IUrlService
    {
        Task<CreateShortUrlResponse> CreateAsync(CreateShortUrlRequest request, CancellationToken cancellationToken = default);
        Task<ShortUrl?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task IncrementClickCountAsync(ShortUrl shortUrl, CancellationToken cancellationToken = default);
    }
}