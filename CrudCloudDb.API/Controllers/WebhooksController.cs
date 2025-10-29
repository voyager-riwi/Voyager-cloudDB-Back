using Microsoft.AspNetCore.Mvc;

namespace CrudCloudDb.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebhooksController : ControllerBase
    {
        private readonly ILogger<WebhooksController> _logger;

        public WebhooksController(ILogger<WebhooksController> logger)
        {
            _logger = logger;
        }
    }
}