using ExpenseManager.Application.ReceiptExpenseLinks.Requests;
using FluentValidation;

namespace ExpenseManager.Application.ReceiptExpenseLinks.Validators;

public sealed class CreateReceiptExpenseLinkRequestValidator : AbstractValidator<CreateReceiptExpenseLinkRequest>
{
    public CreateReceiptExpenseLinkRequestValidator()
    {
        RuleFor(request => request.UserId)
            .NotEmpty();

        RuleFor(request => request.ExpenseId)
            .NotEmpty();

        RuleFor(request => request.ReceiptId)
            .NotEmpty();

        RuleFor(request => request.RequestedBy)
            .NotEmpty();

        RuleFor(request => request.Notes)
            .MaximumLength(500)
            .When(request => request.Notes is not null);
    }
}
