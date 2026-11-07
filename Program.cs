using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using VbeSort.Data;
using VbeSort.Services;

var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envFile))
{
    foreach (var line in File.ReadAllLines(envFile))
    {
        var trimmed = line.Trim();
        if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#"))
        {
            var parts = trimmed.Split('=', 2);
            if (parts.Length == 2)
            {
                var val = parts[1].Trim();
                if ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'")))
                    val = val[1..^1].Trim();
                Environment.SetEnvironmentVariable(parts[0].Trim(), val);
            }
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connStr = Environment.GetEnvironmentVariable("DATABASE_URL")
              ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
              ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrWhiteSpace(connStr))
{
    connStr = connStr.Trim();
    if ((connStr.StartsWith("\"") && connStr.EndsWith("\"")) || (connStr.StartsWith("'") && connStr.EndsWith("'")))
        connStr = connStr[1..^1].Trim();

    if (connStr.StartsWith("postgres://") || connStr.StartsWith("postgresql://"))
    {
        var uri = new Uri(connStr);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = string.IsNullOrEmpty(uri.AbsolutePath.TrimStart('/'))
            ? "postgres"
            : uri.AbsolutePath.TrimStart('/');
        connStr = $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
}

if (string.IsNullOrWhiteSpace(connStr))
{
    throw new InvalidOperationException("Nenustatyta PostgreSQL prisijungimo eilutė.");
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connStr));

builder.Services.AddSingleton<ExerciseService>();
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
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(path),
        RequestPath = "/" + dir
    });
}

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
app.Run();
