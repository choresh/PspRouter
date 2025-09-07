using Npgsql;
using Pgvector;
using System.Text.Json;

namespace PspRouter.Lib;

public interface IVectorMemory
{
    Task EnsureSchemaAsync(CancellationToken ct);
    Task AddAsync(string key, string text, Dictionary<string,string> meta, float[] embedding, CancellationToken ct);
    Task<IReadOnlyList<(string key, string text, Dictionary<string,string> meta, double score)>> 
        SearchAsync(float[] queryEmbedding, int k, CancellationToken ct);
}

public sealed class PgVectorMemory : IVectorMemory
{
    private readonly string _connString;
    private readonly string _table;

    public PgVectorMemory(string connectionString, string table = "psp_lessons")
    {
        _connString = connectionString;
        _table = table;
    }

    public async Task EnsureSchemaAsync(CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync(ct);
        var sql = $@"
            CREATE EXTENSION IF NOT EXISTS vector;
            CREATE TABLE IF NOT EXISTS {_table} (
                key TEXT PRIMARY KEY,
                content TEXT NOT NULL,
                meta JSONB NOT NULL,
                embedding vector(3072)
            );
            CREATE INDEX IF NOT EXISTS {_table}_ivfflat ON {_table} USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
        ";
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task AddAsync(string key, string text, Dictionary<string,string> meta, float[] embedding, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand($@"
            INSERT INTO {_table}(key, content, meta, embedding) 
            VALUES (@k, @c, @m, @e)
            ON CONFLICT (key) DO UPDATE SET content=excluded.content, meta=excluded.meta, embedding=excluded.embedding;
        ", conn);
        cmd.Parameters.AddWithValue("k", key);
        cmd.Parameters.AddWithValue("c", text);
        cmd.Parameters.AddWithValue("m", JsonSerializer.Serialize(meta));
        cmd.Parameters.AddWithValue("e", new Vector(embedding));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<(string key, string text, Dictionary<string,string> meta, double score)>> 
        SearchAsync(float[] queryEmbedding, int k, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand($@"
            SELECT key, content, meta, 1 - (embedding <=> @q) AS score
            FROM {_table}
            ORDER BY embedding <=> @q
            LIMIT @k;
        ", conn);
        cmd.Parameters.AddWithValue("q", new Vector(queryEmbedding));
        cmd.Parameters.AddWithValue("k", k);
        var list = new List<(string, string, Dictionary<string,string>, double)>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var key = reader.GetString(0);
            var content = reader.GetString(1);
            var metaJson = reader.GetString(2);
            var meta = JsonSerializer.Deserialize<Dictionary<string,string>>(metaJson) ?? new();
            var score = reader.GetDouble(3);
            list.Add((key, content, meta, score));
        }
        return list;
    }
}
