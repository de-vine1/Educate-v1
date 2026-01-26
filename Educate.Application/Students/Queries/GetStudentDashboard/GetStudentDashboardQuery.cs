using Educate.Application.Students.Models;
using MediatR;

namespace Educate.Application.Students.Queries.GetStudentDashboard;

public record GetStudentDashboardQuery(string UserId) : IRequest<StudentDashboardDto>;
