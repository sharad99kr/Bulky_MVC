using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bulky.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddProductEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmbeddingGeneratedAt",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "SearchEmbedding",
                table: "Products",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "EmbeddingGeneratedAt", "SearchEmbedding" },
                values: new object[] { "A sweeping time-travel adventure that spans ancient civilizations and modern dilemmas. Perfect for readers who love historical fiction with philosophical depth, fast-paced plots, and questions about fate, free will, and the consequences of changing the past.", new DateTime(2026, 5, 12, 16, 41, 41, 181, DateTimeKind.Local).AddTicks(877), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "EmbeddingGeneratedAt", "SearchEmbedding" },
                values: new object[] { "A gripping psychological thriller set in a remote mountain town where a detective unravels a decades-old conspiracy. Dark, atmospheric, and relentlessly tense — ideal for fans of crime fiction, murder mystery, and slow-burn suspense that keeps you guessing until the final page.", new DateTime(2026, 5, 12, 16, 41, 41, 181, DateTimeKind.Local).AddTicks(925), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "EmbeddingGeneratedAt", "SearchEmbedding" },
                values: new object[] { "A heartwarming romance set against the backdrop of a small coastal village in summer. Two strangers with complicated pasts find unexpected connection. A cozy, emotional read perfect for a relaxing weekend — uplifting, character-driven, and quietly unforgettable.", new DateTime(2026, 5, 12, 16, 41, 41, 181, DateTimeKind.Local).AddTicks(928), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Description", "EmbeddingGeneratedAt", "SearchEmbedding" },
                values: new object[] { "A lighthearted coming-of-age story following a teenager navigating friendship, first love, and family secrets during one unforgettable summer at a travelling carnival. Funny, tender, and full of heart — a feel-good read suitable for young adults and anyone who loves nostalgic, optimistic fiction.", new DateTime(2026, 5, 12, 16, 41, 41, 181, DateTimeKind.Local).AddTicks(930), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Description", "EmbeddingGeneratedAt", "SearchEmbedding" },
                values: new object[] { "A powerful literary novel about isolation, resilience, and survival. A lone geologist stranded on a remote island must confront nature and his own past to find a way home. Thoughtful and introspective — recommended for readers who enjoy character studies, survival stories, and beautifully written literary fiction.", new DateTime(2026, 5, 12, 16, 41, 41, 181, DateTimeKind.Local).AddTicks(932), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Description", "EmbeddingGeneratedAt", "SearchEmbedding" },
                values: new object[] { "A enchanting collection of nature-inspired fantasy short stories exploring magical forests, ancient spirits, and the hidden lives of plants and animals. Lyrical, imaginative, and deeply atmospheric — perfect for fans of fairy tales, folklore, and quiet fantasy that feels closer to poetry than plot.", new DateTime(2026, 5, 12, 16, 41, 41, 181, DateTimeKind.Local).AddTicks(935), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmbeddingGeneratedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SearchEmbedding",
                table: "Products");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "Description",
                value: "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.\r\n\r\nNunc malesuada viverra ipsum sit amet tincidunt. ");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "Description",
                value: "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.\r\n\r\nNunc malesuada viverra ipsum sit amet tincidunt. ");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                column: "Description",
                value: "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.\r\n\r\nNunc malesuada viverra ipsum sit amet tincidunt. ");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "Description",
                value: "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.\r\n\r\nNunc malesuada viverra ipsum sit amet tincidunt. ");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                column: "Description",
                value: "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.\r\n\r\nNunc malesuada viverra ipsum sit amet tincidunt. ");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                column: "Description",
                value: "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.\r\n\r\nNunc malesuada viverra ipsum sit amet tincidunt. ");
        }
    }
}
