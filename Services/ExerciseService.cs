using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using VbeSort.Models;

namespace VbeSort.Services
{
    public class ExerciseService
    {
        private readonly string _basePath;
        private List<Topic> _topics;

        public ExerciseService(IConfiguration config)
        {
            // Default to parent directory if not configured
            _basePath = config["VbeConfig:BasePath"] ?? "..";
            LoadData();
        }

        public List<Topic> GetTopics() => _topics;
        
        public Topic GetTopic(string name) => _topics.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        private void LoadData()
        {
            _topics = new List<Topic>();
            var surusiuotosPath = Path.Combine(_basePath, "surusiuotos");
            var atsakymaiPath = Path.Combine(_basePath, "atsakymai");

            if (!Directory.Exists(surusiuotosPath)) return;

            var answersMap = BuildAnswersMap(atsakymaiPath);

            var topicDirs = Directory.GetDirectories(surusiuotosPath);
            foreach (var topicDir in topicDirs)
            {
                var topicName = Path.GetFileName(topicDir);
                var topic = new Topic { Name = topicName };
                
                var files = Directory.GetFiles(topicDir, "*.png");
                
                var exerciseDict = new Dictionary<string, Exercise>();

                foreach (var file in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var match = Regex.Match(fileName, @"^(\d{4})_(pagr|pak)_(\d+)(.*)$");
                    if (match.Success)
                    {
                        var year = match.Groups[1].Value;
                        var session = match.Groups[2].Value;
                        var numStr = match.Groups[3].Value;
                        var id = $"{year}_{session}_{numStr}";

                        if (!exerciseDict.TryGetValue(id, out var exercise))
                        {
                            exercise = new Exercise
                            {
                                Id = id,
                                Year = year,
                                Session = session,
                                Number = int.Parse(numStr),
                                Topic = topicName
                            };
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
            if (!Directory.Exists(atsakymaiPath)) return map;

            var files = Directory.GetFiles(atsakymaiPath, "*.png", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var filename = Path.GetFileName(file);
                var match = Regex.Match(filename, @"^(\d{4})_(pagr|pak)_(.+)\.png$");
                if (match.Success)
                {
                    var key = $"{match.Groups[1].Value}_{match.Groups[2].Value}";
                    if (!map.ContainsKey(key))
                    {
                        map[key] = new List<string>();
                    }
                    var relPath = Path.GetRelativePath(_basePath, file).Replace("\\", "/");
                    map[key].Add(relPath);
                }
            }

            return map;
        }

        private List<string> FindAnswers(Exercise ex, Dictionary<string, List<string>> answersMap)
        {
            var key = $"{ex.Year}_{ex.Session}";
            var answers = new List<string>();

            if (!answersMap.TryGetValue(key, out var files))
            {
                return answers;
            }

            var exactMatches = files.Where(f => Regex.IsMatch(Path.GetFileNameWithoutExtension(f), $@"^{ex.Year}_{ex.Session}_{ex.Number}(?!\d)(.*)$")).ToList();
            if (exactMatches.Any())
            {
                answers.AddRange(exactMatches);
                answers.Sort();
                return answers;
            }

            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var match = Regex.Match(name, $@"^{ex.Year}_{ex.Session}_(\d+)-(\d+)(.*)$");
                if (match.Success)
                {
                    var start = int.Parse(match.Groups[1].Value);
                    var end = int.Parse(match.Groups[2].Value);
                    if (ex.Number >= start && ex.Number <= end)
                    {
                        answers.Add(file);
                    }
                }
            }
            
            answers.Sort();
            return answers;
        }
    }
}
