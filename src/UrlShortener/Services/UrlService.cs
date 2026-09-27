using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UrlShortener.Data;
using UrlShortener.Dtos;
using UrlShortener.Models;

namespace UrlShortener.Services
{
    public class UrlService : IUrlService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<UrlService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string? _configuredBaseUrl;

        public UrlService(AppDbContext db,IConfiguration configuration,IHttpContextAccessor httpContextAccessor,ILogger<UrlService> logger)
        {
            _db = db;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;

            var configured = configuration.GetValue<string>("BaseUrl");
            _configuredBaseUrl = string.IsNullOrWhiteSpace(configured)
                ? null
                : configured.TrimEnd('/');
        }

        private string ResolveBaseUrl()
        {
            if (_configuredBaseUrl != null) return _configuredBaseUrl;

            var request = _httpContextAccessor.HttpContext?.Request ?? throw new InvalidOperationException("'BaseUrl' is not configured and there is no active HTTP request to derive it from.");

            return $"{request.Scheme}://{request.Host}";
        }

        public async Task<CreateShortUrlResponse> CreateAsync(CreateShortUrlRequest request, CancellationToken cancellationToken = default)
        {
            if (request?.Url == null) throw new ArgumentException("Url is required.", nameof(request));
            if (request.Url.Length > 2048) throw new ArgumentException("Url exceeds maximum length of 2048 characters.", nameof(request));

            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) ||!(string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("The URL must be an absolute HTTP or HTTPS URL.", nameof(request));
            }

            var entity = new ShortUrl
            {
                OriginalUrl = request.Url,
                CreatedAt = DateTimeOffset.UtcNow,
                ClickCount = 0,
                Code = null
            };

            _db.ShortUrls.Add(entity);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            entity.Code = Base62Encoder.Encode(entity.Id);
            _db.ShortUrls.Update(entity);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var shortUrl = $"{ResolveBaseUrl()}/{entity.Code}";
            return new CreateShortUrlResponse
            {
                Code = entity.Code!,
                ShortUrl = shortUrl,
                OriginalUrl = entity.OriginalUrl
            };
        }

        public async Task<ShortUrl?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(code)) return null;
            return await _db.ShortUrls.FirstOrDefaultAsync(x => x.Code == code, cancellationToken).ConfigureAwait(false);
        }

        public async Task IncrementClickCountAsync(ShortUrl shortUrl, CancellationToken cancellationToken = default)
        {
            if (shortUrl == null) return;
            shortUrl.ClickCount++;
            _db.ShortUrls.Update(shortUrl);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}