using MediatR;

namespace Educate.Application.Exams.Commands.SubmitAnswer;

public record SubmitAnswerCommand(
    Guid StudentExamId,
    Guid QuestionId,
    string AnswerText
) : IRequest<bool>; // Returns true if answer was graded (MCQ), false if awaiting manual grading
