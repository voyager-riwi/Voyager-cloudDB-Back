using CrudCloudDb.Application.DTOs.Common;
using CrudCloudDb.Application.DTOs.Payment;
using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Core.Entities;
using MercadoPago.Client.Preference;
using MercadoPago.Resource.Preference;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CrudCloudDb.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly IUserRepository _userRepository;
    private readonly IPlanRepository _planRepository;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IUserRepository userRepository,
        IPlanRepository planRepository,
        ILogger<PaymentService> logger)
    {
        _userRepository = userRepository;
        _planRepository = planRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<CreatePreferenceResponseDto>> CreateSubscriptionPreferenceAsync(Guid userId, CreatePreferenceRequestDto request)
    {
        _logger.LogInformation("Iniciando creación de preferencia de pago para el usuario {UserId} y plan {PlanId}", userId, request.PlanId);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Usuario no encontrado: {UserId}", userId);
            return ApiResponse<CreatePreferenceResponseDto>.Fail("Usuario no encontrado.");
        }

        var plan = await _planRepository.GetByIdAsync(request.PlanId);
        if (plan == null)
        {
            _logger.LogWarning("Plan no encontrado: {PlanId}", request.PlanId);
            return ApiResponse<CreatePreferenceResponseDto>.Fail("El plan seleccionado no existe.");
        }

        // LOGS DETALLADOS PARA DIAGNÓSTICO
        _logger.LogInformation("🎯 DATOS DE LA PREFERENCIA:");
        _logger.LogInformation("  👤 Usuario: {UserName} ({UserEmail})", $"{user.FirstName} {user.LastName}", user.Email);
        _logger.LogInformation("  📦 Plan: {PlanName} (ID: {PlanId})", plan.Name, plan.Id);
        _logger.LogInformation("  💰 Precio: ${Price} COP", plan.Price);
        _logger.LogInformation("  🌍 País: Colombia (COP currency)");

        try
        {
            var notificationUrl = "https://service.voyager.andrescortes.dev/api/Webhooks/mercadopago";
            _logger.LogInformation("🔔 Configurando NotificationUrl para webhooks: {NotificationUrl}", notificationUrl);
            
            var preferenceRequest = new PreferenceRequest
            {
                Items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Title = $"Suscripción Plan {plan.Name}",
                        Description = $"Acceso al plan {plan.Name} en PotterCloud.",
                        Quantity = 1,
                        CurrencyId = "COP", 
                        UnitPrice = plan.Price,
                    },
                },
                Payer = new PreferencePayerRequest
                {
                    Name = user.FirstName,
                    Surname = user.LastName,
                    Email = user.Email,
                },
                BackUrls = new PreferenceBackUrlsRequest
                {
                    Success = "https://voyager.andrescortes.dev/payment-success",
                    Failure = "https://voyager.andrescortes.dev/payment-failure",
                    Pending = "https://voyager.andrescortes.dev/payment-pending",
                },
                AutoReturn = "approved",
                NotificationUrl = notificationUrl,
                ExternalReference = $"user:{userId};plan:{plan.Id}",
                StatementDescriptor = "PotterCloud",
                BinaryMode = false, // Importante: debe ser false para recibir webhooks
            };

            _logger.LogInformation("📤 Enviando preferencia a MercadoPago...");
            var client = new PreferenceClient();
            Preference preference = await client.CreateAsync(preferenceRequest);

            _logger.LogInformation("📥 Respuesta de MercadoPago recibida:");
            _logger.LogInformation("  - Preference ID: {PreferenceId}", preference.Id);
            _logger.LogInformation("  - Init Point: {InitPoint}", preference.InitPoint);
            _logger.LogInformation("  - Notification URL configurada: {NotificationUrl}", preference.NotificationUrl);
            _logger.LogInformation("  - External Reference: {ExternalReference}", preference.ExternalReference);

            var responseDto = new CreatePreferenceResponseDto
            {
                PreferenceId = preference.Id,
                InitPoint = preference.InitPoint,
            };

            _logger.LogInformation("✅ Preferencia de pago creada exitosamente con ID: {PreferenceId}", preference.Id);
            return ApiResponse<CreatePreferenceResponseDto>.Success(responseDto, "Preferencia creada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear la preferencia de pago en Mercado Pago.");
            return ApiResponse<CreatePreferenceResponseDto>.Fail("Error al comunicarse con el servicio de pagos.");
        }
    }
}