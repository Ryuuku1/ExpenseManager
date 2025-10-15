using System;
using System.Collections.Generic;

namespace ExpenseManager.Desktop.Localization;

internal static class CategoryLocalization
{
    private static readonly IReadOnlyDictionary<string, string> NameKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Habitação"] = "CATEGORY_NAME_HOUSING",
        ["Alimentação"] = "CATEGORY_NAME_FOOD",
        ["Transporte"] = "CATEGORY_NAME_TRANSPORTATION",
        ["Lazer"] = "CATEGORY_NAME_LEISURE",
        ["Saúde"] = "CATEGORY_NAME_HEALTH",
        ["Educação"] = "CATEGORY_NAME_EDUCATION"
    };

    private static readonly IReadOnlyDictionary<string, string> DescriptionKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Despesas com casa"] = "CATEGORY_DESCRIPTION_HOUSING",
        ["Supermercado e refeições"] = "CATEGORY_DESCRIPTION_FOOD",
        ["Combustível, transportes públicos"] = "CATEGORY_DESCRIPTION_TRANSPORTATION",
        ["Entretenimento e hobbies"] = "CATEGORY_DESCRIPTION_LEISURE",
        ["Consultas e farmácia"] = "CATEGORY_DESCRIPTION_HEALTH",
        ["Cursos e formação"] = "CATEGORY_DESCRIPTION_EDUCATION"
    };

    public static string TranslateName(ILocalizationManager localization, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var trimmed = name.Trim();
        return NameKeys.TryGetValue(trimmed, out var key)
            ? localization.GetString(key)
            : localization.GetString(trimmed);
    }

    public static string? TranslateDescription(ILocalizationManager localization, string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return description;
        }

        var trimmed = description.Trim();
        return DescriptionKeys.TryGetValue(trimmed, out var key)
            ? localization.GetString(key)
            : localization.GetString(trimmed);
    }
}
