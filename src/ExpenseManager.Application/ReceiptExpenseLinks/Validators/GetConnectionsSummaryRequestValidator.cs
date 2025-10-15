using ExpenseManager.Application.ReceiptExpenseLinks.Requests;
using FluentValidation;

namespace ExpenseManager.Application.ReceiptExpenseLinks.Validators;

public sealed class GetConnectionsSummaryRequestValidator : AbstractValidator<GetConnectionsSummaryRequest>
{
    public GetConnectionsSummaryRequestValidator()
    {
        RuleFor(request => request.UserId)
            .NotEmpty();

        RuleFor(request => request)
            .Must(request => !request.From.HasValue || !request.To.HasValue || request.From.Value <= request.To.Value)
            .WithMessage("The start date must be earlier than or equal to the end date.");
    }
}
