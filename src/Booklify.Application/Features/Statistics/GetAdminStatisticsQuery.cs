using MediatR;
using Booklify.Application.Common.DTOs.Statistics;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Statistics
{
    public class GetAdminStatisticsQuery : IRequest<Result<AdminStatisticsDto>>
    {
    }
} 