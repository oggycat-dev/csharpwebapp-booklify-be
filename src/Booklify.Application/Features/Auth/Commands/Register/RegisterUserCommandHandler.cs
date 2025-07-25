using MediatR;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Web;
using Booklify.Application.Common.DTOs.Auth;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Entities;
using Booklify.Domain.Entities.Identity;
using Booklify.Domain.Enums;
using Booklify.Domain.Commons;

namespace Booklify.Application.Features.Auth.Commands.Register;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<UserRegistrationResponse>>
{
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly UserManager<AppUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IIdentityService identityService, 
        IUnitOfWork unitOfWork,
        IMapper mapper,
        UserManager<AppUser> userManager,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userManager = userManager;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<UserRegistrationResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Create identity user using IdentityService
            var identityResult = await _identityService.RegisterUserAsync(request.Request);
            
            if (!identityResult.IsSuccess)
            {
                return identityResult;
            }
            
            // Find the created user to get ID for UserProfile
            var userResult = await _identityService.FindByUsernameAsync(request.Request.Username);
            if (!userResult.IsSuccess || userResult.Data == null)
            {
                return Result<UserRegistrationResponse>.Failure("User not found after creation", ErrorCode.UserNotFound);
            }
            
            var user = userResult.Data;
            
            // Generate email confirmation token and send email
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = HttpUtility.UrlEncode(token);
            
            // Get backend URL from environment configuration
            var backendUrl = _configuration["BackendUrl"];
            if (string.IsNullOrEmpty(backendUrl))
            {
                _logger.LogWarning("BackendUrl not configured, using default localhost URL");
                backendUrl = "http://localhost:5123"; // Match default from EnvironmentConfiguration
            }
            
            // Create verification link pointing directly to API
            var verificationLink = $"{backendUrl}/api/auth/confirm-email?email={HttpUtility.UrlEncode(user.Email!)}&token={encodedToken}";
            
            // Send verification email (don't fail registration if email fails)
            try
            {
                var emailResult = await _emailService.SendEmailVerificationAsync(user.Email!, verificationLink);
                if (!emailResult.IsSuccess)
                {
                    _logger.LogError("Failed to send verification email to {Email}: {Error}", user.Email, emailResult.Message);
                }
                else
                {
                    _logger.LogInformation("Verification email sent successfully to {Email}", user.Email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while sending verification email to {Email}", user.Email);
            }
            
            // Create UserProfile
            var userProfile = new UserProfile
            {
                IdentityUserId = user.Id,
                FirstName = request.Request.FirstName,
                LastName = request.Request.LastName,
                FullName = $"{request.Request.LastName} {request.Request.FirstName}",
                Phone = request.Request.PhoneNumber,
                Status = EntityStatus.Active
            };

            BaseEntityExtensions.InitializeBaseEntity(userProfile, user.Id);
            
            await _unitOfWork.UserProfileRepository.AddAsync(userProfile);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Update EntityId in Identity User
            await _identityService.UpdateEntityIdAsync(user.Id, userProfile.Id);
            
            return identityResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during user registration for {Username}", request.Request.Username);
            return Result<UserRegistrationResponse>.Failure("An error occurred during registration", ErrorCode.InternalError);
        }
    }
} 