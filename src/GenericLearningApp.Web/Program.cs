using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GenericLearningApp.Web.Components;
using GenericLearningApp.Web.Components.Account;
using GenericLearningApp.Web.Data;
using GenericLearningApp.Web.Admin;
using GenericLearningApp.Application.Site;
using GenericLearningApp.Infrastructure;
using GenericLearningApp.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

var authentication = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
});
authentication.AddIdentityCookies();

// Google and Microsoft sign-in switch on when their credentials are configured
// (user-secrets in development, environment variables in the container). Without
// them the app runs normally and the sign-in page simply has no such button.
var googleAuth = builder.Configuration.GetSection("Authentication:Google");
if (!string.IsNullOrEmpty(googleAuth["ClientId"]) && !string.IsNullOrEmpty(googleAuth["ClientSecret"]))
{
    authentication.AddGoogle(options =>
    {
        options.ClientId = googleAuth["ClientId"]!;
        options.ClientSecret = googleAuth["ClientSecret"]!;
    });
}

var microsoftAuth = builder.Configuration.GetSection("Authentication:Microsoft");
if (!string.IsNullOrEmpty(microsoftAuth["ClientId"]) && !string.IsNullOrEmpty(microsoftAuth["ClientSecret"]))
{
    authentication.AddMicrosoftAccount(options =>
    {
        options.ClientId = microsoftAuth["ClientId"]!;
        options.ClientSecret = microsoftAuth["ClientSecret"]!;
    });
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Login cookies and emailed links are signed with these keys; keep them in the database so they
// survive container rebuilds and travel with a backup. The application name pins them to this app.
builder.Services.AddDataProtection()
    .SetApplicationName("GenericLearningApp")
    .PersistKeysToDbContext<ApplicationDbContext>();

// The learning aggregates live in their own context, on the same database.
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Admin area: published site settings held in memory, account/role management, and the access rules.
builder.Services.AddSingleton<SiteConfigStore>();
builder.Services.AddSingleton<AdminAccountService>();
builder.Services.AddSingleton<SiteAccess>();

// The container's healthcheck calls this; it must not require authentication.
builder.Services.AddHealthChecks();

var app = builder.Build();

// Bring the schema up to date, create the built-in roles and the super admin, then publish the site
// configuration to memory. A single-instance app can safely do this on start; set
// Database:MigrateOnStartup=false to run migrations yourself instead.
await using (var scope = app.Services.CreateAsyncScope())
{
    var services = scope.ServiceProvider;

    if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
    {
        await services.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
        await using var learning = await services.GetRequiredService<IDbContextFactory<LearningDbContext>>().CreateDbContextAsync();
        await learning.Database.MigrateAsync();
    }

    await services.GetRequiredService<ISiteConfigService>().EnsureDefaultsAsync();
    await app.Services.GetRequiredService<AdminAccountService>().SeedAsync(
        app.Configuration["Admin:SuperAdminEmail"],
        app.Configuration["Admin:SuperAdminPassword"]);
}
await app.Services.GetRequiredService<SiteConfigStore>().RefreshAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// The admin's site rules ("require sign-in", who may open each menu) for a fresh page load. Navigation
// inside an open circuit skips the pipeline, so MainLayout applies the same check.
app.Use(async (context, next) =>
{
    if (HttpMethods.IsGet(context.Request.Method))
    {
        var decision = await context.RequestServices.GetRequiredService<SiteAccess>()
            .CheckAsync(context.User, context.Request.Path.Value ?? "/");

        if (decision == AccessDecision.SignIn)
        {
            var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
            context.Response.Redirect($"/Account/Login?ReturnUrl={returnUrl}");
            return;
        }

        if (decision == AccessDecision.Denied)
        {
            context.Response.Redirect("/Account/AccessDenied");
            return;
        }
    }

    await next();
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();
app.MapHealthChecks("/health");

app.Run();
