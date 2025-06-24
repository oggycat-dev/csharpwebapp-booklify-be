using Booklify.Domain.Entities;
using Booklify.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileInfo = Booklify.Domain.Entities.FileInfo;

namespace Booklify.Application.Common.Interfaces
{
    public interface IBooklifyDbContext
    {
        DbSet<UserProfile> UserProfiles { get; set; }
        DbSet<StaffProfile> StaffProfiles { get; set; }
        DbSet<FileInfo> FileInfos { get; set; }
        DbSet<Book> Books { get; set; }
        DbSet<BookCategory> BookCategories { get; set; }
        DbSet<Subscription> Subscriptions { get; set; }
        DbSet<UserSubscription> UserSubscriptions { get; set; }
        DbSet<Payment> Payments { get; set; }
        DbSet<AppUser> IdentityUsers { get; set; }
        DbSet<AppRole> IdentityRoles { get; set; }
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
