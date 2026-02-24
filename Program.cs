var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpClient<JPracticeWeb.Services.IJapaneseAutoFillService, JPracticeWeb.Services.JapaneseAutoFillService>();

var app = builder.Build();

var projectRoot = ResolveProjectRoot(app.Environment.ContentRootPath);
var canonicalDataPath = Path.Combine(projectRoot, "App_Data", "testwords.json");
var runtimeDataPath = Path.Combine(app.Environment.ContentRootPath, "App_Data", "testwords.json");

JPracticeWeb.Services.TestWordStore.Initialize(canonicalDataPath, [runtimeDataPath]);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

static string ResolveProjectRoot(string contentRootPath)
{
    var dir = new DirectoryInfo(contentRootPath);
    while (dir is not null)
    {
        if (dir.GetFiles("*.csproj").Length > 0)
        {
            return dir.FullName;
        }

        dir = dir.Parent;
    }

    return contentRootPath;
}
