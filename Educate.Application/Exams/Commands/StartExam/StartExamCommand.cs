using Educate.Application.Exams.Models;
using MediatR;

namespace Educate.Application.Exams.Commands.StartExam;

public record StartExamCommand(Guid ExamId, string UserId) : IRequest<ExamAttemptDto>;
