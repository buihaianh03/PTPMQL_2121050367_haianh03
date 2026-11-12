using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoMVC.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Khoa",
                columns: table => new
                {
                    MaKhoa = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Khoa", x => x.MaKhoa);
                });

            migrationBuilder.CreateTable(
                name: "Majors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    AdmissionCode = table.Column<string>(type: "TEXT", maxLength: 12, nullable: true),
                    StudentCode = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Majors", x => x.Id);
                    table.CheckConstraint("CK_Majors_StudentCode", "\"StudentCode\" IS NULL OR (length(\"StudentCode\") = 2 AND \"StudentCode\" GLOB '[0-9][0-9]')");
                });

            migrationBuilder.CreateTable(
                name: "StudentNumberSequences",
                columns: table => new
                {
                    AdmissionYear = table.Column<int>(type: "INTEGER", nullable: false),
                    TrainingSystemCode = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    MajorCode = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    LastSequence = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentNumberSequences", x => new { x.AdmissionYear, x.TrainingSystemCode, x.MajorCode });
                    table.CheckConstraint("CK_StudentNumberSequences_AdmissionYear", "\"AdmissionYear\" >= 2000 AND \"AdmissionYear\" <= 2099");
                    table.CheckConstraint("CK_StudentNumberSequences_Codes", "length(\"TrainingSystemCode\") = 2 AND \"TrainingSystemCode\" GLOB '[0-9][0-9]' AND length(\"MajorCode\") = 2 AND \"MajorCode\" GLOB '[0-9][0-9]'");
                    table.CheckConstraint("CK_StudentNumberSequences_LastSequence", "\"LastSequence\" >= 0 AND \"LastSequence\" <= 9999");
                });

            migrationBuilder.CreateTable(
                name: "TrainingSystems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    StudentCode = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingSystems", x => x.Id);
                    table.CheckConstraint("CK_TrainingSystems_StudentCode", "\"StudentCode\" IS NULL OR (length(\"StudentCode\") = 2 AND \"StudentCode\" GLOB '[0-9][0-9]')");
                });

            migrationBuilder.CreateTable(
                name: "SinhVien",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MaSV = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AdmissionYear = table.Column<int>(type: "INTEGER", nullable: false),
                    TrainingSystemId = table.Column<int>(type: "INTEGER", nullable: false),
                    MajorId = table.Column<int>(type: "INTEGER", nullable: false),
                    MaKhoa = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SinhVien", x => x.Id);
                    table.CheckConstraint("CK_SinhVien_AdmissionYear", "\"AdmissionYear\" >= 2000 AND \"AdmissionYear\" <= 2099");
                    table.CheckConstraint("CK_SinhVien_MaSV", "length(\"MaSV\") = 10 AND \"MaSV\" NOT GLOB '*[^0-9]*'");
                    table.ForeignKey(
                        name: "FK_SinhVien_Khoa_MaKhoa",
                        column: x => x.MaKhoa,
                        principalTable: "Khoa",
                        principalColumn: "MaKhoa",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SinhVien_Majors_MajorId",
                        column: x => x.MajorId,
                        principalTable: "Majors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SinhVien_TrainingSystems_TrainingSystemId",
                        column: x => x.TrainingSystemId,
                        principalTable: "TrainingSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentNumberAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MaSV = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    AdmissionYear = table.Column<int>(type: "INTEGER", nullable: false),
                    TrainingSystemCode = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    MajorCode = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    SequenceNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    StudentId = table.Column<int>(type: "INTEGER", nullable: true),
                    AssignedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentNumberAssignments", x => x.Id);
                    table.CheckConstraint("CK_StudentNumberAssignments_AdmissionYear", "\"AdmissionYear\" >= 2000 AND \"AdmissionYear\" <= 2099");
                    table.CheckConstraint("CK_StudentNumberAssignments_Codes", "length(\"TrainingSystemCode\") = 2 AND \"TrainingSystemCode\" GLOB '[0-9][0-9]' AND length(\"MajorCode\") = 2 AND \"MajorCode\" GLOB '[0-9][0-9]'");
                    table.CheckConstraint("CK_StudentNumberAssignments_MaSV", "length(\"MaSV\") = 10 AND \"MaSV\" NOT GLOB '*[^0-9]*'");
                    table.CheckConstraint("CK_StudentNumberAssignments_SequenceNumber", "\"SequenceNumber\" >= 1 AND \"SequenceNumber\" <= 9999");
                    table.ForeignKey(
                        name: "FK_StudentNumberAssignments_SinhVien_StudentId",
                        column: x => x.StudentId,
                        principalTable: "SinhVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Khoa_Name",
                table: "Khoa",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Majors_AdmissionCode",
                table: "Majors",
                column: "AdmissionCode",
                unique: true,
                filter: "\"AdmissionCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Majors_Name",
                table: "Majors",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Majors_StudentCode",
                table: "Majors",
                column: "StudentCode",
                unique: true,
                filter: "\"StudentCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SinhVien_MajorId",
                table: "SinhVien",
                column: "MajorId");

            migrationBuilder.CreateIndex(
                name: "IX_SinhVien_MaKhoa",
                table: "SinhVien",
                column: "MaKhoa");

            migrationBuilder.CreateIndex(
                name: "IX_SinhVien_MaSV",
                table: "SinhVien",
                column: "MaSV",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SinhVien_TrainingSystemId",
                table: "SinhVien",
                column: "TrainingSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentNumberAssignments_AdmissionYear_TrainingSystemCode_MajorCode_SequenceNumber",
                table: "StudentNumberAssignments",
                columns: new[] { "AdmissionYear", "TrainingSystemCode", "MajorCode", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentNumberAssignments_MaSV",
                table: "StudentNumberAssignments",
                column: "MaSV",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentNumberAssignments_StudentId",
                table: "StudentNumberAssignments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSystems_Name",
                table: "TrainingSystems",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSystems_StudentCode",
                table: "TrainingSystems",
                column: "StudentCode",
                unique: true,
                filter: "\"StudentCode\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentNumberAssignments");

            migrationBuilder.DropTable(
                name: "StudentNumberSequences");

            migrationBuilder.DropTable(
                name: "SinhVien");

            migrationBuilder.DropTable(
                name: "Khoa");

            migrationBuilder.DropTable(
                name: "Majors");

            migrationBuilder.DropTable(
                name: "TrainingSystems");
        }
    }
}
