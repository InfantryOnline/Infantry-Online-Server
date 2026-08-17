using AccountWeb.Services;
using Database.Sqlite;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console());

var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .SetApplicationName("AccountWeb")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

var connectionString = builder.Configuration.GetConnectionString("InfantryDatabase");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:InfantryDatabase is required.");
}

connectionString = ResolveSqliteDataSource(connectionString, builder.Environment.ContentRootPath);

builder.Services.AddSingleton<SqlitePragmaConnectionInterceptor>();
builder.Services.AddDbContext<SqliteDbContext>((serviceProvider, options) =>
{
    options
        .UseSqlite(connectionString)
        .AddInterceptors(serviceProvider.GetRequiredService<SqlitePragmaConnectionInterceptor>());
});
builder.Services.AddScoped<AccountSignInService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/Login";
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/Logout");
    options.Conventions.AllowAnonymousToPage("/ResetPassword");
    options.Conventions.AllowAnonymousToPage("/Error");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Error", "?statusCode={0}");

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

static string ResolveSqliteDataSource(string connectionString, string contentRootPath)
{
    var sqliteBuilder = new SqliteConnectionStringBuilder(connectionString);

    if (!string.IsNullOrWhiteSpace(sqliteBuilder.DataSource)
        && sqliteBuilder.DataSource != ":memory:"
        && !Path.IsPathRooted(sqliteBuilder.DataSource))
    {
        sqliteBuilder.DataSource = Path.GetFullPath(Path.Combine(contentRootPath, sqliteBuilder.DataSource));
    }

    return sqliteBuilder.ConnectionString;
}
