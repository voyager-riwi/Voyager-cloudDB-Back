using Microsoft.AspNetCore.Mvc;

namespace CrudCloudDb.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "CrudCloudDb API"
            });
        }
        
        [HttpGet("error")]
        public IActionResult ThrowError()
        {
            throw new InvalidOperationException("Este es un error de prueba para verificar el middleware.");
        }
    }
}