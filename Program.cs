using ContractDiffTool.Domain.Interfaces;
using ContractDiffTool.Services;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();

builder.Services.AddScoped<IPdfTextExtractor, PdfTextExtractorService>();
builder.Services.AddScoped<IDocumentParser, ContractParserService>();
builder.Services.AddScoped<IDiffEngine, DiffEngineService>();
builder.Services.AddScoped<IChangeClassifier, ChangeClassifierService>();
builder.Services.AddScoped<IReportGenerator, HtmlReportGeneratorService>();

var maxFileSize = builder.Configuration.GetValue<long>("Upload:MaxFileSizeBytes");

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxFileSize;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxFileSize;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapControllers();

app.Run();
