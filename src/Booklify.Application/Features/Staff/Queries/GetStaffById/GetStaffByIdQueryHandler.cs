using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Booklify.Application.Common.DTOs.Staff;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Entities;

namespace Booklify.Application.Features.Staff.Queries.GetStaffById;

public class GetStaffByIdQueryHandler : IRequestHandler<GetStaffByIdQuery, Result<StaffResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetStaffByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<StaffResponse>> Handle(GetStaffByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get staff profile with identity user included for IsActive status
            var staffProfile = await _unitOfWork.StaffProfileRepository.GetFirstOrDefaultAsync
            (
                x => x.Id == request.Id,
                x => x.IdentityUser
            );

            if (staffProfile == null)
            {
                return Result<StaffResponse>.Failure(
                        "Staff not found",
                        ErrorCode.NotFound);
            }

            var response = _mapper.Map<StaffResponse>(staffProfile);
            return Result<StaffResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<StaffResponse>.Failure(
                $"An error occurred while getting staff details: {ex.Message}",
                ErrorCode.InternalError);
        }
    }
} 