using Booklify.Application.Common.Interfaces.Repositories;
using Booklify.Domain.Entities;
using Booklify.Infrastructure.Persistence;
using Booklify.Application.Common.DTOs.Staff;
using Booklify.Infrastructure.Repositories.Extensions;
using System.Linq.Expressions;

namespace Booklify.Infrastructure.Repositories;

public class StaffProfileRepository : GenericRepository<StaffProfile>, IStaffProfileRepository
{
    public StaffProfileRepository(BooklifyDbContext context) : base(context)
    {
    }

    public async Task<(List<StaffProfile> Staffs, int TotalCount)> GetPagedStaffsAsync(StaffFilterModel filter)
    {
        // Build filter predicate
        Expression<Func<StaffProfile, bool>> predicate = BuildFilterPredicate(filter);
        
        // Determine sorting property
        var orderByExpression = GetOrderByExpression(filter.SortBy);

        // Get paged data from base repository with includes for IdentityUser
        return await GetPagedAsync(
            predicate,
            orderByExpression,
            filter.IsAscending,
            filter.PageNumber,
            filter.PageSize,
            s => s.IdentityUser  // Include IdentityUser for Email and IsActive filtering
        );
    }
    
    private Expression<Func<StaffProfile, bool>> BuildFilterPredicate(StaffFilterModel filter)
    {
        // Start with filtering out Administrator position (position = 1)
        Expression<Func<StaffProfile, bool>> predicate = s => s.Position != Domain.Enums.StaffPosition.Administrator;
        
        // Apply conditional filters
        if (!string.IsNullOrEmpty(filter.StaffCode))
        {
            predicate = predicate.CombineAnd(s => s.StaffCode.Contains(filter.StaffCode));
        }
        
        if (!string.IsNullOrEmpty(filter.FullName))
        {
            predicate = predicate.CombineAnd(s => s.FullName.Contains(filter.FullName));
        }
        
        if (!string.IsNullOrEmpty(filter.Email))
        {
            predicate = predicate.CombineAnd(s => s.Email.Contains(filter.Email));
        }
        
        if (!string.IsNullOrEmpty(filter.Phone))
        {
            predicate = predicate.CombineAnd(s => s.Phone.Contains(filter.Phone));
        }
        
        if (filter.Position.HasValue)
        {
            predicate = predicate.CombineAnd(s => s.Position == filter.Position);
        }
        
        // Filter by account status instead of profile status
        if (filter.IsActive.HasValue)
        {
            predicate = predicate.CombineAnd(s => s.IdentityUser != null && s.IdentityUser.IsActive == filter.IsActive.Value);
        }
        
        return predicate;
    }

    private Expression<Func<StaffProfile, object>> GetOrderByExpression(string? sortBy)
    {
        // Default sort by staff code if not specified
        if (string.IsNullOrEmpty(sortBy))
        {
            return s => s.StaffCode;
        }
        
        // Return appropriate sorting expression based on property name
        return sortBy.ToLower() switch
        {
            "staffcode" => s => s.StaffCode,
            "fullname" => s => s.FullName,
            "email" => s => s.Email,
            "phone" => s => s.Phone,
            "position" => s => s.Position,
            "createdat" => s => s.CreatedAt,
            _ => s => s.StaffCode // Default to staff code for unrecognized properties
        };
    }
}   