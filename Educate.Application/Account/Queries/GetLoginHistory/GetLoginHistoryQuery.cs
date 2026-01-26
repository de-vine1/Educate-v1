using MediatR;

namespace Educate.Application.Account.Queries.GetLoginHistory;

public record GetLoginHistoryQuery(
    string UserId,
    int Page = 1,
    int PageSize = 20
) : IRequest<GetLoginHistoryResult>;

public record GetLoginHistoryResult(
    int CurrentPage,
    int TotalPages,
    int TotalRecords,
    List<LoginHistoryDto> Items
);

public record LoginHistoryDto(
    DateTime Timestamp,
    string IpAddress,
    string Device,
    string? Location,
    bool Success,
    string? FailureReason
);
