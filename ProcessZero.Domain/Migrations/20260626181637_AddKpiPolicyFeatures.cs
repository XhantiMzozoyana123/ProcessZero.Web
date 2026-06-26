using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessZero.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddKpiPolicyFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_KPIs_UserId_CreatedAt",
                table: "KPIs",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_KpiPolicies_EffectiveFrom",
                table: "KpiPolicies",
                column: "EffectiveFrom");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KPIs_UserId_CreatedAt",
                table: "KPIs");

            migrationBuilder.DropIndex(
                name: "IX_KpiPolicies_EffectiveFrom",
                table: "KpiPolicies");
        }
    }
}