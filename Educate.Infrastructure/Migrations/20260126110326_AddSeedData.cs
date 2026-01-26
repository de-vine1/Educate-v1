using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Educate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Courses",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("d7d2d3a0-1b2c-4d3e-8f5a-2b3c4d5e6f7a"), "ATS", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Association of Accountancy Bodies in West Africa - Accounting Technicians Scheme", true, "ATS" },
                    { new Guid("e8e3d4b1-2c3d-5e4f-9a6b-3c4d5e6f7a8b"), "ICAN", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Institute of Chartered Accountants of Nigeria", true, "ICAN" }
                });

            migrationBuilder.InsertData(
                table: "Levels",
                columns: new[] { "Id", "CourseId", "IsActive", "Name", "Order", "Price" },
                values: new object[,]
                {
                    { new Guid("a1a1a1a1-b2b2-c3c3-d4d4-e5e5e5e5e5e5"), new Guid("d7d2d3a0-1b2c-4d3e-8f5a-2b3c4d5e6f7a"), true, "ATS 1", 1, 15000m },
                    { new Guid("a2a2a2a2-b3b3-c4c4-d5d5-e6e6e6e6e6e6"), new Guid("d7d2d3a0-1b2c-4d3e-8f5a-2b3c4d5e6f7a"), true, "ATS 2", 2, 20000m },
                    { new Guid("a3a3a3a3-b4b4-c5c5-d6d6-e7e7e7e7e7e7"), new Guid("d7d2d3a0-1b2c-4d3e-8f5a-2b3c4d5e6f7a"), true, "ATS 3", 3, 25000m },
                    { new Guid("b2b2b2b2-b3b3-c4c4-d5d5-e6e6e6e6e6e6"), new Guid("e8e3d4b1-2c3d-5e4f-9a6b-3c4d5e6f7a8b"), true, "Skills", 2, 55000m },
                    { new Guid("c3c3c3c3-c4c4-d5d5-e6e6-f7f7f7f7f7f7"), new Guid("e8e3d4b1-2c3d-5e4f-9a6b-3c4d5e6f7a8b"), true, "Professional", 3, 70000m },
                    { new Guid("f1f1f1f1-a2a2-b3b3-c4c4-d5d5d5d5d5d5"), new Guid("e8e3d4b1-2c3d-5e4f-9a6b-3c4d5e6f7a8b"), true, "Foundation", 1, 40000m }
                });

            migrationBuilder.InsertData(
                table: "Subjects",
                columns: new[] { "Id", "Code", "IsActive", "LevelId", "Name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111101"), "BAPS", true, new Guid("a1a1a1a1-b2b2-c3c3-d4d4-e5e5e5e5e5e5"), "Basic Accounting Processes & Systems" },
                    { new Guid("11111111-1111-1111-1111-111111111102"), "ECO", true, new Guid("a1a1a1a1-b2b2-c3c3-d4d4-e5e5e5e5e5e5"), "Economics" },
                    { new Guid("11111111-1111-1111-1111-111111111103"), "LAW", true, new Guid("a1a1a1a1-b2b2-c3c3-d4d4-e5e5e5e5e5e5"), "Business Law" },
                    { new Guid("11111111-1111-1111-1111-111111111104"), "COM", true, new Guid("a1a1a1a1-b2b2-c3c3-d4d4-e5e5e5e5e5e5"), "Communication Skills" },
                    { new Guid("22222222-2222-2222-2222-222222222201"), "AUD", true, new Guid("a2a2a2a2-b3b3-c4c4-d5d5-e6e6e6e6e6e6"), "Principles of Auditing" },
                    { new Guid("22222222-2222-2222-2222-222222222202"), "PSA", true, new Guid("a2a2a2a2-b3b3-c4c4-d5d5-e6e6e6e6e6e6"), "Public Sector Accounting" },
                    { new Guid("22222222-2222-2222-2222-222222222203"), "QTA", true, new Guid("a2a2a2a2-b3b3-c4c4-d5d5-e6e6e6e6e6e6"), "Quantitative Analysis" },
                    { new Guid("22222222-2222-2222-2222-222222222204"), "IT", true, new Guid("a2a2a2a2-b3b3-c4c4-d5d5-e6e6e6e6e6e6"), "Information Technology" },
                    { new Guid("33333333-3333-3333-3333-333333333301"), "TAX", true, new Guid("a3a3a3a3-b4b4-c5c5-d6d6-e7e7e7e7e7e7"), "Principles of Taxation" },
                    { new Guid("33333333-3333-3333-3333-333333333302"), "COST", true, new Guid("a3a3a3a3-b4b4-c5c5-d6d6-e7e7e7e7e7e7"), "Cost Accounting" },
                    { new Guid("33333333-3333-3333-3333-333333333303"), "PTCR", true, new Guid("a3a3a3a3-b4b4-c5c5-d6d6-e7e7e7e7e7e7"), "Preparing Tax Computations & Returns" },
                    { new Guid("33333333-3333-3333-3333-333333333304"), "MGT", true, new Guid("a3a3a3a3-b4b4-c5c5-d6d6-e7e7e7e7e7e7"), "Management" },
                    { new Guid("44444444-4444-4444-4444-444444444401"), "FA", true, new Guid("f1f1f1f1-a2a2-b3b3-c4c4-d5d5d5d5d5d5"), "Financial Accounting" },
                    { new Guid("44444444-4444-4444-4444-444444444402"), "BL", true, new Guid("f1f1f1f1-a2a2-b3b3-c4c4-d5d5d5d5d5d5"), "Business Law" },
                    { new Guid("44444444-4444-4444-4444-444444444403"), "MI", true, new Guid("f1f1f1f1-a2a2-b3b3-c4c4-d5d5d5d5d5d5"), "Management Information" },
                    { new Guid("55555555-5555-5555-5555-555555555501"), "FR", true, new Guid("b2b2b2b2-b3b3-c4c4-d5d5-e6e6e6e6e6e6"), "Financial Reporting" },
                    { new Guid("55555555-5555-5555-5555-555555555502"), "AA", true, new Guid("b2b2b2b2-b3b3-c4c4-d5d5-e6e6e6e6e6e6"), "Audit & Assurance" },
                    { new Guid("55555555-5555-5555-5555-555555555503"), "TX", true, new Guid("b2b2b2b2-b3b3-c4c4-d5d5-e6e6e6e6e6e6"), "Taxation" },
                    { new Guid("66666666-6666-6666-6666-666666666601"), "CR", true, new Guid("c3c3c3c3-c4c4-d5d5-e6e6-f7f7f7f7f7f7"), "Corporate Reporting" },
                    { new Guid("66666666-6666-6666-6666-666666666602"), "AAA", true, new Guid("c3c3c3c3-c4c4-d5d5-e6e6-f7f7f7f7f7f7"), "Advanced Audit & Assurance" },
                    { new Guid("66666666-6666-6666-6666-666666666603"), "SFM", true, new Guid("c3c3c3c3-c4c4-d5d5-e6e6-f7f7f7f7f7f7"), "Strategic Financial Management" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"));

            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"));

            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"));

            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"));

            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222201"));

            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222202"));

            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222203"));

            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222204"));

            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333301"));

            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333302"));

            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333303"));

            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333304"));

            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444401"));

            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444402"));

            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444403"));

            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555501"));

            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555502"));

            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555503"));

            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666601"));

            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666602"));

            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666603"));

            migrationBuilder.DeleteData(
                table: "Levels",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-b2b2-c3c3-d4d4-e5e5e5e5e5e5"));

            migrationBuilder.DeleteData(
                table: "Levels",
                keyColumn: "Id",
                keyValue: new Guid("a2a2a2a2-b3b3-c4c4-d5d5-e6e6e6e6e6e6"));

            migrationBuilder.DeleteData(
                table: "Levels",
                keyColumn: "Id",
                keyValue: new Guid("a3a3a3a3-b4b4-c5c5-d6d6-e7e7e7e7e7e7"));

            migrationBuilder.DeleteData(
                table: "Levels",
                keyColumn: "Id",
                keyValue: new Guid("b2b2b2b2-b3b3-c4c4-d5d5-e6e6e6e6e6e6"));

            migrationBuilder.DeleteData(
                table: "Levels",
                keyColumn: "Id",
                keyValue: new Guid("c3c3c3c3-c4c4-d5d5-e6e6-f7f7f7f7f7f7"));

            migrationBuilder.DeleteData(
                table: "Levels",
                keyColumn: "Id",
                keyValue: new Guid("f1f1f1f1-a2a2-b3b3-c4c4-d5d5d5d5d5d5"));

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: new Guid("d7d2d3a0-1b2c-4d3e-8f5a-2b3c4d5e6f7a"));

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: new Guid("e8e3d4b1-2c3d-5e4f-9a6b-3c4d5e6f7a8b"));
        }
    }
}
