using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationFormFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AvailableImmediately",
                table: "applications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CoverLetter",
                table: "applications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "applications",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "applications",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LinkedInUrl",
                table: "applications",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NoticePeriodWeeks",
                table: "applications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "applications",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearsOfExperience",
                table: "applications",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailableImmediately",
                table: "applications");

            migrationBuilder.DropColumn(
                name: "CoverLetter",
                table: "applications");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "applications");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "applications");

            migrationBuilder.DropColumn(
                name: "LinkedInUrl",
                table: "applications");

            migrationBuilder.DropColumn(
                name: "NoticePeriodWeeks",
                table: "applications");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "applications");

            migrationBuilder.DropColumn(
                name: "YearsOfExperience",
                table: "applications");
        }
    }
}
