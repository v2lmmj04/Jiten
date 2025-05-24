using System.Globalization;
using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.PostgreSql;
using Jiten.Api.Jobs;
using Jiten.Core;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(Path.Combine(Environment.CurrentDirectory, "..", "Shared", "sharedsettings.json"), optional: true,
                                  reloadOnChange: true);
builder.Configuration.AddJsonFile("sharedsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization
                                                         .JsonNumberHandling.AllowNamedFloatingPointLiterals;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();

builder.Services.AddDbContext<JitenDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("JitenDatabase"),
                                                                           o =>
                                                                           {
                                                                               o.UseQuerySplittingBehavior(QuerySplittingBehavior
                                                                                   .SplitQuery);
                                                                           }));

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("fixed", context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
                                                        context.Connection.RemoteIpAddress?.ToString() ??
                                                        context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
                                                        "unknown",
                                                        _ => new FixedWindowRateLimiterOptions
                                                             {
                                                                 PermitLimit = 120, Window = TimeSpan.FromSeconds(60),
                                                                 QueueProcessingOrder = QueueProcessingOrder.OldestFirst, QueueLimit = 3,
                                                                 AutoReplenishment = true
                                                             });
    });

    options.AddPolicy("download", context =>
    {
        return RateLimitPartition.GetSlidingWindowLimiter(
                                                          context.Connection.RemoteIpAddress?.ToString() ??
                                                          context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
                                                          "unknown",
                                                          _ => new SlidingWindowRateLimiterOptions
                                                               {
                                                                   PermitLimit = 5, Window = TimeSpan.FromSeconds(60),
                                                                   SegmentsPerWindow = 10,
                                                                   QueueProcessingOrder = QueueProcessingOrder.OldestFirst, QueueLimit = 2,
                                                                   AutoReplenishment = true
                                                               });
    });

    options.OnRejected = async (context, token) =>
    {
        var origin = context.HttpContext.Request.Headers.Origin.FirstOrDefault();
        var allowedOrigins = new[] { "https://localhost:3000", "https://jiten.moe" };

        if (!string.IsNullOrEmpty(origin) && allowedOrigins.Contains(origin))
        {
            context.HttpContext.Response.Headers.Append("Access-Control-Allow-Origin", origin);
        }

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
        }

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "text/plain";
        await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
    };
});

builder.Services.AddResponseCaching();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowSpecificOrigin",
                      policy =>
                      {
                          policy.WithOrigins("https://localhost:3000",
                                             "https://jiten.moe")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

// Hangfire jobs
builder.Services.AddScoped<ParseJob>();
builder.Services.AddScoped<ReparseJob>();
builder.Services.AddScoped<ComputationJob>();

builder.Services.AddHangfire(configuration =>
                                 configuration.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                                              .UseSimpleAssemblyNameTypeSerializer()
                                              .UseRecommendedSerializerSettings()
                                              .UsePostgreSqlStorage((options) =>
                                                                        options.UseNpgsqlConnection(() => builder.Configuration
                                                                            .GetConnectionString("JitenDatabase"))));

builder.Services.AddHangfireServer((options) => { options.WorkerCount = Environment.ProcessorCount / 4; });


var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
                        {
                            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                        });

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
        c.RoutePrefix = "";
    });
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseCors("AllowSpecificOrigin");

app.UseResponseCaching();

app.UseRateLimiter();

app.UseStaticFiles();

bool.TryParse(app.Configuration["UseBunnyCdn"], out var useBunnyCdn);
if (useBunnyCdn)
{
    //
}
else
{
    app.UseStaticFiles(new StaticFileOptions
                       {
                           FileProvider =
                               new PhysicalFileProvider(app.Configuration["StaticFilesPath"] ??
                                                        throw new Exception("Please set the StaticFilesPath in appsettings.json")),
                           RequestPath = "/static"
                       });
}

app.UseHangfireDashboard();

app.MapSwagger();
app.MapControllers();
app.MapHangfireDashboard();

app.Run();