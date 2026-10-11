using System.Collections.Generic;

namespace VbeSort.Models
{
    public class MockExamSetupViewModel
    {
        public List<string> AvailableTopics { get; set; } = new List<string>();
        public List<string> SelectedTopics { get; set; } = new List<string>();
        public int QuestionCount { get; set; } = 10;
    }

    public class MockExamViewModel
    {
        public List<Exercise> Exercises { get; set; } = new List<Exercise>();
    }
}

