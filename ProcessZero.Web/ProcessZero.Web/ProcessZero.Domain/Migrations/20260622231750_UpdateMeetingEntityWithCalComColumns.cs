using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessZero.Domain.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMeetingEntityWithCalComColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CalBookingId",
                table: "Meetings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CalBookingUid",
                table: "Meetings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Meetings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CancelledByEmail",
                table: "Meetings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Meetings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "Meetings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Meetings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MeetingUrl",
                table: "Meetings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RawPayload",
                table: "Meetings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Meetings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Meetings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalBookingId",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "CalBookingUid",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "CancelledByEmail",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "MeetingUrl",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "RawPayload",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Meetings");
        }
    }
}
