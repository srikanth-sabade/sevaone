namespace SevaOne.Infrastructure.Connectors;

public sealed class DepartmentConnectorRegistry : IDepartmentConnectorRegistry
{
    private readonly IReadOnlyList<IDepartmentConnector> _connectors;

    public DepartmentConnectorRegistry(IEnumerable<IDepartmentConnector> connectors)
    {
        _connectors = connectors.ToList();
    }

    public IReadOnlyCollection<IDepartmentConnector> All => _connectors;

    public IDepartmentConnector Get(string departmentCode)
    {
        var connector = _connectors.FirstOrDefault(c =>
            c.DepartmentCode.Equals(departmentCode, StringComparison.OrdinalIgnoreCase));
        return connector ?? throw new KeyNotFoundException($"No connector registered for department '{departmentCode}'.");
    }
}
