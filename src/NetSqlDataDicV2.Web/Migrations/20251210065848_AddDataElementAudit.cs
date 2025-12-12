using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetSqlDataDicV2.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDataElementAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataElementAudits",
                columns: table => new
                {
                    DataElementAuditId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataElementId = table.Column<int>(type: "int", nullable: false),
                    SyncHistoryId = table.Column<int>(type: "int", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PropertyName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OldValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangeTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataElementAudits", x => x.DataElementAuditId);
                    table.ForeignKey(
                        name: "FK_DataElementAudits_DataElements_DataElementId",
                        column: x => x.DataElementId,
                        principalTable: "DataElements",
                        principalColumn: "DataElementId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DataElementAudits_SyncHistory_SyncHistoryId",
                        column: x => x.SyncHistoryId,
                        principalTable: "SyncHistory",
                        principalColumn: "SyncHistoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataElementAudits_ChangeTime",
                table: "DataElementAudits",
                column: "ChangeTime");

            migrationBuilder.CreateIndex(
                name: "IX_DataElementAudits_DataElementId",
                table: "DataElementAudits",
                column: "DataElementId");

            migrationBuilder.CreateIndex(
                name: "IX_DataElementAudits_SyncHistoryId",
                table: "DataElementAudits",
                column: "SyncHistoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataElementAudits");
        }
    }
}
