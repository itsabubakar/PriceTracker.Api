using Hangfire;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Hangfire.PostgreSql;
using PriceTracker.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")))
);
builder.Services.AddHangfireServer();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientApp", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Job dependencies
builder.Services.AddScoped<PriceUpdatedService>();
builder.Services.AddHttpClient<ScraperService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var error = context.Features.Get<IExceptionHandlerFeature>();
        if (error != null)
        {
            await context.Response.WriteAsJsonAsync(new
            {
                message = error.Error.Message
            });
        }
    });
});
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.RegisterHangfireJobs();

app.UseHangfireDashboard("/hangfire");

app.UseHttpsRedirection();
app.UseCors("ClientApp");
app.Use(async (context, next) =>
{
    if (!context.Request.Cookies.TryGetValue(VisitorIdentity.AnonymousCookieName, out var anonId) ||
        !Guid.TryParse(anonId, out _))
    {
        context.Response.Cookies.Append(
            VisitorIdentity.AnonymousCookieName,
            Guid.NewGuid().ToString(),
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
    }

    await next();
});

app.MapControllers();

app.Run();
