using System.Text.RegularExpressions;
using VbeSort.Infrastructure;
using VbeSort.Models;

namespace VbeSort.Services;

public class ExerciseService
{
    private readonly string _basePath;
    private List<Topic> _topics = new();
    private Dictionary<string, Exercise> _exercisesById = new(StringComparer.OrdinalIgnoreCase);

    public ExerciseService(IConfiguration config)
    {
        _basePath = config["VbeConfig:BasePath"] ?? Directory.GetCurrentDirectory();
        LoadData();
    }

    public List<Topic> GetTopics() => _topics;

    public Topic? GetTopic(string name) =>
        _topics.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public Exercise? GetExerciseById(string exerciseId) =>
        _exercisesById.TryGetValue(exerciseId, out var ex) ? ex : null;

    public IReadOnlyList<Exercise> GetAllExercises() => _exercisesById.Values.ToList();

    public List<ExamSessionOptionDto> GetAvailableExamSessions()
    {
        return _exercisesById.Values
            .GroupBy(e => $"{e.Year}_{e.Session}", StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var first = g.First();
                var sessName = first.Session == "pagr" ? "pagrindinė sesija" : "pakartotinė sesija";
                return new ExamSessionOptionDto(
                    Key: $"{first.Year}_{first.Session}",
                    Year: first.Year,
                    Session: first.Session,
                    Label: $"{first.Year} m. {sessName} ({g.Count()} užd.)",
                    ExerciseCount: g.Count()
                );
            })
            .OrderByDescending(x => x.Year)
            .ThenBy(x => x.Session)
            .ToList();
    }

    public List<Exercise> GetExercisesByExamSession(string year, string session)
    {
        return _exercisesById.Values
            .Where(e => e.Year.Equals(year, StringComparison.OrdinalIgnoreCase) &&
                        e.Session.Equals(session, StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.Number)
            .ToList();
    }

    public List<Exercise> BuildMockExam(
        IEnumerable<string> selectedTopics,
        int count,
        string mode,
        IReadOnlyDictionary<string, ExerciseProgress>? userProgress = null)
    {
        var clampedCount = Math.Clamp(count, 1, 50);
        var topicSet = new HashSet<string>(selectedTopics, StringComparer.OrdinalIgnoreCase);

        var candidates = _topics
            .Where(t => topicSet.Count == 0 || topicSet.Contains(t.Name))
            .SelectMany(t => t.Exercises)
            .GroupBy(e => e.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        userProgress ??= new Dictionary<string, ExerciseProgress>();

        switch (mode)
        {
            case "unseen_only":
            {
                var unseen = candidates.Where(e =>
                    !userProgress.TryGetValue(e.Id, out var p) || p.Status == ExerciseStatus.Unseen).ToList();
                if (unseen.Count > 0) candidates = unseen;
                break;
            }
            case "review_due":
            {
                return candidates
                    .Where(e => userProgress.TryGetValue(e.Id, out var p) &&
                                (p.Status == ExerciseStatus.Hard || p.Status == ExerciseStatus.NeedsReview))
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(clampedCount)
                    .ToList();
            }
        }

        return candidates
            .OrderBy(_ => Random.Shared.Next())
            .Take(clampedCount)
            .ToList();
    }

    private void LoadData()
    {
        _topics = new List<Topic>();
        _exercisesById = new Dictionary<string, Exercise>(StringComparer.OrdinalIgnoreCase);

        var surusiuotosPath = Path.Combine(_basePath, "surusiuotos");
        var atsakymaiPath = Path.Combine(_basePath, "atsakymai");

        if (!Directory.Exists(surusiuotosPath)) return;

        var answersMap = Directory.Exists(atsakymaiPath)
            ? BuildAnswersMap(atsakymaiPath)
            : new Dictionary<string, List<string>>();

        foreach (var topicDir in Directory.GetDirectories(surusiuotosPath))
        {
            var topicName = Path.GetFileName(topicDir);
            var topic = new Topic { Name = topicName };
            var exerciseDict = new Dictionary<string, Exercise>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in Directory.GetFiles(topicDir, "*.png"))
            {
                var match = Regex.Match(Path.GetFileNameWithoutExtension(file), @"^(\d{4})_(pagr|pak)_(\d+)(.*)$");
                if (!match.Success) continue;

                var (year, session, numStr) = (match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
                var number = int.Parse(numStr);
                var id = $"{year}_{session}_{numStr}";

                if (!exerciseDict.TryGetValue(id, out var exercise))
                {
                    exercise = new Exercise
                    {
                        Id = id,
                        Year = year,
                        Session = session,
                        Number = number,
                        Topic = topicName,
                        OfficialPdfPath = $"Home/ExamPdf?year={year}&session={session}"
                    };
                    exerciseDict[id] = exercise;
                }

                var relPath = $"surusiuotos/{topicName}/{Path.GetFileName(file)}";
                exercise.QuestionImages.Add(relPath);
            }

            foreach (var ex in exerciseDict.Values)
            {
                ex.QuestionImages = SortImagesNaturally(ex.QuestionImages);
                ex.QuestionAssets = ex.QuestionImages.Select(p => BuildImageAsset(p, isSharedRange: false, null, null)).ToList();

                var answerMatches = FindAnswersWithMetadata(ex, answersMap);
                ex.AnswerAssets = answerMatches;
                ex.AnswerImages = answerMatches.Select(a => a.Path).ToList();

                topic.Exercises.Add(ex);

                if (!_exercisesById.ContainsKey(ex.Id))
                {
                    _exercisesById[ex.Id] = ex;
                }
            }

            topic.Exercises = topic.Exercises
                .OrderByDescending(e => e.Year)
                .ThenBy(e => e.Session)
                .ThenBy(e => e.Number)
                .ToList();

            _topics.Add(topic);
        }

        _topics = _topics.OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private Dictionary<string, List<string>> BuildAnswersMap(string atsakymaiPath)
    {
        var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(atsakymaiPath, "*.png", SearchOption.AllDirectories))
        {
            var match = Regex.Match(Path.GetFileName(file), @"^(\d{4})_(pagr|pak)_(.+)\.png$", RegexOptions.IgnoreCase);
            if (!match.Success) continue;

            var key = $"{match.Groups[1].Value}_{match.Groups[2].Value.ToLowerInvariant()}";
            if (!map.TryGetValue(key, out var list))
            {
                list = new List<string>();
                map[key] = list;
            }
            list.Add(Path.GetRelativePath(_basePath, file).Replace("\\", "/"));
        }
        return map;
    }

    private List<ImageAsset> FindAnswersWithMetadata(Exercise ex, Dictionary<string, List<string>> answersMap)
    {
        var key = $"{ex.Year}_{ex.Session}";
        var result = new List<ImageAsset>();
        if (!answersMap.TryGetValue(key, out var files)) return result;

        var exact = SortImagesNaturally(
            files.Where(f => Regex.IsMatch(Path.GetFileNameWithoutExtension(f), $@"^{ex.Year}_{ex.Session}_{ex.Number}(?!\d)(.*)$", RegexOptions.IgnoreCase)));

        if (exact.Count > 0)
        {
            return exact.Select(f => BuildImageAsset(f, isSharedRange: false, null, null)).ToList();
        }

        foreach (var file in SortImagesNaturally(files))
        {
            var match = Regex.Match(Path.GetFileNameWithoutExtension(file), $@"^{ex.Year}_{ex.Session}_(\d+)-(\d+)(.*)$", RegexOptions.IgnoreCase);
            if (!match.Success) continue;

            var start = int.Parse(match.Groups[1].Value);
            var end = int.Parse(match.Groups[2].Value);
            if (ex.Number >= start && ex.Number <= end)
            {
                result.Add(BuildImageAsset(file, isSharedRange: true, start, end));
            }
        }

        return result;
    }

    public static List<string> SortImagesNaturally(IEnumerable<string> paths)
    {
        return paths
            .OrderBy(GetImagePartNumber)
            .ThenBy(p => Path.GetFileNameWithoutExtension(p), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int GetImagePartNumber(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var m = Regex.Match(name, @"\((\d+)\)\s*$");
        return m.Success && int.TryParse(m.Groups[1].Value, out var part) ? part : 1;
    }

    private ImageAsset BuildImageAsset(string relativePath, bool isSharedRange, int? start, int? end)
    {
        var fullPath = Path.Combine(_basePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var dims = PngDimensionReader.TryRead(fullPath);

        return new ImageAsset
        {
            Path = relativePath,
            Width = dims?.Width ?? 800,
            Height = dims?.Height ?? 320,
            IsSharedRange = isSharedRange,
            RangeStart = start,
            RangeEnd = end
        };
    }
}
