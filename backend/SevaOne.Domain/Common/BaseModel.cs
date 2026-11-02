namespace SevaOne.Domain.Common;

/// <summary>
/// Marker only — Dapper POCOs are flat, column-mapped data bags, not
/// EF-tracked entities. This exists so every model at least agrees on the Id
/// shape; there's no change-tracking or navigation-property magic to inherit.
/// </summary>
public abstract class BaseModel
{
    public Guid Id { get; set; }
}
