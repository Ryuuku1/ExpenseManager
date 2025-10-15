using System;

namespace ExpenseManager.Domain.Abstractions;

public abstract class Entity
{
    public Guid Id { get; protected set; }

    protected Entity(Guid id)
    {
        Id = id;
    }

    protected Entity()
    {
        Id = Guid.NewGuid();
    }
}