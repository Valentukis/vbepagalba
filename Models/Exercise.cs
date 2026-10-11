using System.Collections.Generic;

namespace VbeSort.Models
{
    public class Exercise
    {
        public string Id { get; set; }
        public string Year { get; set; }
        public string Session { get; set; }
        public int Number { get; set; }
        public string Topic { get; set; }
        
        // Relative paths from the base directory or just file paths that our controller can resolve.
        public List<string> QuestionImages { get; set; } = new List<string>();
        public List<string> AnswerImages { get; set; } = new List<string>();
    }

    public class Topic
    {
        public string Name { get; set; }
        public List<Exercise> Exercises { get; set; } = new List<Exercise>();
    }
}

