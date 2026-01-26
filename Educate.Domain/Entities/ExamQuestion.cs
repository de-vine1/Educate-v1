namespace Educate.Domain.Entities;

public class ExamQuestion
{
    public Guid Id { get; private set; }
    public Guid ExamId { get; private set; }
    public Guid QuestionId { get; private set; }
    public int OrderNumber { get; private set; }

    public Exam Exam { get; set; } = null!;
    public Question Question { get; set; } = null!;

    private ExamQuestion() { } // EF Core

    public static ExamQuestion Create(Guid examId, Guid questionId, int orderNumber)
    {
        if (orderNumber <= 0)
            throw new ArgumentException("Order number must be greater than zero");

        return new ExamQuestion
        {
            Id = Guid.NewGuid(),
            ExamId = examId,
            QuestionId = questionId,
            OrderNumber = orderNumber
        };
    }
}
