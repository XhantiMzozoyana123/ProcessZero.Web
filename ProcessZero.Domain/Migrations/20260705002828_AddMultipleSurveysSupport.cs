using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessZero.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddMultipleSurveysSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SurveyRespondents_Email",
                table: "SurveyRespondents");

            migrationBuilder.AddColumn<int>(
                name: "SurveyId",
                table: "SurveyResponses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SurveyId",
                table: "SurveyRespondents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "SurveyQuestions",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "SurveyQuestions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Active")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponses_SurveyId",
                table: "SurveyResponses",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponses_SurveyId_SubmittedAt",
                table: "SurveyResponses",
                columns: new[] { "SurveyId", "SubmittedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyRespondents_SurveyId",
                table: "SurveyRespondents",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyRespondents_SurveyId_Email",
                table: "SurveyRespondents",
                columns: new[] { "SurveyId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveyQuestions_Status",
                table: "SurveyQuestions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyQuestions_UserId_Status",
                table: "SurveyQuestions",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_SurveyRespondents_SurveyQuestions_SurveyId",
                table: "SurveyRespondents",
                column: "SurveyId",
                principalTable: "SurveyQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SurveyResponses_SurveyQuestions_SurveyId",
                table: "SurveyResponses",
                column: "SurveyId",
                principalTable: "SurveyQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SurveyRespondents_SurveyQuestions_SurveyId",
                table: "SurveyRespondents");

            migrationBuilder.DropForeignKey(
                name: "FK_SurveyResponses_SurveyQuestions_SurveyId",
                table: "SurveyResponses");

            migrationBuilder.DropIndex(
                name: "IX_SurveyResponses_SurveyId",
                table: "SurveyResponses");

            migrationBuilder.DropIndex(
                name: "IX_SurveyResponses_SurveyId_SubmittedAt",
                table: "SurveyResponses");

            migrationBuilder.DropIndex(
                name: "IX_SurveyRespondents_SurveyId",
                table: "SurveyRespondents");

            migrationBuilder.DropIndex(
                name: "IX_SurveyRespondents_SurveyId_Email",
                table: "SurveyRespondents");

            migrationBuilder.DropIndex(
                name: "IX_SurveyQuestions_Status",
                table: "SurveyQuestions");

            migrationBuilder.DropIndex(
                name: "IX_SurveyQuestions_UserId_Status",
                table: "SurveyQuestions");

            migrationBuilder.DropColumn(
                name: "SurveyId",
                table: "SurveyResponses");

            migrationBuilder.DropColumn(
                name: "SurveyId",
                table: "SurveyRespondents");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "SurveyQuestions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "SurveyQuestions");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyRespondents_Email",
                table: "SurveyRespondents",
                column: "Email");
        }
    }
}
