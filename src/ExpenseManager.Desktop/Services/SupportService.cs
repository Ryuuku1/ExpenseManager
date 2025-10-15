using System;
using System.Diagnostics;
using ExpenseManager.Desktop.Services.Options;
using Microsoft.Extensions.Options;

namespace ExpenseManager.Desktop.Services;

internal sealed class SupportService : ISupportService
{
    private readonly SupportOptions _options;

    public SupportService(IOptions<SupportOptions> options)
    {
        _options = options.Value;
    }

    public bool TryOpenDonationPage()
    {
        if (string.IsNullOrWhiteSpace(_options.PayPalEmail))
        {
            return false;
        }

        var currency = string.IsNullOrWhiteSpace(_options.CurrencyCode)
            ? "EUR"
            : _options.CurrencyCode!.ToUpperInvariant();

        var donationUri = BuildDonationUri(_options.PayPalEmail!, currency);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = donationUri,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildDonationUri(string email, string currencyCode)
    {
        var encodedEmail = Uri.EscapeDataString(email);
        var encodedCurrency = Uri.EscapeDataString(currencyCode);
        return $"https://www.paypal.com/donate/?business={encodedEmail}&currency_code={encodedCurrency}";
    }
}
