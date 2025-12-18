using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetSqlDataDicV2.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDataElementNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataElementNotes",
                columns: table => new
                {
                    DataElementNoteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataElementId = table.Column<int>(type: "int", nullable: false),
                    NoteText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataElementNotes", x => x.DataElementNoteId);
                    table.ForeignKey(
                        name: "FK_DataElementNotes_DataElements_DataElementId",
                        column: x => x.DataElementId,
                        principalTable: "DataElements",
                        principalColumn: "DataElementId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataElementNotes_CreatedAt",
                table: "DataElementNotes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DataElementNotes_DataElementId",
                table: "DataElementNotes",
                column: "DataElementId");

            // Migrate existing notes from the legacy Notes column
            migrationBuilder.Sql(@"
                INSERT INTO DataElementNotes (DataElementId, NoteText, CreatedAt)
                SELECT DataElementId, Notes, COALESCE(LastUpdateTime, GETUTCDATE())
                FROM DataElements
                WHERE Notes IS NOT NULL AND Notes <> ''
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataElementNotes");
        }
    }
}
