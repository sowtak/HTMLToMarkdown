using AspNetCoreRateLimit;
using HTMLToMarkdown.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowAllOrigins",
      policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Configure rate limiting
builder.Services.AddOptions();
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.Configure<IpRateLimitOptions>(options =>
{
  options.EnableEndpointRateLimiting = true;
  options.StackBlockedRequests = false;
  options.HttpStatusCode = 429;
  options.RealIpHeader = "X-Forwarded-For";
});
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

builder.Services.AddSingleton<IDomProcessorService, DomProcessorService>();
builder.Services.AddSingleton<IFormatterService, FormattersService>();
builder.Services.AddSingleton<ICommonFiltersService, CommonFiltersService>();
builder.Services.AddSingleton<IAppleDevDocParserService, AppleDevDocParserService>();
builder.Services.AddSingleton<IHTMLTableToMarkdownService, HTMLTableToMarkdownService>();

builder.Services.AddHttpClient();

builder.Services.AddLogging();


builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
  var ipPolicyStore = scope.ServiceProvider.GetRequiredService<IIpPolicyStore>();
  ipPolicyStore.SeedAsync().Wait();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseDeveloperExceptionPage();
}

if (app.Environment.IsProduction())
{
  app.UseExceptionHandler("/Error");
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("AllowAllOrigins");

app.UseIpRateLimiting();

app.UseAuthorization();
app.MapControllers();

app.Run();