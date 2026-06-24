using Bulky.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bulky.McpServer
{
    [McpServerToolType]
    public static class InventoryTools
    {
        private const int LowStockTHreshold = 5;

        [McpServerTool(Name = "get_low_inventory_products")]
        [Description("List all products at or below the low-stock threshold," +
            "each with its current quantity.")]
        public static async Task<string> GetLowInventoryProducts(
            IDbContextFactory<ApplicationDbContext> dbFactory,
            CancellationToken ct) {

            await using var db=await dbFactory.CreateDbContextAsync(ct);

            var low = await db.Products
                .Where(p=>p.StockQuantity <= LowStockTHreshold)
                .Select(p=>new {p.Id, p.Title, p.StockQuantity})
                .ToListAsync(ct);

            return JsonSerializer.Serialize(new {
                threshold = LowStockTHreshold,
                count =low.Count,
                products = low 
            });
        }

        [McpServerTool(Name = "get_product_stock")]
        [Description("Get the current stock quantity for a single product by its " +
                 "numeric ID.")]
        public static async Task<string> GetProductStock(
            IDbContextFactory<ApplicationDbContext> dbContextFactory,
            [Description("The numeric product ID")] int productId,
            CancellationToken ct) {

            await using var db = await dbContextFactory.CreateDbContextAsync(ct);

            var p = await db.Products
                .Where(p => p.Id == productId)
                .Select(x => new { x.Id, x.Title, x.StockQuantity })
                .FirstOrDefaultAsync(ct);

            return p is null ?
                JsonSerializer.Serialize(new { error = $"Product {productId} not found." })
                : JsonSerializer.Serialize(p);
        }

        [McpServerTool(Name ="get_stock_discrepancies")]
        [Description("List SQL-vs-warehouse stock discrepancies. Returns an empty " +
                 "set until the Excel warehouse source is wired in Week 6.")]
        public static Task<string> GetStockDiscrepancies() {
            // Todo: load the Excel warehouse counts (via the Excel MCP client),
            // compare against SQL, and return products whose quantities diverge
            // beyond the discrepancy threshold (>40% => Urgent).

            return Task.FromResult(JsonSerializer.Serialize(new {
                note = "Discrepancy detection is wired in Week 6 (Excel source).",
                count = 0,
                discrepancies = Array.Empty<object>()
            }));
        }
    }
}
