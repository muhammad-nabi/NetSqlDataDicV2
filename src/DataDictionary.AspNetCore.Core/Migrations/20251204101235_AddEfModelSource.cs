using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataDictionary.AspNetCore.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddEfModelSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EfModelSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProviderType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssemblyPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DbContextTypeName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConnectionString = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TargetServer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TargetDatabase = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastComparedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EfModelSources", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EfModelSources_IsActive",
                table: "EfModelSources",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_EfModelSources_TargetServer_TargetDatabase",
                table: "EfModelSources",
                columns: new[] { "TargetServer", "TargetDatabase" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EfModelSources");
        }
    }
}
