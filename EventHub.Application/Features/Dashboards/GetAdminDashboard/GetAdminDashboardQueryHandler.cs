using EventHub.Application.Common.Dtos.Dashboards;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Constants;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Dashboards.GetAdminDashboard
{
    public class GetAdminDashboardQueryHandler(
        IGenericRepository<Event> _eventRepository,
        IGenericRepository<Registration> _registrationRepository,
        IGenericRepository<PaymentTransaction> _paymentTransactionRepository,
        IUserManagementService _userManagementService,
        IUserContext _userContext,
        IDbExecutor _executor) : IRequestHandler<GetAdminDashboardQuery, RequestResult<AdminDashboardDto>>
    {
        public async Task<RequestResult<AdminDashboardDto>> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
        {
            if (!_userContext.IsInRole(RoleNames.Admin))
                return RequestResult<AdminDashboardDto>.Failure(ErrorCode.Forbidden);

            var events = _eventRepository.GetAll();
            var registrations = _registrationRepository.GetAll();
            var payments = _paymentTransactionRepository.GetAll();

            var eventStatusRows = await _executor.ToListAsync(
                events.GroupBy(@event => @event.Status)
                    .Select(group => new { Status = group.Key, Count = group.Count() }),
                cancellationToken);
            var paymentStatusRows = await _executor.ToListAsync(
                payments.GroupBy(payment => payment.Status)
                    .Select(group => new { Status = group.Key, Count = group.Count() }),
                cancellationToken);
            var totalEventRows = await _executor.ToListAsync(
                events.GroupBy(_ => 1).Select(group => group.Count()),
                cancellationToken);
            var totalRegistrationRows = await _executor.ToListAsync(
                registrations.GroupBy(_ => 1).Select(group => group.Count()),
                cancellationToken);
            var successfulPaymentAmountRows = await _executor.ToListAsync(
                payments.Where(payment => payment.Status == PaymentTransactionStatus.Success)
                    .GroupBy(_ => 1)
                    .Select(group => group.Sum(payment => payment.Amount)),
                cancellationToken);
            var totalUsers = await _userManagementService.GetUserCountAsync(cancellationToken);

            var paymentOverview = paymentStatusRows
                .Select(row => new PaymentStatusCountDto(row.Status.ToString(), row.Count))
                .ToList();

            return RequestResult<AdminDashboardDto>.Success(new AdminDashboardDto
            {
                TotalUsers = totalUsers,
                TotalEvents = totalEventRows.FirstOrDefault(),
                TotalRegistrations = totalRegistrationRows.FirstOrDefault(),
                EventStatusOverview = eventStatusRows
                    .Select(row => new EventStatusCountDto(row.Status.ToString(), row.Count))
                    .ToList(),
                Payments = new PaymentDashboardDto
                {
                    TotalTransactions = paymentOverview.Sum(payment => payment.Count),
                    SuccessfulPayments = paymentOverview
                        .Where(payment => payment.Status == PaymentTransactionStatus.Success.ToString())
                        .Sum(payment => payment.Count),
                    PendingPayments = paymentOverview
                        .Where(payment => payment.Status == PaymentTransactionStatus.Pending.ToString())
                        .Sum(payment => payment.Count),
                    FailedOrCanceledPayments = paymentOverview
                        .Where(payment => payment.Status is nameof(PaymentTransactionStatus.Failed) or nameof(PaymentTransactionStatus.Canceled))
                        .Sum(payment => payment.Count),
                    SuccessfulPaymentAmount = successfulPaymentAmountRows.FirstOrDefault(),
                    StatusOverview = paymentOverview
                }
            });
        }
    }
}
