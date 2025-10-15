using ExpenseManager.Application.ReceiptExpenseLinks.Requests;
using FluentValidation;

namespace ExpenseManager.Application.ReceiptExpenseLinks.Validators;

public sealed class DeleteReceiptExpenseLinkRequestValidator : AbstractValidator<DeleteReceiptExpenseLinkRequest>
{
    public DeleteReceiptExpenseLinkRequestValidator()
    {
        RuleFor(request => request.UserId)
            .NotEmpty();

        RuleFor(request => request.LinkId)
            .NotEmpty();

        RuleFor(request => request.RequestedBy)
            .NotEmpty();
    }
}
