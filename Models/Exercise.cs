using System.Collections.Generic;

namespace VbeSort.Models
{
    public class Exercise
    {
        public string Id { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string Session { get; set; } = string.Empty;
        public int Number { get; set; }
        public string Topic { get; set; } = string.Empty;
        
        // Relative paths from the base directory or just file paths that our controller can resolve.
        public List<string> QuestionImages { get; set; } = new List<string>();
        public List<string> AnswerImages { get; set; } = new List<string>();
    }

    public class Topic
    {
        public string Name { get; set; } = string.Empty;
        public List<Exercise> Exercises { get; set; } = new List<Exercise>();
    }
}

