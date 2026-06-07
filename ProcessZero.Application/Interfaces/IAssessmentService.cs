using ProcessZero.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Interfaces
{
    public interface IAssessmentService
    {
        /// <summary>
        /// Get the assessment for a product (productId = 0 for the global/platform assessment).
        /// Returns null if no assessment JSON has been uploaded for the given productId.
        /// </summary>
        Task<AssessmentClientDto?> GetAssessmentAsync(int productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Submit answers for the assessment tied to a product (productId = 0 for global).
        /// </summary>
        Task<SubmissionResultDto> SubmitAsync(int productId, SubmissionDto submission, CancellationToken cancellationToken = default);

        /// <summary>
        /// Admin: upload / replace the assessment JSON for a product (productId = 0 for global).
        /// </summary>
        Task UploadAssessmentAsync(int productId, AssessmentDto assessment, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the latest submission result for the current logged-in user for a given productId.
        /// Returns null if the user has not submitted the assessment.
        /// </summary>
        Task<SubmissionResultDto?> GetMyResultAsync(int productId, CancellationToken cancellationToken = default);
        /// <summary>
        /// Admin: retrieve the raw assessment payload (including correct answers) for a product.
        /// Returns null if no assessment has been uploaded for the given productId.
        /// </summary>
        /// 
        /// <summary>
        /// Get all assessments available to the client.
        /// Returns an empty list if no assessments exist.
        /// </summary>
        Task<List<SubmissionResultDto>> GetAllMyResultsAsync(CancellationToken cancellationToken = default);

        Task<AssessmentDto?> GetAssessmentForAdminAsync(int productId, CancellationToken cancellationToken = default);
        /// <summary>
        /// Admin: retrieve the latest assessment for all products (including global) for management UI.
        /// Returns an empty list if no assessments exist.
        /// </summary>
        Task<List<AssessmentDto>> GetAllAssessmentsForAdminAsync(CancellationToken cancellationToken = default);
    }
}
