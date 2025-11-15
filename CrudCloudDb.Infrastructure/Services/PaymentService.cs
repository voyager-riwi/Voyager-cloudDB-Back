using CrudCloudDb.Application.DTOs.Common;
using CrudCloudDb.Application.DTOs.Payment;
using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Core.Entities;
using MercadoPago.Client.Preference;
using MercadoPago.Resource.Preference;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CrudCloudDb.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly IUserRepository _userRepository;
    private readonly IPlanRepository _planRepository;
    private readonly ILogger<PaymentService> _logger;
    private readonly string _notificationUrl;

    public PaymentService(
        IUserRepository userRepository,
        IPlanRepository planRepository,
        ILogger<PaymentService> logger,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _planRepository = planRepository;
        _logger = logger;
        _notificationUrl = configuration["MercadoPagoSettings:NotificationUrl"]
                           ?? Environment.GetEnvironmentVariable("MERCADOPAGO_NOTIFICATION_URL")
                           ?? "https://service.voyager.andrescortes.dev/api/Webhooks/mercadopago";
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

        try
        {
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
                NotificationUrl = _notificationUrl,
                ExternalReference = $"user:{userId};plan:{plan.Id}",
            };

            var client = new PreferenceClient();
            Preference preference = await client.CreateAsync(preferenceRequest);

            var responseDto = new CreatePreferenceResponseDto
            {
                PreferenceId = preference.Id,
                InitPoint = preference.InitPoint,
            };

            _logger.LogInformation("Preferencia de pago creada exitosamente con ID: {PreferenceId}", preference.Id);
            return ApiResponse<CreatePreferenceResponseDto>.Success(responseDto, "Preferencia creada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear la preferencia de pago en Mercado Pago.");
            return ApiResponse<CreatePreferenceResponseDto>.Fail("Error al comunicarse con el servicio de pagos.");
        }
    }
}