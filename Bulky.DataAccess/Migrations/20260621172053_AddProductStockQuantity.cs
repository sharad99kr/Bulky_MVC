using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bulky.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddProductStockQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EmbeddingGeneratedAt", "StockQuantity" },
                values: new object[] { new DateTime(2026, 6, 21, 13, 20, 51, 722, DateTimeKind.Local).AddTicks(1987), 0 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "EmbeddingGeneratedAt", "StockQuantity" },
                values: new object[] { new DateTime(2026, 6, 21, 13, 20, 51, 722, DateTimeKind.Local).AddTicks(2039), 0 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "EmbeddingGeneratedAt", "StockQuantity" },
                values: new object[] { new DateTime(2026, 6, 21, 13, 20, 51, 722, DateTimeKind.Local).AddTicks(2043), 0 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "EmbeddingGeneratedAt", "StockQuantity" },
                values: new object[] { new DateTime(2026, 6, 21, 13, 20, 51, 722, DateTimeKind.Local).AddTicks(2046), 0 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "EmbeddingGeneratedAt", "StockQuantity" },
                values: new object[] { new DateTime(2026, 6, 21, 13, 20, 51, 722, DateTimeKind.Local).AddTicks(2048), 0 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "EmbeddingGeneratedAt", "StockQuantity" },
                values: new object[] { new DateTime(2026, 6, 21, 13, 20, 51, 722, DateTimeKind.Local).AddTicks(2051), 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "Products");

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
        }
    }
}
