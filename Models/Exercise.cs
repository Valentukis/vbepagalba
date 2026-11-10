namespace VbeSort.Models;

/// <summary>
/// Paveikslėlio metaduomenys su tiksliais pikselių matmenimis iš PNG IHDR antraštės.
/// </summary>
public sealed class ImageAsset
{
    public string Path { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public bool IsSharedRange { get; init; }
    public int? RangeStart { get; init; }
    public int? RangeEnd { get; init; }

    public double AspectRatio => Height > 0 ? Math.Round((double)Width / Height, 2) : 1.0;
    public bool IsWideStrip => AspectRatio >= 5.5;
    public bool IsTallColumn => AspectRatio <= 0.9 && Height > 420;

    public string RangeLabel =>
        IsSharedRange && RangeStart.HasValue && RangeEnd.HasValue
            ? $"{RangeStart}–{RangeEnd} užd. bendra lentelė"
            : string.Empty;
}

public class Exercise
{
    public string Id { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public string Session { get; set; } = string.Empty;
    public int Number { get; set; }
    public string Topic { get; set; } = string.Empty;

    public List<string> QuestionImages { get; set; } = new();
    public List<string> AnswerImages { get; set; } = new();

    public List<ImageAsset> QuestionAssets { get; set; } = new();
    public List<ImageAsset> AnswerAssets { get; set; } = new();

    public string? OfficialPdfPath { get; set; }

    public int EstimatedDifficulty => Number switch
    {
        <= 10 => 1,
        <= 18 => 2,
        _ => 3
    };

    public int SuggestedPoints => EstimatedDifficulty switch
    {
        1 => 1,
        2 => 2,
        _ => 4
    };

    public string SessionShortLabel => Session == "pagr" ? "Pagr." : "Pak.";
}

public class Topic
{
    public string Name { get; set; } = string.Empty;
    public List<Exercise> Exercises { get; set; } = new();
}
