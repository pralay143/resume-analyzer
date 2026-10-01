using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResumeAnalyzer.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "analyses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: true),
                    job_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    resume_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    resume_text = table.Column<string>(type: "text", nullable: false),
                    job_description = table.Column<string>(type: "text", nullable: false),
                    match_score = table.Column<int>(type: "integer", nullable: false),
                    matched_skills = table.Column<List<string>>(type: "text[]", nullable: false),
                    missing_required_skills = table.Column<List<string>>(type: "text[]", nullable: false),
                    missing_preferred_skills = table.Column<List<string>>(type: "text[]", nullable: false),
                    suggestions = table.Column<List<string>>(type: "text[]", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: false),
                    ai_model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    input_tokens = table.Column<int>(type: "integer", nullable: false),
                    output_tokens = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_analyses", x => x.id);
                    table.CheckConstraint("ck_analyses_match_score", "match_score BETWEEN 0 AND 100");
                });

            migrationBuilder.CreateIndex(
                name: "ix_analyses_created_at",
                table: "analyses",
                column: "created_at",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analyses");
        }
    }
}
