using ECommerce.Infrastructure;
using ECommerce.Shared.Constants;
using ECommerce.Web.Filters;
using ECommerce.Web.Security;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
    options.Filters.AddService<RbacPermissionFilter>();
    options.Filters.AddService<AdminOperationAuditFilter>();
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<RbacPermissionFilter>();
builder.Services.AddScoped<RefreshUserPrincipalCookieEvents>();
builder.Services.AddScoped<AdminOperationAuditFilter>();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services
    .AddAuthentication(AuthConstants.AuthenticationScheme)
    .AddCookie(AuthConstants.AuthenticationScheme, options =>
    {
        options.Cookie.Name = AuthConstants.AuthenticationScheme;
        options.LoginPath = "/account/login";
        options.AccessDeniedPath = "/account/access-denied";
        options.SlidingExpiration = true;
        options.EventsType = typeof(RefreshUserPrincipalCookieEvents);
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthConstants.Policies.CustomerOnly, policy =>
        policy.RequireRole(AuthConstants.Roles.User, AuthConstants.Roles.Service, AuthConstants.Roles.Admin));

    options.AddPolicy(AuthConstants.Policies.ServiceOrAdmin, policy =>
        policy.RequireRole(AuthConstants.Roles.Service, AuthConstants.Roles.Admin));

    options.AddPolicy(AuthConstants.Policies.AdminOnly, policy =>
        policy.RequireRole(AuthConstants.Roles.Admin));
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
