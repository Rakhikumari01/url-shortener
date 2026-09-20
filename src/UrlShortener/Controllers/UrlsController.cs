using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UrlShortener.Dtos;
using UrlShortener.Services;

namespace UrlShortener.Controllers
{
    [ApiController]
    [Route("api/urls")]
    public class UrlsController : ControllerBase
    {
        private readonly IUrlService _service;
        private readonly ILogger<UrlsController> _logger;

        public UrlsController(IUrlService service, ILogger<UrlsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateShortUrlRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _service.CreateAsync(request, cancellationToken).ConfigureAwait(false);
                return CreatedAtAction(nameof(GetStats), new { code = result.Code }, result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogInformation("Invalid URL create request: {Message}", ex.Message);
                var pd = new ProblemDetails
                {
                    Title = "Invalid request",
                    Detail = ex.Message,
                    Status = 400
                };
                return BadRequest(pd);
            }
        }

        [HttpGet("{code:regex(^[0-9a-zA-Z]{1,11}$)}/stats", Name = nameof(GetStats))]
        public async Task<IActionResult> GetStats(string code, CancellationToken cancellationToken)
        {
            var entity = await _service.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
            if (entity == null) return NotFound();

            var response = new UrlStatsResponse
            {
                Code = entity.Code ?? string.Empty,
                OriginalUrl = entity.OriginalUrl,
                CreatedAt = entity.CreatedAt,
                ClickCount = entity.ClickCount
            };

            return Ok(response);
        }
    }
}