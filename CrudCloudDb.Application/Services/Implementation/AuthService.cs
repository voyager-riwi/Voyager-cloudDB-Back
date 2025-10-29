using CrudCloudDb.Application.DTOs.Auth;
using CrudCloudDb.Application.DTOs.Common;
using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Application.Utilities;
using CrudCloudDb.Core.Entities;
using Microsoft.Extensions.Configuration;

namespace CrudCloudDb.Application.Services.Implementation
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPlanRepository _planRepository;
        private readonly IConfiguration _configuration;

        public AuthService(
            IUserRepository userRepository, 
            IPlanRepository planRepository, 
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _planRepository = planRepository;
            _configuration = configuration;
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return ApiResponse<AuthResponseDto>.Fail("Credenciales inválidas.");
            }

            if (!PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return ApiResponse<AuthResponseDto>.Fail("Credenciales inválidas.");
            }

            var token = JwtHelper.GenerateJwtToken(user, _configuration);
            
            var response = new AuthResponseDto
            {
                UserId = user.Id.ToString(),
                Email = user.Email,
                FullName = $"{user.FirstName} {user.LastName}",
                Token = token
            };

            return ApiResponse<AuthResponseDto>.Success(response, "Inicio de sesión exitoso.");
        }

        public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request)
        {
            if (await _userRepository.IsEmailTakenAsync(request.Email))
            {
                return ApiResponse<AuthResponseDto>.Fail("El correo electrónico ya está en uso.");
            }

            var defaultPlan = await _planRepository.GetDefaultPlanAsync();
            if (defaultPlan == null)
            {
                // Esto es un error crítico del sistema, no debería pasar
                return ApiResponse<AuthResponseDto>.Fail("Error del sistema: No se encontró el plan por defecto.");
            }
            
            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PasswordHash = PasswordHasher.HashPassword(request.Password),
                CurrentPlanId = defaultPlan.Id
            };

            var createdUser = await _userRepository.CreateAsync(user);

            // Después de registrar, hacemos login para generar el token
            var token = JwtHelper.GenerateJwtToken(createdUser, _configuration);
            
            var response = new AuthResponseDto
            {
                UserId = createdUser.Id.ToString(),
                Email = createdUser.Email,
                FullName = $"{createdUser.FirstName} {createdUser.LastName}",
                Token = token
            };

            return ApiResponse<AuthResponseDto>.Success(response, "Usuario registrado exitosamente.");
        }
    }
}