﻿using CrudCloudDb.Application.DTOs.Auth;
using CrudCloudDb.Application.DTOs.Common;
using CrudCloudDb.Application.DTOs.Email;
using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Application.Utilities;
using CrudCloudDb.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CrudCloudDb.Application.Services.Implementation
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPlanRepository _planRepository;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
   

        public AuthService(
            IUserRepository userRepository, 
            IPlanRepository planRepository, 
            IConfiguration configuration,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _planRepository = planRepository;
            _configuration = configuration;
            _emailService = emailService;
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

        public async Task<ApiResponse<object>> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                // Por seguridad, siempre devolvemos un mensaje genérico.
                return ApiResponse<object>.Success(null, "Si existe una cuenta con este correo, se ha enviado un código de recuperación.");
            }

            // 2. Generamos el token NUMÉRICO de 6 dígitos
            var resetToken = CredentialGenerator.GenerateNumericToken(); 
            user.PasswordResetToken = resetToken;
            user.PasswordResetExpires = DateTime.UtcNow.AddMinutes(15); // Válido por 15 mins

            await _userRepository.UpdateAsync(user);

            // 3. Creamos el DTO para el servicio de correo
            var emailDto = new AccountPasswordResetEmailDto
            {
                ToEmail = user.Email,
                Username = user.FirstName,
                ResetToken = resetToken
            };
            
            // 4. Llamamos al servicio de correo para enviar el email
            await _emailService.SendAccountPasswordResetEmailAsync(emailDto);
            
            return ApiResponse<object>.Success(null, "Si existe una cuenta con este correo, se ha enviado un código de recuperación.");
        }

        public async Task<ApiResponse<object>> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            var user = await _userRepository.GetByPasswordResetTokenAsync(request.Token);

            if (user == null || user.PasswordResetExpires < DateTime.UtcNow)
            {
                return ApiResponse<object>.Fail("El código es inválido o ha expirado.");
            }

            user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetExpires = null;

            await _userRepository.UpdateAsync(user);

            return ApiResponse<object>.Success(null, "Tu contraseña ha sido restablecida exitosamente.");
        }
    }
}