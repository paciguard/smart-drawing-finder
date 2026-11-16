using System.Collections.Generic;

namespace DrawingSearchTools.Models;

/// <summary>
/// Represents a single drawing entry parsed from the BOM Excel file
/// and the matching files discovered on disk.
/// </summary>
public class DrawingItem
{
    /// <summary>1-based row number in the source Excel sheet.</summary>
    public int RowNumber { get; set; }

    /// <summary>Original drawing number as found in column E, e.g. "4-104 064 010".</summary>
    public string DrawingNumber { get; set; } = string.Empty;

    /// <summary>Position number from column D, if any (e.g. "503.01").</summary>
    public string? PositionNumber { get; set; }

    /// <summary>Description from column F, if any.</summary>
    public string? Description { get; set; }

    /// <summary>Search keyword (e.g. "064010") or wildcard pattern (e.g. "*p*142159*").</summary>
    public string SearchKey { get; set; } = string.Empty;

    /// <summary>Absolute paths of matching files found during search.</summary>
    public List<string> MatchedFiles { get; } = new();

    /// <summary>Status text shown in the UI grid.</summary>
    public string Status { get; set; } = "Pending";
}

