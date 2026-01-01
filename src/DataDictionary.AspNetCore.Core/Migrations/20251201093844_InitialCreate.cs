using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataDictionary.AspNetCore.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataElements",
                columns: table => new
                {
                    DataElementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataElementName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DataElementType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DataType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DataPurpose = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntityPurpose = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DatabaseServer = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DatabaseName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SchemaName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, defaultValue: "dbo"),
                    TableName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ColumnName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    OriginalDataSource = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ForeignKeyTo = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    RowCount = table.Column<long>(type: "bigint", nullable: true),
                    IsNullable = table.Column<bool>(type: "bit", nullable: false),
                    IsPrimaryKey = table.Column<bool>(type: "bit", nullable: false),
                    MaxLength = table.Column<int>(type: "int", nullable: true),
                    Precision = table.Column<int>(type: "int", nullable: true),
                    Scale = table.Column<int>(type: "int", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSyncTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataElements", x => x.DataElementId);
                });

            migrationBuilder.CreateTable(
                name: "SourceConnections",
                columns: table => new
                {
                    ConnectionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConnectionName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DatabaseServer = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DatabaseName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastSyncTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceConnections", x => x.ConnectionId);
                });

            migrationBuilder.CreateTable(
                name: "SyncHistory",
                columns: table => new
                {
                    SyncHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatabaseServer = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DatabaseName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SyncStartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SyncEndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TablesProcessed = table.Column<int>(type: "int", nullable: true),
                    ColumnsProcessed = table.Column<int>(type: "int", nullable: true),
                    ColumnsAdded = table.Column<int>(type: "int", nullable: true),
                    ColumnsUpdated = table.Column<int>(type: "int", nullable: true),
                    ColumnsRemoved = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncHistory", x => x.SyncHistoryId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataElements_Table",
                table: "DataElements",
                columns: new[] { "DatabaseServer", "DatabaseName", "SchemaName", "TableName" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_DataElements_Location",
                table: "DataElements",
                columns: new[] { "DatabaseServer", "DatabaseName", "SchemaName", "TableName", "ColumnName" },
                unique: true,
                filter: "[ColumnName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataElements");

            migrationBuilder.DropTable(
                name: "SourceConnections");

            migrationBuilder.DropTable(
                name: "SyncHistory");
        }
    }
}
