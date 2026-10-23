using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Basilisque.Demo.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Demo_Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Demo_Documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Demo_DocumentsDeleteAudit",
                columns: table => new
                {
                    AuditId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DeletedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Demo_DocumentsDeleteAudit", x => x.AuditId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Demo_DocumentsDeleteAudit_EntityKey",
                table: "Demo_DocumentsDeleteAudit",
                column: "EntityKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Demo_Documents");

            migrationBuilder.DropTable(
                name: "Demo_DocumentsDeleteAudit");
        }
    }
}
