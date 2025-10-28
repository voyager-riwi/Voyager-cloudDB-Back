using Microsoft.AspNetCore.Mvc;

namespace CrudCloudDb.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabasesController : ControllerBase
    {
        private readonly ILogger<DatabasesController> _logger;

        public DatabasesController(ILogger<DatabasesController> logger)
        {
            _logger = logger;
        }
    }
}