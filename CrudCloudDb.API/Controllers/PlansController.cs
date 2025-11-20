﻿using Microsoft.AspNetCore.Mvc;
using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Application.DTOs.Plan;
using Microsoft.AspNetCore.Authorization;

namespace CrudCloudDb.API.Controllers
{
    /// <summary>
    /// Controller para gestión de planes de suscripción
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PlansController : ControllerBase
    {
        private readonly ILogger<PlansController> _logger;
        private readonly IPlanRepository _planRepository;

        public PlansController(
            ILogger<PlansController> logger,
            IPlanRepository planRepository)
        {
            _logger = logger;
            _planRepository = planRepository;
        }

        /// <summary>
        /// Obtiene todos los planes disponibles
        /// </summary>
        /// <returns>Lista de planes con sus características y precios</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<PlanDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllPlans()
        {
            try
            {
                _logger.LogInformation("Obteniendo todos los planes disponibles");
                
                var plans = await _planRepository.GetAllAsync();
                
                var planDtos = plans.Select(p => new PlanDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    PlanType = p.PlanType,
                    Price = p.Price,
                    DatabaseLimitPerEngine = p.DatabaseLimitPerEngine
                }).OrderBy(p => p.Price);
                
                _logger.LogInformation("Se encontraron {Count} planes disponibles", planDtos.Count());
                
                return Ok(planDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los planes disponibles");
                return StatusCode(500, new { message = "Error al obtener los planes disponibles" });
            }
        }

        /// <summary>
        /// Obtiene un plan específico por su ID
        /// </summary>
        /// <param name="id">ID del plan</param>
        /// <returns>Detalles del plan</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PlanDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPlanById(Guid id)
        {
            try
            {
                _logger.LogInformation("Obteniendo plan con ID: {PlanId}", id);
                
                var plan = await _planRepository.GetByIdAsync(id);
                
                if (plan == null)
                {
                    _logger.LogWarning("Plan no encontrado: {PlanId}", id);
                    return NotFound(new { message = "Plan no encontrado" });
                }
                
                var planDto = new PlanDto
                {
                    Id = plan.Id,
                    Name = plan.Name,
                    PlanType = plan.PlanType,
                    Price = plan.Price,
                    DatabaseLimitPerEngine = plan.DatabaseLimitPerEngine
                };
                
                return Ok(planDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el plan con ID: {PlanId}", id);
                return StatusCode(500, new { message = "Error al obtener el plan" });
            }
        }
    }
}