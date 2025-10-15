using ExpenseManager.Domain.Abstractions;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    private Money(decimal amount, Currency currency)
    {
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency;
    }

    public static Money Create(decimal amount, Currency currency)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be greater than or equal to zero.");
        }

        return new Money(amount, currency);
    }

    public static Money Zero(Currency currency)
    {
        return new Money(0m, currency);
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException("Cannot add money values with different currencies.");
        }

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException("Cannot subtract money values with different currencies.");
        }

        var result = Amount - other.Amount;
        if (result < 0)
        {
            throw new InvalidOperationException("Resulting money amount cannot be negative.");
        }

        return new Money(result, Currency);
    }

    public Money MultiplyByPercent(decimal percent)
    {
        if (percent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), percent, "Percentage must be greater than or equal to zero.");
        }

        var factor = percent / 100m;
        return new Money(Amount * factor, Currency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString()
    {
        return $"{Amount:0.00} {Currency}";
    }
}