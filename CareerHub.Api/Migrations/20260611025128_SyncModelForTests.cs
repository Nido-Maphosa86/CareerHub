using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace CareerHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelForTests : Migration
    {
        // This migration was originally scaffolded as an exact duplicate of
        // AddSearchVectorAndConstraints — it tried to drop and recreate the
        // same index, rename the same index, and alter the same column a
        // second time. Running it on a fresh database fails with
        // "index does not exist" because the previous migration already did
        // all of this work.
        //
        // The model snapshot confirms there is nothing left for this
        // migration to actually do, so Up and Down are intentionally empty.
        // We keep the migration file (instead of deleting it) so the
        // migration history stays consistent with any database that already
        // has this migration recorded as applied.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}