using CrudCloudDb.Application.DTOs.Common;
using CrudCloudDb.Application.DTOs.User;
using System;
using System.Threading.Tasks;

namespace CrudCloudDb.Application.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApiResponse<UserProfileDto>> GetProfileAsync(Guid userId);
        Task<ApiResponse<object>> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request);
        Task<ApiResponse<object>> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request);
    }   
}