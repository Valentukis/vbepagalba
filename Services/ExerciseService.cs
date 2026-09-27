using System.Text.RegularExpressions;
using VbeSort.Models;

namespace VbeSort.Services
{
    public class ExerciseService
    {
        private readonly string _basePath;
        private List<Topic> _topics = new();

        public ExerciseService(IConfiguration config)
        {
            _basePath = config["VbeConfig:BasePath"] ?? Directory.GetCurrentDirectory();
            LoadData();
        }

        public List<Topic> GetTopics() => _topics;
        public Topic? GetTopic(string name) => _topics.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        private void LoadData()
        {
            _topics = new List<Topic>();
            var surusiuotosPath = Path.Combine(_basePath, "surusiuotos");
            var answersMap = BuildAnswersMap(Path.Combine(_basePath, "atsakymai"));

            foreach (var topicDir in Directory.GetDirectories(surusiuotosPath))
            {
                var topicName = Path.GetFileName(topicDir);
                var topic = new Topic { Name = topicName };
                var exerciseDict = new Dictionary<string, Exercise>();

                foreach (var file in Directory.GetFiles(topicDir, "*.png"))
                {
                    var match = Regex.Match(Path.GetFileNameWithoutExtension(file), @"^(\d{4})_(pagr|pak)_(\d+)(.*)$");
                    if (match.Success)
                    {
                        var (year, session, numStr) = (match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
                        var id = $"{year}_{session}_{numStr}";

                        if (!exerciseDict.TryGetValue(id, out var exercise))
                        {
                            exercise = new Exercise { Id = id, Year = year, Session = session, Number = int.Parse(numStr), Topic = topicName };
                            exerciseDict[id] = exercise;
                        }
                        exercise.QuestionImages.Add($"surusiuotos/{topicName}/{Path.GetFileName(file)}");
                    }
                }

                foreach (var ex in exerciseDict.Values)
                {
                    ex.QuestionImages.Sort();
                    ex.AnswerImages = FindAnswers(ex, answersMap);
                    topic.Exercises.Add(ex);
                }

                topic.Exercises = topic.Exercises.OrderByDescending(e => e.Year).ThenBy(e => e.Number).ToList();
                _topics.Add(topic);
            }
            _topics = _topics.OrderBy(t => t.Name).ToList();
        }

        private Dictionary<string, List<string>> BuildAnswersMap(string atsakymaiPath)
        {
            var map = new Dictionary<string, List<string>>();
            foreach (var file in Directory.GetFiles(atsakymaiPath, "*.png", SearchOption.AllDirectories))
            {
                var match = Regex.Match(Path.GetFileName(file), @"^(\d{4})_(pagr|pak)_(.+)\.png$");
                if (match.Success)
                {
                    var key = $"{match.Groups[1].Value}_{match.Groups[2].Value}";
                    if (!map.ContainsKey(key)) map[key] = new List<string>();
                    map[key].Add(Path.GetRelativePath(_basePath, file).Replace("\\", "/"));
                }
            }
            return map;
        }

        private List<string> FindAnswers(Exercise ex, Dictionary<string, List<string>> answersMap)
        {
            var key = $"{ex.Year}_{ex.Session}";
            var answers = new List<string>();
            if (!answersMap.TryGetValue(key, out var files)) return answers;

            var exact = files.Where(f => Regex.IsMatch(Path.GetFileNameWithoutExtension(f), $@"^{ex.Year}_{ex.Session}_{ex.Number}(?!\d)(.*)$")).ToList();
            if (exact.Any()) { exact.Sort(); return exact; }

            foreach (var file in files)
            {
                var match = Regex.Match(Path.GetFileNameWithoutExtension(file), $@"^{ex.Year}_{ex.Session}_(\d+)-(\d+)(.*)$");
                if (match.Success && ex.Number >= int.Parse(match.Groups[1].Value) && ex.Number <= int.Parse(match.Groups[2].Value))
                {
                    answers.Add(file);
                }
            }
            answers.Sort();
            return answers;
        }
    }
}
