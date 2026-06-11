using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace CareerHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelForTests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_job_listings_CompanyId",
                table: "job_listings");

            migrationBuilder.RenameIndex(
                name: "IX_applications_JobListingId",
                table: "applications",
                newName: "ix_applications_joblistingid");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "job_listings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "job_listings",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "to_tsvector('english', coalesce(\"Title\", '') || ' ' || coalesce(\"Description\", ''))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "ix_job_listings_companyid_status",
                table: "job_listings",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_job_listings_searchvector",
                table: "job_listings",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "ix_job_listings_status_closingdate",
                table: "job_listings",
                columns: new[] { "Status", "ClosingDate" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_job_listings_closingdate_after_postedat",
                table: "job_listings",
                sql: "\"ClosingDate\" > \"PostedAt\"");

            migrationBuilder.AddCheckConstraint(
                name: "ck_job_listings_salarymax_gt_min",
                table: "job_listings",
                sql: "\"SalaryMax\" IS NULL OR \"SalaryMin\" IS NULL OR \"SalaryMax\" > \"SalaryMin\"");

            migrationBuilder.AddCheckConstraint(
                name: "ck_job_listings_salarymin_positive",
                table: "job_listings",
                sql: "\"SalaryMin\" IS NULL OR \"SalaryMin\" > 0");

            migrationBuilder.CreateIndex(
                name: "ix_applications_joblistingid_applicantid",
                table: "applications",
                columns: new[] { "JobListingId", "ApplicantId" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_applications_submittedAt_not_future",
                table: "applications",
                sql: "\"SubmittedAt\" <= NOW()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_job_listings_companyid_status",
                table: "job_listings");

            migrationBuilder.DropIndex(
                name: "ix_job_listings_searchvector",
                table: "job_listings");

            migrationBuilder.DropIndex(
                name: "ix_job_listings_status_closingdate",
                table: "job_listings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_job_listings_closingdate_after_postedat",
                table: "job_listings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_job_listings_salarymax_gt_min",
                table: "job_listings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_job_listings_salarymin_positive",
                table: "job_listings");

            migrationBuilder.DropIndex(
                name: "ix_applications_joblistingid_applicantid",
                table: "applications");

            migrationBuilder.DropCheckConstraint(
                name: "ck_applications_submittedAt_not_future",
                table: "applications");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "job_listings");

            migrationBuilder.RenameIndex(
                name: "ix_applications_joblistingid",
                table: "applications",
                newName: "IX_applications_JobListingId");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "job_listings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_job_listings_CompanyId",
                table: "job_listings",
                column: "CompanyId");
        }
    }
}
