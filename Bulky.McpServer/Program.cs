using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Bulky.DataAccess.Data;
using Microsoft.Extensions.Configuration;

var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

// CRITICAL for stdio: stdout is the JSON-RPC channel. All logs MUST go to
// stderr or they will corrupt the protocol and break the server.
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// A DbContext FACTORY (not a scoped DbContext) — the server is long-lived
// and each tool call creates and disposes its own short-lived context.
builder.Services.AddDbContextFactory<ApplicationDbContext>(o => o.UseSqlServer(
    builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly();

await builder.Build().RunAsync();
