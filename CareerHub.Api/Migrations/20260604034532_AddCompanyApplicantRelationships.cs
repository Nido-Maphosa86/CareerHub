using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CareerHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyApplicantRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_job_listings_title_company",
                table: "job_listings");

            migrationBuilder.DropColumn(
                name: "Company",
                table: "job_listings");

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "job_listings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "applicants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_applicants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "applications",
                columns: table => new
                {
                    JobListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_applications", x => new { x.ApplicantId, x.JobListingId });
                    table.ForeignKey(
                        name: "FK_applications_applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "applicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_applications_job_listings_JobListingId",
                        column: x => x.JobListingId,
                        principalTable: "job_listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "applicants",
                columns: new[] { "Id", "Email", "Name", "Username" },
                values: new object[,]
                {
                    { new Guid("a0000000-0000-0000-0000-000000000001"), "alice@example.com", "Alice Smith", "applicant1" },
                    { new Guid("a0000000-0000-0000-0000-000000000002"), "bob@example.com", "Bob Jones", "applicant2" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_listings_CompanyId",
                table: "job_listings",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "ix_job_listings_title_companyid",
                table: "job_listings",
                columns: new[] { "Title", "CompanyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_applicants_Email",
                table: "applicants",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_applicants_Username",
                table: "applicants",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_applications_JobListingId",
                table: "applications",
                column: "JobListingId");

            migrationBuilder.CreateIndex(
                name: "IX_companies_Name",
                table: "companies",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_job_listings_companies_CompanyId",
                table: "job_listings",
                column: "CompanyId",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_job_listings_companies_CompanyId",
                table: "job_listings");

            migrationBuilder.DropTable(
                name: "applications");

            migrationBuilder.DropTable(
                name: "companies");

            migrationBuilder.DropTable(
                name: "applicants");

            migrationBuilder.DropIndex(
                name: "IX_job_listings_CompanyId",
                table: "job_listings");

            migrationBuilder.DropIndex(
                name: "ix_job_listings_title_companyid",
                table: "job_listings");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "job_listings");

            migrationBuilder.AddColumn<string>(
                name: "Company",
                table: "job_listings",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_job_listings_title_company",
                table: "job_listings",
                columns: new[] { "Title", "Company" },
                unique: true);
        }
    }
}
