using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UrlShortener.Services;

namespace UrlShortener.Controllers
{
    [ApiController]
    public class RedirectController : ControllerBase
    {
        private readonly IUrlService _service;
        private readonly ILogger<RedirectController> _logger;

        public RedirectController(IUrlService service, ILogger<RedirectController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("{code:regex(^[0-9a-zA-Z]{1,11}$)}")]
        public async Task<IActionResult> Get(string code, CancellationToken cancellationToken)
        {
            var entity = await _service.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
            if (entity == null) return NotFound();

            try
            {
                await _service.IncrementClickCountAsync(entity, cancellationToken).ConfigureAwait(false);
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "Failed to increment click count for code {Code}", code);
            }

            return Redirect(entity.OriginalUrl);
        }
    }
}