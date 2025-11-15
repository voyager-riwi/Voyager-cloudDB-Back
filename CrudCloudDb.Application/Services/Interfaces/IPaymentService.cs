using CrudCloudDb.Application.DTOs.Common;
using CrudCloudDb.Application.DTOs.Payment;

namespace CrudCloudDb.Application.Services.Interfaces;

public interface IPaymentService
{
    /// <summary>
    /// Crea una preferencia de pago en Mercado Pago para una suscripción a un plan.
    /// </summary>
    /// <param name="userId">El ID del usuario que realiza la compra.</param>
    /// <param name="request">El DTO con el ID del plan a suscribir.</param>
    /// <returns>La respuesta con el ID y la URL de pago (Init Point).</returns>
    Task<ApiResponse<CreatePreferenceResponseDto>> CreateSubscriptionPreferenceAsync(Guid userId, CreatePreferenceRequestDto request);
}