using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using VbeSort.Data;
using VbeSort.Infrastructure;
using VbeSort.Services;

EnvFileLoader.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

var dbSettings = DatabaseConfiguration.Resolve(builder.Configuration);
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (dbSettings.Provider == DatabaseProvider.Sqlite)
    {
        options.UseSqlite(dbSettings.ConnectionString);
    }
    else
    {
        options.UseNpgsql(dbSettings.ConnectionString);
    }
});

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<ExerciseService>();
builder.Services.AddSingleton<ExamPdfService>();
builder.Services.AddScoped<StudyProgressService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    DbInitializer.Initialize(scope.ServiceProvider.GetRequiredService<AppDbContext>());
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

var basePath = builder.Configuration["VbeConfig:BasePath"] ?? builder.Environment.ContentRootPath;

foreach (var dir in new[] { "surusiuotos", "atsakymai" })
{
    var path = Path.Combine(basePath, dir);
    if (Directory.Exists(path))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(path),
            RequestPath = "/" + dir
        });
    }
}

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
app.Run();

public partial class Program { }
