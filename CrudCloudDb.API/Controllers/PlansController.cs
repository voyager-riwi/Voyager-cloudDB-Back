using CrudCloudDb.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrudCloudDb.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlansController : ControllerBase
    {
        private readonly ILogger<PlansController> _logger;
        private readonly IPlanRepository _planRepository;

        public PlansController(ILogger<PlansController> logger, IPlanRepository planRepository)
        {
            _logger = logger;
            _planRepository = planRepository;
        }

        /// <summary>
        /// Obtiene todos los planes disponibles
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllPlans()
        {
            _logger.LogInformation("Obteniendo todos los planes disponibles");
            
            var plans = await _planRepository.GetAllAsync();
            
            var plansDto = plans.Select(p => new
            {
                id = p.Id,
                name = p.Name,
                price = p.Price,
                databaseLimitPerEngine = p.DatabaseLimitPerEngine,
                planType = p.PlanType.ToString(),
                features = new[]
                {
                    $"Hasta {p.DatabaseLimitPerEngine} bases de datos por motor",
                    p.PlanType == Core.Enums.PlanType.Free ? "Soporte básico" : "Soporte prioritario",
                    p.PlanType != Core.Enums.PlanType.Free ? "Sin anuncios" : "Con anuncios"
                }
            }).OrderBy(p => p.price);

            return Ok(plansDto);
        }

        /// <summary>
        /// Obtiene un plan específico por ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPlanById(Guid id)
        {
            _logger.LogInformation("Obteniendo plan con ID: {PlanId}", id);
            
            var plan = await _planRepository.GetByIdAsync(id);
            
            if (plan == null)
            {
                return NotFound(new { message = "Plan no encontrado" });
            }

            var planDto = new
            {
                id = plan.Id,
                name = plan.Name,
                price = plan.Price,
                databaseLimitPerEngine = plan.DatabaseLimitPerEngine,
                planType = plan.PlanType.ToString(),
                features = new[]
                {
                    $"Hasta {plan.DatabaseLimitPerEngine} bases de datos por motor",
                    plan.PlanType == Core.Enums.PlanType.Free ? "Soporte básico" : "Soporte prioritario",
                    plan.PlanType != Core.Enums.PlanType.Free ? "Sin anuncios" : "Con anuncios"
                }
            };

            return Ok(planDto);
        }
    }
}