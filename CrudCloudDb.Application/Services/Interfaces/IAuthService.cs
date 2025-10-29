using CrudCloudDb.Application.DTOs.Auth;
using CrudCloudDb.Application.DTOs.Common;


namespace CrudCloudDb.Application.Services.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto dto);
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request);
}