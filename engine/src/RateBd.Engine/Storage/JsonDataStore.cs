using System.Text.Json;
using RateBd.Engine.Json;
using RateBd.Engine.Models;

namespace RateBd.Engine.Storage;

/// <summary>
/// File-based storage: <c>{root}/{corridor}.json</c> holds the latest snapshot and
/// <c>{root}/history/{corridor}/{yyyy-MM-dd}.json</c> keeps one snapshot per UTC day (a same-day rerun overwrites it).
/// </summary>
public sealed class JsonDataStore(string root)
{
    public string Root { get; } = Path.GetFullPath(root);

    public string LatestPath(string corridorId) => Path.Combine(Root, $"{corridorId}.json");

    public string HistoryPath(string corridorId, DateTime generatedAt) =>
        Path.Combine(Root, "history", corridorId, $"{generatedAt:yyyy-MM-dd}.json");

    public CorridorSnapshot? ReadLatest(string corridorId)
    {
        var path = LatestPath(corridorId);
        if (!File.Exists(path))
        {
            return null;
        }
        using var stream = File.OpenRead(path);
        var snapshot = JsonSerializer.Deserialize(stream, RateBdJsonContext.Default.CorridorSnapshot);
        // Snapshots written before posted rates existed have neither list.
        return snapshot is null
            ? null
            : snapshot with
            {
                PostedRateProviders = snapshot.PostedRateProviders ?? [],
                PostedRates = snapshot.PostedRates ?? [],
            };
    }

    public void Write(CorridorSnapshot snapshot)
    {
        var json = JsonSerializer.Serialize(snapshot, RateBdJsonContext.Default.CorridorSnapshot) + "\n";
        WriteAtomically(LatestPath(snapshot.Corridor), json);
        WriteAtomically(HistoryPath(snapshot.Corridor, snapshot.GeneratedAt), json);
    }

    private static void WriteAtomically(string path, string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".tmp";
        File.WriteAllText(temp, contents);
        File.Move(temp, path, overwrite: true);
    }

    /// <summary>Finds <c>data/</c> at the root of the git checkout that contains <paramref name="start"/>.</summary>
    public static string LocateDefaultRoot(string start)
    {
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        {
            // .git is a directory in a normal clone and a file in linked worktrees and submodules.
            var git = Path.Combine(dir.FullName, ".git");
            if (Directory.Exists(git) || File.Exists(git))
            {
                return Path.Combine(dir.FullName, "data");
            }
        }
        return Path.Combine(start, "data");
    }
}
