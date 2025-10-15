using System;
using System.Collections.Generic;
using ExpenseManager.Domain.Abstractions;

namespace ExpenseManager.Domain.Entities.Categories;

public sealed class Category : AggregateRoot
{
    private readonly List<Category> _children = new();

    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Guid? ParentId { get; private set; }
    public bool IsDefault { get; private set; }

    public IReadOnlyCollection<Category> Children => _children.AsReadOnly();

    private Category(Guid id, string name, string? description, Guid? parentId, bool isDefault) : base(id)
    {
        Name = name;
        Description = description;
        ParentId = parentId;
        IsDefault = isDefault;
    }

    private Category()
    {
        Name = string.Empty;
    }

    public static Category Create(string name, string? description = null, Guid? parentId = null, bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        }

        return new Category(Guid.NewGuid(), name.Trim(), description?.Trim(), parentId, isDefault);
    }

    public void UpdateDetails(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        }

        Name = name.Trim();
        Description = description?.Trim();
    }

    public void MarkAsDefault()
    {
        IsDefault = true;
    }
}