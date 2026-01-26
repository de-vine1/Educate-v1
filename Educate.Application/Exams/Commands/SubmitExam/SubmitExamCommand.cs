using Educate.Application.Exams.Models;
using MediatR;

namespace Educate.Application.Exams.Commands.SubmitExam;

public record SubmitExamCommand(Guid StudentExamId) : IRequest<ExamResultDto>;
