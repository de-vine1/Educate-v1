using MediatR;

namespace Educate.Application.Exams.Commands.CreateExam;

public record CreateExamCommand(
    string Title,
    Guid SubjectId,
    int Duration,
    int PassingScore,
    List<Guid> QuestionIds
) : IRequest<Guid>;
