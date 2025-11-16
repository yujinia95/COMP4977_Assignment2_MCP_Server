var builder = WebApplication.CreateBuilder(args);

// Bind Ticketmaster API settings from appsettings
builder.Services.Configure<StudentsMcpServer.Models.TicketmasterOptions>(builder.Configuration.GetSection("TicketmasterApi"));

// Register a typed HttpClient + TicketmasterService using the configured options
builder.Services.AddHttpClient<StudentsMcpServer.Models.TicketmasterService>((sp, client) =>
{
	var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<StudentsMcpServer.Models.TicketmasterOptions>>().Value;
	var baseUrl = opts?.BaseUrl?.TrimEnd('/') ?? "https://app.ticketmaster.com/discovery/v2";
	client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddMcpServer()
	.WithHttpTransport()
	.WithToolsFromAssembly();

var app = builder.Build();

// Add MCP server middleware
app.MapMcp();

app.Run();
