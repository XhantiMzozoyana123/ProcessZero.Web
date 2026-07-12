using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Category of survey question: Contact (mandatory contact info) or Business (custom pain point questions)
    /// </summary>
    public enum QuestionCategory
    {
        Contact,
        Business
    }

    /// <summary>
    /// Type of survey question: MultipleChoice (has fixed options) or OpenEnded (free text)
    /// </summary>
    public enum SurveyQuestionType
    {
        MultipleChoice,
        OpenEnded
    }

    /// <summary>
    /// Individual question within a survey.
    /// Each question belongs to a Survey and can be either MultipleChoice or OpenEnded.
    /// Contact questions (email, name, phone, company, job, industry) are marked with Category = Contact.
    /// Multiple-choice options are stored as individual SurveyQuestionOption rows (no JSON).
    /// </summary>
    public class SurveyQuestion : BaseEntity
    {
        /// <summary>
        /// Foreign key to the survey this question belongs to
        /// </summary>
        public int SurveyId { get; set; }

        /// <summary>
        /// Navigation property to the survey
        /// </summary>
        public Survey? Survey { get; set; }

        /// <summary>
        /// The question text shown to respondents
        /// </summary>
        [MaxLength(500)]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Display order within the survey (0-6 for contact questions, 7+ for business questions)
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Category of question: Contact (mandatory contact info) or Business (custom pain point questions)
        /// </summary>
        public QuestionCategory Category { get; set; } = QuestionCategory.Business;

        /// <summary>
        /// Whether the respondent MUST answer this question.
        /// Contact questions 0-3 (email, firstName, lastName, phone) are typically required.
        /// Company/Job/Industry are optional.
        /// </summary>
        public bool IsRequired { get; set; } = true;

        /// <summary>
        /// Type of question: MultipleChoice (has fixed options) or OpenEnded (free text).
        /// Contact questions are always OpenEnded.
        /// Business questions can be either type.
        /// </summary>
        public SurveyQuestionType Type { get; set; } = SurveyQuestionType.OpenEnded;

        /// <summary>
        /// Selectable options for a MultipleChoice question (one SurveyQuestionOption row per option).
        /// Empty for OpenEnded questions.
        /// </summary>
        public ICollection<SurveyQuestionOption> Options { get; set; } = new List<SurveyQuestionOption>();
    }
}