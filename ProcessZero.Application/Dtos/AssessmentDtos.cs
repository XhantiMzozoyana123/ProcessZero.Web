using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Dtos
{
    /// <summary>
    /// MCQ question. The JSON file must include CorrectIndex so the service can score automatically.
    /// When served to the client (GET), CorrectIndex is stripped so candidates cannot cheat.
    /// </summary>
    public class QuestionDto
    {
        public string Text { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new List<string>();
        public int CorrectIndex { get; set; }
        public int Weight { get; set; } = 1;
    }

    public class OpenQuestionDto
    {
        public string Text { get; set; } = string.Empty;
        public int Weight { get; set; } = 2;
    }

    /// <summary>
    /// Full assessment definition stored as JSON on disk.
    /// ProductId = 0 means global (platform-wide) assessment.
    /// ProductId > 0 targets a specific product.
    /// PassMark is per-assessment and overrides the global default when present.
    /// </summary>
    public class AssessmentDto
    {
        public string Title { get; set; } = string.Empty;
        // Optional human-friendly description explaining expectations for this assessment
        public string Description { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public double? PassMark { get; set; }
        public List<QuestionDto> MCQs { get; set; } = new List<QuestionDto>();
        public List<OpenQuestionDto> OpenQuestions { get; set; } = new List<OpenQuestionDto>();
    }

    /// <summary>
    /// Client-facing assessment (CorrectIndex removed from MCQs).
    /// </summary>
    public class AssessmentClientDto
    {
        public string Title { get; set; } = string.Empty;
        // Optional description to show to clients explaining expectations
        public string Description { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public List<QuestionClientDto> MCQs { get; set; } = new List<QuestionClientDto>();
        public List<OpenQuestionDto> OpenQuestions { get; set; } = new List<OpenQuestionDto>();
    }

    public class QuestionClientDto
    {
        public string Text { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new List<string>();
        public int Weight { get; set; } = 1;
    }

    public class SubmissionDto
    {
        public List<int> McqAnswers { get; set; } = new List<int>();
        public List<string?> OpenAnswers { get; set; } = new List<string?>();
    }

    public class SubmissionResultDto
    {
        public int ProductId { get; set; }
        public int Score { get; set; }
        public int Total { get; set; }
        public double Percentage { get; set; }
        public bool Passed { get; set; }
    }
}
