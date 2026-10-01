using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResumeAnalyzer.Api.Models.Entities;

namespace ResumeAnalyzer.Api.Data.Configurations;

public class AnalysisConfiguration : IEntityTypeConfiguration<Analysis>
{
    public void Configure(EntityTypeBuilder<Analysis> builder)
    {
        builder.ToTable("analyses", t =>
            t.HasCheckConstraint("ck_analyses_match_score", "match_score BETWEEN 0 AND 100"));

        builder.HasKey(a => a.Id).HasName("pk_analyses");

        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.UserId).HasColumnName("user_id");

        builder.Property(a => a.JobTitle).HasColumnName("job_title").HasMaxLength(200).IsRequired();
        builder.Property(a => a.CompanyName).HasColumnName("company_name").HasMaxLength(200);

        builder.Property(a => a.ResumeFileName).HasColumnName("resume_file_name").HasMaxLength(255).IsRequired();
        builder.Property(a => a.ResumeText).HasColumnName("resume_text").HasColumnType("text").IsRequired();
        builder.Property(a => a.JobDescription).HasColumnName("job_description").HasColumnType("text").IsRequired();

        builder.Property(a => a.MatchScore).HasColumnName("match_score");

        builder.Property(a => a.MatchedSkills).HasColumnName("matched_skills").HasColumnType("text[]").IsRequired();
        builder.Property(a => a.MissingRequiredSkills).HasColumnName("missing_required_skills").HasColumnType("text[]").IsRequired();
        builder.Property(a => a.MissingPreferredSkills).HasColumnName("missing_preferred_skills").HasColumnType("text[]").IsRequired();
        builder.Property(a => a.Suggestions).HasColumnName("suggestions").HasColumnType("text[]").IsRequired();

        builder.Property(a => a.Summary).HasColumnName("summary").HasColumnType("text").IsRequired();

        builder.Property(a => a.AiModel).HasColumnName("ai_model").HasMaxLength(100).IsRequired();
        builder.Property(a => a.InputTokens).HasColumnName("input_tokens");
        builder.Property(a => a.OutputTokens).HasColumnName("output_tokens");

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder.HasIndex(a => a.CreatedAt)
            .HasDatabaseName("ix_analyses_created_at")
            .IsDescending();
    }
}
