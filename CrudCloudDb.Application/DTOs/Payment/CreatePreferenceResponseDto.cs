namespace CrudCloudDb.Application.DTOs.Payment;

public class CreatePreferenceResponseDto
{
    /// <summary>
    /// El ID de la preferencia generada por Mercado Pago.
    /// </summary>
    public string PreferenceId { get; set; } = string.Empty;

    /// <summary>
    /// La URL a la que debes redirigir al usuario para que complete el pago.
    /// </summary>
    public string InitPoint { get; set; } = string.Empty;
}