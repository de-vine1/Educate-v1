using Educate.Domain.Enums;
using MediatR;

namespace Educate.Application.Questions.Commands.CreateQuestion;

public record CreateQuestionCommand(
    Guid SubjectId,
    string QuestionText,
    QuestionType QuestionType,
    DifficultyLevel DifficultyLevel,
    int Points,
    string? Options,
    string? CorrectAnswer
) : IRequest<Guid>;
