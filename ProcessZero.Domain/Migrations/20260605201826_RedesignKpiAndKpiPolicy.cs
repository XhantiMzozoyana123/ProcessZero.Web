using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessZero.Domain.Migrations
{
    /// <inheritdoc />
    public partial class RedesignKpiAndKpiPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Remove legacy KPI columns ──────────────────────────
            migrationBuilder.DropColumn(name: "ActiveTeamSize", table: "KPIs");
            migrationBuilder.DropColumn(name: "ActivityConsistency", table: "KPIs");
            migrationBuilder.DropColumn(name: "AverageDealSize", table: "KPIs");
            migrationBuilder.DropColumn(name: "BasicClientRetention", table: "KPIs");
            migrationBuilder.DropColumn(name: "BrandCompliance", table: "KPIs");
            migrationBuilder.DropColumn(name: "BrandRiskManagement", table: "KPIs");
            migrationBuilder.DropColumn(name: "CallsAttended", table: "KPIs");
            migrationBuilder.DropColumn(name: "CallsBooked", table: "KPIs");
            migrationBuilder.DropColumn(name: "ClientRetention", table: "KPIs");
            migrationBuilder.DropColumn(name: "DealsAttempted", table: "KPIs");
            migrationBuilder.DropColumn(name: "DealsClosed", table: "KPIs");
            migrationBuilder.DropColumn(name: "DealsInfluenced", table: "KPIs");
            migrationBuilder.DropColumn(name: "GrowthRate", table: "KPIs");
            migrationBuilder.DropColumn(name: "InnovationContribution", table: "KPIs");
            migrationBuilder.DropColumn(name: "LeaderActivityLevel", table: "KPIs");
            migrationBuilder.DropColumn(name: "LeadershipStability", table: "KPIs");
            migrationBuilder.DropColumn(name: "LongTermRevenueGrowth", table: "KPIs");
            migrationBuilder.DropColumn(name: "OutreachAttempts", table: "KPIs");
            migrationBuilder.DropColumn(name: "RevenueGenerated", table: "KPIs");
            migrationBuilder.DropColumn(name: "RevenueInfluenced", table: "KPIs");
            migrationBuilder.DropColumn(name: "StrategicInitiativesDelivered", table: "KPIs");
            migrationBuilder.DropColumn(name: "TeamChurnRate", table: "KPIs");
            migrationBuilder.DropColumn(name: "TeamCloseRate", table: "KPIs");
            migrationBuilder.DropColumn(name: "TeamPerformanceHealth", table: "KPIs");
            migrationBuilder.DropColumn(name: "TeamRevenue", table: "KPIs");

            // ── Add new daily sales rep KPI columns ────────────────
            migrationBuilder.AddColumn<int>(name: "CallOutreach", table: "KPIs", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<int>(name: "EmailOutreach", table: "KPIs", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<int>(name: "CallsMade", table: "KPIs", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<int>(name: "MeetingsBooked", table: "KPIs", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<decimal>(name: "DealSizeClosed", table: "KPIs", type: "decimal(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<int>(name: "ActiveClients", table: "KPIs", type: "int", nullable: false, defaultValue: 0);

            // ── KpiPolicies: replace thresholds with a single MRR target ──
            migrationBuilder.DropColumn(name: "MinCallsBooked", table: "KpiPolicies");
            migrationBuilder.DropColumn(name: "MinOutreachAttempts", table: "KpiPolicies");

            migrationBuilder.RenameColumn(
                name: "MinMonthlyRevenue",
                table: "KpiPolicies",
                newName: "TargetMRR");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ── Revert KpiPolicies ─────────────────────────────────
            migrationBuilder.RenameColumn(
                name: "TargetMRR",
                table: "KpiPolicies",
                newName: "MinMonthlyRevenue");

            migrationBuilder.AddColumn<int>(name: "MinCallsBooked", table: "KpiPolicies", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<int>(name: "MinOutreachAttempts", table: "KpiPolicies", type: "int", nullable: false, defaultValue: 0);

            // ── Remove new KPI columns ─────────────────────────────
            migrationBuilder.DropColumn(name: "CallOutreach", table: "KPIs");
            migrationBuilder.DropColumn(name: "EmailOutreach", table: "KPIs");
            migrationBuilder.DropColumn(name: "CallsMade", table: "KPIs");
            migrationBuilder.DropColumn(name: "MeetingsBooked", table: "KPIs");
            migrationBuilder.DropColumn(name: "DealSizeClosed", table: "KPIs");
            migrationBuilder.DropColumn(name: "ActiveClients", table: "KPIs");

            // ── Restore legacy KPI columns ─────────────────────────
            migrationBuilder.AddColumn<int>(name: "ActiveTeamSize", table: "KPIs", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<double>(name: "ActivityConsistency", table: "KPIs", type: "float", nullable: false, defaultValue: 0.0);
            migrationBuilder.AddColumn<decimal>(name: "AverageDealSize", table: "KPIs", type: "decimal(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<double>(name: "BasicClientRetention", table: "KPIs", type: "float", nullable: false, defaultValue: 0.0);
            migrationBuilder.AddColumn<double>(name: "BrandCompliance", table: "KPIs", type: "float", nullable: false, defaultValue: 0.0);
            migrationBuilder.AddColumn<double>(name: "BrandRiskManagement", table: "KPIs", type: "float", nullable: false, defaultValue: 0.0);
            migrationBuilder.AddColumn<int>(name: "CallsAttended", table: "KPIs", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<int>(name: "CallsBooked", table: "KPIs", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<double>(name: "ClientRetention", table: "KPIs", type: "float", nullable: false, defaultValue: 0.0);
            migrationBuilder.AddColumn<int>(name: "DealsAttempted", table: "KPIs", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<int>(name: "DealsClosed", table: "KPIs", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<int>(name: "DealsInfluenced", table: "KPIs", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<double>(name: "GrowthRate", table: "KPIs", type: "float", nullable: false, defaultValue: 0.0);
            migrationBuilder.AddColumn<double>(name: "InnovationContribution", table: "KPIs", type: "float", nullable: false, defaultValue: 0.0);
            migrationBuilder.AddColumn<double>(name: "LeaderActivityLevel", table: "KPIs", type: "float", nullable: false, defaultValue: 0.0);
            migrationBuilder.AddColumn<double>(name: "LeadershipStability", table: "KPIs", type: "float", nullable: false, defaultValue: 0.0);
            migrationBuilder.AddColumn<double>(name: "LongTermRevenueGrowth", table: "KPIs", type: "float", nullable: false, defaultValue: 0.0);
            migrationBuilder.AddColumn<int>(name: "OutreachAttempts", table: "KPIs", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<decimal>(name: "RevenueGenerated", table: "KPIs", type: "decimal(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(name: "RevenueInfluenced", table: "KPIs", type: "decimal(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<int>(name: "StrategicInitiativesDelivered", table: "KPIs", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<double>(name: "TeamChurnRate", table: "KPIs", type: "float", nullable: false, defaultValue: 0.0);
            migrationBuilder.AddColumn<double>(name: "TeamCloseRate", table: "KPIs", type: "float", nullable: false, defaultValue: 0.0);
            migrationBuilder.AddColumn<double>(name: "TeamPerformanceHealth", table: "KPIs", type: "float", nullable: false, defaultValue: 0.0);
            migrationBuilder.AddColumn<decimal>(name: "TeamRevenue", table: "KPIs", type: "decimal(18,2)", nullable: false, defaultValue: 0m);
        }
    }
}
