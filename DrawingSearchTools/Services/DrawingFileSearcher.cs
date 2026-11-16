using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using DrawingSearchTools.Models;

namespace DrawingSearchTools.Services;

/// <summary>
/// Recursively searches one or more root directories for drawing files
/// whose file name contains any of the supplied search keys. Supports
/// cancellation and progress reporting and is robust against
/// permission-denied folders.
/// </summary>
public class DrawingFileSearcher
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".dwg", ".pdf", ".tif", ".tiff" };

    /// <summary>Folder names that should always be skipped.</summary>
    private static readonly HashSet<string> SkipFolderNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "$RECYCLE.BIN",
            "System Volume Information",
            "Recovery",
            ".git",
            "node_modules",
        };

    /// <summary>
    /// Performs the search on a background thread.
    /// </summary>
    /// <param name="rootDirectories">Folders to scan (e.g. "E:\\").</param>
    /// <param name="items">Drawing items to match against (their SearchKey is used).</param>
    /// <param name="progress">
    /// Reports the number of folders scanned plus the path of the folder currently being scanned.
    /// </param>
    /// <param name="cancellationToken">Allows the caller to cancel.</param>
    public Task SearchAsync(
        IEnumerable<string> rootDirectories,
        IList<DrawingItem> items,
        IProgress<SearchProgress>? progress,
        CancellationToken cancellationToken)
    {
        // Build a dictionary of search key -> DrawingItem for O(1) match.
        var keyToItem = new Dictionary<string, DrawingItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            item.MatchedFiles.Clear();
            item.Status = "Pending";
            if (!string.IsNullOrWhiteSpace(item.SearchKey))
                keyToItem[item.SearchKey] = item;
        }

        return Task.Run(() =>
        {
            int foldersScanned = 0;
            int filesScanned = 0;

            foreach (var root in rootDirectories)
            {
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                    continue;

                WalkDirectory(
                    root,
                    keyToItem,
                    progress,
                    cancellationToken,
                    ref foldersScanned,
                    ref filesScanned);
            }

            // Finalize statuses
            foreach (var it in items)
            {
                it.Status = it.MatchedFiles.Count switch
                {
                    0 => "Not found",
                    1 => "1 match",
                    _ => $"{it.MatchedFiles.Count} matches",
                };
            }

            // Remove duplicates (same file name, different directories) —
            // keep the file with the most-recent LastWriteTime.
            DeduplicateByFileName(items);

            progress?.Report(new SearchProgress
            {
                FoldersScanned = foldersScanned,
                FilesScanned = filesScanned,
                CurrentPath = "Done",
                IsFinal = true,
            });
        }, cancellationToken);
    }

    /// <summary>
    /// For each <see cref="DrawingItem"/>, removes duplicate matched files that
    /// share the same file name (case-insensitive) across different directories.
    /// When duplicates exist, the file with the most-recent
    /// <see cref="FileInfo.LastWriteTimeUtc"/> is kept; ties are broken by
    /// preferring the shortest full path (a rough proxy for "closer to root").
    /// </summary>
    public static void DeduplicateByFileName(IList<DrawingItem> items)
    {
        foreach (var item in items)
        {
            if (item.MatchedFiles.Count <= 1)
                continue;

            // Group by file extension only — keep one file per type (.dwg, .pdf, etc.).
            var groups = item.MatchedFiles
                .GroupBy(f => Path.GetExtension(f), StringComparer.OrdinalIgnoreCase);

            var kept = new List<string>();
            foreach (var grp in groups)
            {
                // Within each same-name group, keep the newest file.
                string best = grp
                    .OrderByDescending(f =>
                    {
                        try   { return new FileInfo(f).LastWriteTimeUtc; }
                        catch { return DateTime.MinValue; }
                    })
                    .ThenBy(f => f.Length)   // shorter path wins on tie
                    .First();

                kept.Add(best);
            }

            item.MatchedFiles.Clear();
            item.MatchedFiles.AddRange(kept);
        }
    }

    private void WalkDirectory(
        string path,
        Dictionary<string, DrawingItem> keyToItem,
        IProgress<SearchProgress>? progress,
        CancellationToken cancellationToken,
        ref int foldersScanned,
        ref int filesScanned)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var name = new DirectoryInfo(path).Name;
        if (SkipFolderNames.Contains(name))
            return;

        foldersScanned++;
        // Throttle progress reporting to avoid flooding the UI thread.
        if (foldersScanned % 25 == 0)
        {
            progress?.Report(new SearchProgress
            {
                FoldersScanned = foldersScanned,
                FilesScanned = filesScanned,
                CurrentPath = path,
            });
        }

        // ---- files ----
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(path);
        }
        catch (UnauthorizedAccessException) { files = Array.Empty<string>(); }
        catch (PathTooLongException)        { files = Array.Empty<string>(); }
        catch (DirectoryNotFoundException)  { files = Array.Empty<string>(); }
        catch (IOException)                 { files = Array.Empty<string>(); }

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            filesScanned++;

            string ext = Path.GetExtension(file);
            if (!AllowedExtensions.Contains(ext))
                continue;

            string fileNameNoExt = Path.GetFileNameWithoutExtension(file);

            // For each search key, check if the file name contains it.
            // We compare against fileNameNoExt to avoid matching part of
            // the extension or path. Using IndexOf with OrdinalIgnoreCase
            // is faster than building regexes.
            foreach (var kv in keyToItem)
            {
                if (MatchesSearchKey(fileNameNoExt, kv.Key))
                {
                    lock (kv.Value.MatchedFiles)
                    {
                        if (!kv.Value.MatchedFiles.Contains(file,
                                StringComparer.OrdinalIgnoreCase))
                        {
                            kv.Value.MatchedFiles.Add(file);
                        }
                    }
                }
            }
        }

        // ---- subdirectories ----
        IEnumerable<string> subDirs;
        try
        {
            subDirs = Directory.EnumerateDirectories(path);
        }
        catch (UnauthorizedAccessException) { return; }
        catch (PathTooLongException)        { return; }
        catch (DirectoryNotFoundException)  { return; }
        catch (IOException)                 { return; }

        foreach (var sub in subDirs)
        {
            // Skip reparse points (junctions / symlinks) so we don't loop.
            try
            {
                var info = new DirectoryInfo(sub);
                if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
                    continue;
            }
            catch { continue; }

            WalkDirectory(sub, keyToItem, progress, cancellationToken,
                          ref foldersScanned, ref filesScanned);
        }
    }

    /// <summary>Matches ordered literal segments; '*' allows any intervening text.</summary>
    public static bool MatchesSearchKey(string fileNameNoExt, string searchKey)
    {
        if (string.IsNullOrWhiteSpace(searchKey)) return false;
        if (searchKey.All(char.IsDigit))
        {
            // A drawing's numeric groups may be separated in the filename.
            string numericPattern = string.Join(@"[.\s_-]*", searchKey.Select(c => Regex.Escape(c.ToString())));
            return Regex.IsMatch(fileNameNoExt, numericPattern, RegexOptions.CultureInvariant);
        }
        int start = 0;
        foreach (string segment in searchKey.Split('*', StringSplitOptions.RemoveEmptyEntries))
        {
            int index = fileNameNoExt.IndexOf(segment, start, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return false;
            start = index + segment.Length;
        }
        return searchKey.Any(c => c != '*');
    }
}

/// <summary>Progress data reported by <see cref="DrawingFileSearcher"/>.</summary>
public class SearchProgress
{
    public int FoldersScanned { get; set; }
    public int FilesScanned   { get; set; }
    public string CurrentPath { get; set; } = string.Empty;
    public bool IsFinal       { get; set; }
}

