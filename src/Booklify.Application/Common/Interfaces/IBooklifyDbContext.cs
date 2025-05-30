using Booklify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Booklify.Application.Common.Interfaces
{
    public interface IBooklifyDbContext
    {
        DbSet<UserProfile> UserProfiles { get; set; }
    }
}
