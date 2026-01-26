using Educate.Application.Questions.Models;
using Educate.Application.Common.Models;
using Educate.Domain.Enums;
using MediatR;

namespace Educate.Application.Questions.Queries.GetQuestions;

public record GetQuestionsQuery(
    Guid? SubjectId = null,
    QuestionType? QuestionType = null,
    DifficultyLevel? DifficultyLevel = null,
    bool? IsActive = true,
    int Page = 1,
    int PageSize = 10
) : IRequest<PaginatedResult<QuestionDto>>;
