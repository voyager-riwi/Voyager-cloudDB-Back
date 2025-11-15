using CrudCloudDb.Application.DTOs.Common;
using CrudCloudDb.Application.DTOs.User;
using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Application.Utilities;

namespace CrudCloudDb.Application.Services.Implementation;

public class UserService : IUserService
{

    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ApiResponse<UserProfileDto>> GetProfileAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdWithPlanAsync(userId);
        if (user == null)
        {
            return ApiResponse<UserProfileDto>.Fail("Usuario no encontrado");
        }
        
        var userProfile = new UserProfileDto
        {
            Id = user.Id.ToString(),
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            MemberSince = user.CreatedAt,
            CurrentPlanName = user.CurrentPlan.Name,
            DatabaseLimitPerEngine = user.CurrentPlan.DatabaseLimitPerEngine
        };
        
        return ApiResponse<UserProfileDto>.Success(userProfile, "Perfil creado exitosamente");
    }

    public async Task<ApiResponse<Object>> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request)
    {
        var user = await _userRepository.GetByIdWithPlanAsync(userId);
        if (user == null)
        {
            return ApiResponse<object>.Fail("Usuario no encontrado");
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;

        await _userRepository.UpdateAsync(user);
        return ApiResponse<object>.Success("Usuario editado exitosamente");
    }
    
    public async Task<ApiResponse<object>> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
            
        if (user == null)
        {
            return ApiResponse<object>.Fail("Usuario no encontrado.");
        }
        
        if (!PasswordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return ApiResponse<object>.Fail("La contraseña actual es incorrecta.");
        }
        
        user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
            
        await _userRepository.UpdateAsync(user);

        return ApiResponse<object>.Success(null, "Contraseña actualizada exitosamente.");
    }
}

    
