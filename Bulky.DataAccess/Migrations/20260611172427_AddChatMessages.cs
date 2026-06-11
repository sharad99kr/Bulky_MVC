using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bulky.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TokensUsed = table.Column<int>(type: "int", nullable: false),
                    FallbackUsed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "EmbeddingGeneratedAt",
                value: new DateTime(2026, 6, 11, 13, 24, 26, 164, DateTimeKind.Local).AddTicks(9830));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "EmbeddingGeneratedAt",
                value: new DateTime(2026, 6, 11, 13, 24, 26, 164, DateTimeKind.Local).AddTicks(9872));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                column: "EmbeddingGeneratedAt",
                value: new DateTime(2026, 6, 11, 13, 24, 26, 164, DateTimeKind.Local).AddTicks(9876));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "EmbeddingGeneratedAt",
                value: new DateTime(2026, 6, 11, 13, 24, 26, 164, DateTimeKind.Local).AddTicks(9879));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                column: "EmbeddingGeneratedAt",
                value: new DateTime(2026, 6, 11, 13, 24, 26, 164, DateTimeKind.Local).AddTicks(9881));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                column: "EmbeddingGeneratedAt",
                value: new DateTime(2026, 6, 11, 13, 24, 26, 164, DateTimeKind.Local).AddTicks(9884));

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ConversationId_CreatedAtUtc",
                table: "ChatMessages",
                columns: new[] { "ConversationId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "EmbeddingGeneratedAt",
                value: new DateTime(2026, 5, 12, 16, 41, 41, 181, DateTimeKind.Local).AddTicks(877));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "EmbeddingGeneratedAt",
                value: new DateTime(2026, 5, 12, 16, 41, 41, 181, DateTimeKind.Local).AddTicks(925));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                column: "EmbeddingGeneratedAt",
                value: new DateTime(2026, 5, 12, 16, 41, 41, 181, DateTimeKind.Local).AddTicks(928));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "EmbeddingGeneratedAt",
                value: new DateTime(2026, 5, 12, 16, 41, 41, 181, DateTimeKind.Local).AddTicks(930));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                column: "EmbeddingGeneratedAt",
                value: new DateTime(2026, 5, 12, 16, 41, 41, 181, DateTimeKind.Local).AddTicks(932));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                column: "EmbeddingGeneratedAt",
                value: new DateTime(2026, 5, 12, 16, 41, 41, 181, DateTimeKind.Local).AddTicks(935));
        }
    }
}
