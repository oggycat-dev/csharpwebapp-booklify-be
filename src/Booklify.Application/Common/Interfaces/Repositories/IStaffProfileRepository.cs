using Booklify.Domain.Entities;
using Booklify.Application.Common.DTOs.Staff;

namespace Booklify.Application.Common.Interfaces.Repositories;

public interface IStaffProfileRepository : IGenericRepository<StaffProfile>
{
    Task<(List<StaffProfile> Staffs, int TotalCount)> GetPagedStaffsAsync(StaffFilterModel filter);
}