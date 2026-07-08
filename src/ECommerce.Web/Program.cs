using ECommerce.Infrastructure;
using ECommerce.Shared.Constants;
using DotNetEnv;
Env.Load("../../deployment/.env");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddAuthentication(AuthConstants.AuthenticationScheme)
    .AddCookie(AuthConstants.AuthenticationScheme, options =>
    {
        options.Cookie.Name = AuthConstants.AuthenticationScheme;
        options.LoginPath = "/account/login";
        options.AccessDeniedPath = "/account/access-denied";
        options.SlidingExpiration = true;
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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// ========== 新增：只在开发环境启用 Swagger ==========
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// =================================================

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
