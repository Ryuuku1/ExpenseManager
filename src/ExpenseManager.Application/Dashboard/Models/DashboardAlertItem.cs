using System;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Application.Dashboard.Models;

public sealed record DashboardAlertItem(
	Guid EventId,
	string Title,
	DateTime OccursAt,
	AlertType AlertType,
	bool IsRecurring,
	bool IsDismissed);