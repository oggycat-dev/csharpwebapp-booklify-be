using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Booklify.Application.Common.DTOs.Statistics;
using Booklify.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Booklify.Application.Common.Models;
using AutoMapper;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.DTOs.User;

namespace Booklify.Application.Features.Statistics
{
    public class GetAdminStatisticsQueryHandler : IRequestHandler<GetAdminStatisticsQuery, Result<AdminStatisticsDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public GetAdminStatisticsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<AdminStatisticsDto>> Handle(GetAdminStatisticsQuery request, CancellationToken cancellationToken)
        {
            // Tổng người dùng (UserProfile, Status = Active)
            var totalUsers = await _unitOfWork.UserProfileRepository
                .CountAsync(u => u.Status == Booklify.Domain.Enums.EntityStatus.Active);

            // Tổng sách
            var totalBooks = await _unitOfWork.BookRepository.CountAsync(b => true);

            // Người dùng premium (UserSubscription.Status = Active, EndDate >= Now)
            var now = DateTime.UtcNow;
            var totalPremiumUsers = await _unitOfWork.UserSubscriptionRepository
                .CountAsync(us => us.Status == Booklify.Domain.Enums.EntityStatus.Active && us.EndDate >= now);

            // Tổng lượt đọc sách (sum Book.TotalViews)
            var bookQuery = _unitOfWork.BookRepository.FindByCondition(b => true); // build truy vấn IQueryable
            var totalBookReads = await bookQuery.SumAsync(b => b.TotalViews, cancellationToken);

            // Sách chờ duyệt (pending books) - lấy 5 sách mới nhất
            var pendingBooks = await _unitOfWork.BookRepository
                .FindByCondition(b => b.ApprovalStatus == Booklify.Domain.Enums.ApprovalStatus.Pending)
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .ToListAsync(cancellationToken);
            var pendingBooksDto = _mapper.Map<List<BookListItemResponse>>(pendingBooks);

            // User mới đăng ký (recent users) - lấy 2 user mới nhất
            var recentUsers = await _unitOfWork.UserProfileRepository
                .FindByCondition(u => u.Status == Booklify.Domain.Enums.EntityStatus.Active)
                .OrderByDescending(u => u.CreatedAt)
                .Take(2)
                .ToListAsync(cancellationToken);
            var recentUsersDto = _mapper.Map<List<UserResponse>>(recentUsers);

            // Sách mới được tải lên (recent books) - lấy 2 sách mới nhất
            var recentBooks = await _unitOfWork.BookRepository
                .FindByCondition(b => true)
                .OrderByDescending(b => b.CreatedAt)
                .Take(2)
                .ToListAsync(cancellationToken);
            var recentBooksDto = _mapper.Map<List<BookListItemResponse>>(recentBooks);

            var dto = new AdminStatisticsDto
            {
                TotalUsers = totalUsers,
                TotalBooks = totalBooks,
                TotalPremiumUsers = totalPremiumUsers,
                TotalBookReads = totalBookReads,
                PendingBooks = pendingBooksDto,
                RecentUsers = recentUsersDto,
                RecentBooks = recentBooksDto
            };
            return Result<AdminStatisticsDto>.Success(dto, "Lấy thống kê thành công");
        }
    }
} 