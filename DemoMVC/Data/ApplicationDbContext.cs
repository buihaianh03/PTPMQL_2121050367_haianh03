using Microsoft.EntityFrameworkCore;
using DemoMVC.Models;

namespace DemoMVC.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options
        ) : base(options)
        {

        }

        public DbSet<Person> Persons => Set<Person>();
        public DbSet<Faculty> Faculties => Set<Faculty>();
        public DbSet<TrainingSystem> TrainingSystems => Set<TrainingSystem>();
        public DbSet<Major> Majors => Set<Major>();
        public DbSet<StudentRecord> Students => Set<StudentRecord>();
        public DbSet<StudentNumberSequence> StudentNumberSequences => Set<StudentNumberSequence>();
        public DbSet<StudentNumberAssignment> StudentNumberAssignments => Set<StudentNumberAssignment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Faculty>(entity =>
            {
                entity.ToTable("Khoa");
                entity.HasIndex(x => x.Name).IsUnique();
                entity.Property(x => x.Name).IsRequired().HasMaxLength(120);
            });

            modelBuilder.Entity<TrainingSystem>(entity =>
            {
                entity.HasIndex(x => x.Name).IsUnique();
                entity.HasIndex(x => x.StudentCode)
                    .IsUnique()
                    .HasFilter("\"StudentCode\" IS NOT NULL");
                entity.ToTable(table => table.HasCheckConstraint(
                    "CK_TrainingSystems_StudentCode",
                    "\"StudentCode\" IS NULL OR (length(\"StudentCode\") = 2 AND \"StudentCode\" GLOB '[0-9][0-9]')"));
            });

            modelBuilder.Entity<Major>(entity =>
            {
                entity.HasIndex(x => x.Name).IsUnique();
                entity.HasIndex(x => x.StudentCode)
                    .IsUnique()
                    .HasFilter("\"StudentCode\" IS NOT NULL");
                entity.HasIndex(x => x.AdmissionCode)
                    .IsUnique()
                    .HasFilter("\"AdmissionCode\" IS NOT NULL");
                entity.ToTable(table =>
                {
                    table.HasCheckConstraint(
                        "CK_Majors_StudentCode",
                        "\"StudentCode\" IS NULL OR (length(\"StudentCode\") = 2 AND \"StudentCode\" GLOB '[0-9][0-9]')");
                });
            });

            modelBuilder.Entity<StudentRecord>(entity =>
            {
                entity.ToTable("SinhVien", table =>
                {
                    table.HasCheckConstraint(
                        "CK_SinhVien_MaSV",
                        "length(\"MaSV\") = 10 AND \"MaSV\" NOT GLOB '*[^0-9]*'");
                    table.HasCheckConstraint(
                        "CK_SinhVien_AdmissionYear",
                        "\"AdmissionYear\" >= 2000 AND \"AdmissionYear\" <= 2099");
                });
                entity.HasIndex(x => x.MaSV).IsUnique();
                entity.HasOne(x => x.Faculty)
                    .WithMany(x => x.Students)
                    .HasForeignKey(x => x.MaKhoa)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.TrainingSystem)
                    .WithMany(x => x.Students)
                    .HasForeignKey(x => x.TrainingSystemId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Major)
                    .WithMany(x => x.Students)
                    .HasForeignKey(x => x.MajorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StudentNumberSequence>(entity =>
            {
                entity.HasKey(x => new { x.AdmissionYear, x.TrainingSystemCode, x.MajorCode });
                entity.Property(x => x.TrainingSystemCode).HasMaxLength(2);
                entity.Property(x => x.MajorCode).HasMaxLength(2);
                entity.ToTable(table =>
                {
                    table.HasCheckConstraint(
                        "CK_StudentNumberSequences_LastSequence",
                        "\"LastSequence\" >= 0 AND \"LastSequence\" <= 9999");
                    table.HasCheckConstraint(
                        "CK_StudentNumberSequences_Codes",
                        "length(\"TrainingSystemCode\") = 2 AND \"TrainingSystemCode\" GLOB '[0-9][0-9]' AND length(\"MajorCode\") = 2 AND \"MajorCode\" GLOB '[0-9][0-9]'");
                    table.HasCheckConstraint(
                        "CK_StudentNumberSequences_AdmissionYear",
                        "\"AdmissionYear\" >= 2000 AND \"AdmissionYear\" <= 2099");
                });
            });

            modelBuilder.Entity<StudentNumberAssignment>(entity =>
            {
                entity.Property(x => x.TrainingSystemCode).HasMaxLength(2);
                entity.Property(x => x.MajorCode).HasMaxLength(2);
                entity.HasIndex(x => x.MaSV).IsUnique();
                entity.HasIndex(x => new
                {
                    x.AdmissionYear,
                    x.TrainingSystemCode,
                    x.MajorCode,
                    x.SequenceNumber
                }).IsUnique();
                entity.ToTable(table =>
                {
                    table.HasCheckConstraint(
                        "CK_StudentNumberAssignments_SequenceNumber",
                        "\"SequenceNumber\" >= 1 AND \"SequenceNumber\" <= 9999");
                    table.HasCheckConstraint(
                        "CK_StudentNumberAssignments_MaSV",
                        "length(\"MaSV\") = 10 AND \"MaSV\" NOT GLOB '*[^0-9]*'");
                    table.HasCheckConstraint(
                        "CK_StudentNumberAssignments_Codes",
                        "length(\"TrainingSystemCode\") = 2 AND \"TrainingSystemCode\" GLOB '[0-9][0-9]' AND length(\"MajorCode\") = 2 AND \"MajorCode\" GLOB '[0-9][0-9]'");
                    table.HasCheckConstraint(
                        "CK_StudentNumberAssignments_AdmissionYear",
                        "\"AdmissionYear\" >= 2000 AND \"AdmissionYear\" <= 2099");
                });
                entity.HasOne(x => x.Student)
                    .WithMany()
                    .HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}