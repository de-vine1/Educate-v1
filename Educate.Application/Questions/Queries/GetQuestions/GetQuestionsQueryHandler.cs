using Educate.Application.Interfaces;
using Educate.Application.Questions.Models;
using Educate.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Questions.Queries.GetQuestions;

public class GetQuestionsQueryHandler : IRequestHandler<GetQuestionsQuery, PaginatedResult<QuestionDto>>
{
    private readonly IAppDbContext _context;

    public GetQuestionsQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<QuestionDto>> Handle(GetQuestionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Questions
            .Include(q => q.Subject)
            .AsNoTracking()
            .AsQueryable();

        if (request.SubjectId.HasValue)
            query = query.Where(q => q.SubjectId == request.SubjectId.Value);

        if (request.QuestionType.HasValue)
            query = query.Where(q => q.QuestionType == request.QuestionType.Value);

        if (request.DifficultyLevel.HasValue)
            query = query.Where(q => q.DifficultyLevel == request.DifficultyLevel.Value);

        if (request.IsActive.HasValue)
            query = query.Where(q => q.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(q => new QuestionDto(
                q.Id,
                q.SubjectId,
                q.Subject.Name,
                q.QuestionText,
                q.QuestionType,
                q.Options,
                q.CorrectAnswer,
                q.Points,
                q.DifficultyLevel,
                q.IsActive
            ))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<QuestionDto>(items, totalCount, request.Page, request.PageSize);
    }
}
