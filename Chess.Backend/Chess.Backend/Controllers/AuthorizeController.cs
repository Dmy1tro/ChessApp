using Microsoft.AspNetCore.Mvc;

namespace Chess.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthorizeController : ControllerBase
    {
        private readonly ILogger<AuthorizeController> _logger;

        public AuthorizeController(ILogger<AuthorizeController> logger)
        {
            _logger = logger;
        }

        [HttpPost("authorize")]
        public IActionResult Authorize()
        {
            return Ok(new
            {
                IdToken = Guid.NewGuid()
            });
        }
    }
}