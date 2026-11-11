using Dapper;
using SevaOne.Domain.Connection;
using SevaOne.Infrastructure.Repositories.Common;

namespace SevaOne.Infrastructure.Repositories.Portal;

public sealed class StoredDigiLockerDocument
{
    public Guid Id { get; set; }
    public string DocUri { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DocType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string UploadDate { get; set; } = string.Empty;
    public string[] Formats { get; set; } = [];
    public string[] StoredFormats { get; set; } = [];
    public DateTimeOffset SyncedAt { get; set; }
}

public sealed class StoredDigiLockerFile
{
    public string Name { get; set; } = string.Empty;
    public string Mime { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
}

public interface IDigiLockerDocumentRepository
{
    Task<Guid> UpsertAsync(Guid citizenId, string uri, string name, string type, string description, string issuer, string uploadDate, string[] formats, CancellationToken ct);
    Task<IReadOnlyList<StoredDigiLockerDocument>> ListAsync(Guid citizenId, CancellationToken ct);
    Task SaveFileAsync(Guid documentId, string format, string mime, int sizeBytes, string sha256, string storageKey, CancellationToken ct);
    Task<StoredDigiLockerFile?> GetFileAsync(Guid citizenId, Guid documentId, string format, CancellationToken ct);
}

public sealed class DigiLockerDocumentRepository : RepositoryBase, IDigiLockerDocumentRepository
{
    private readonly IDbConnectionFactory _factory;

    public DigiLockerDocumentRepository(IDbConnectionFactory factory) : base(factory)
    {
        _factory = factory;
    }

    public async Task<Guid> UpsertAsync(Guid citizenId, string uri, string name, string type, string description, string issuer, string uploadDate, string[] formats, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<Guid>(new CommandDefinition(
            "SELECT portal.sp_upsert_digilocker_document(@CitizenId, @Uri, @Name, @Type, @Description, @Issuer, @UploadDate, @Formats)",
            new { CitizenId = citizenId, Uri = uri, Name = name, Type = type, Description = description, Issuer = issuer, UploadDate = uploadDate, Formats = formats },
            cancellationToken: ct));
    }

    public async Task<IReadOnlyList<StoredDigiLockerDocument>> ListAsync(Guid citizenId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<StoredDigiLockerDocument>(new CommandDefinition(
            "SELECT * FROM portal.sp_list_digilocker_documents(@CitizenId)", new { CitizenId = citizenId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task SaveFileAsync(Guid documentId, string format, string mime, int sizeBytes, string sha256, string storageKey, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteScalarAsync(new CommandDefinition(
            "SELECT portal.sp_save_digilocker_document_file(@DocumentId, @Format, @Mime, @SizeBytes, @Sha256, @StorageKey)",
            new { DocumentId = documentId, Format = format, Mime = mime, SizeBytes = sizeBytes, Sha256 = sha256, StorageKey = storageKey },
            cancellationToken: ct));
    }

    public async Task<StoredDigiLockerFile?> GetFileAsync(Guid citizenId, Guid documentId, string format, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<StoredDigiLockerFile>(new CommandDefinition(
            "SELECT * FROM portal.sp_get_digilocker_document_file(@CitizenId, @DocumentId, @Format)",
            new { CitizenId = citizenId, DocumentId = documentId, Format = format }, cancellationToken: ct));
    }
}
