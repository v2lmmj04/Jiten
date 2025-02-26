using Jiten.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(Path.Combine(Environment.CurrentDirectory, "..", "Shared", "sharedsettings.json"), optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile("sharedsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization
                                                         .JsonNumberHandling.AllowNamedFloatingPointLiterals;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<JitenDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("JitenDatabase"),
                                                                           o =>
                                                                           {
                                                                               o.UseQuerySplittingBehavior(QuerySplittingBehavior
                                                                                   .SplitQuery);
                                                                           }));


builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowSpecificOrigin",
                      policy =>
                      {
                          policy.WithOrigins("https://localhost:3000")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
        c.RoutePrefix = "";
    });
}

app.UseRouting();

app.UseCors("AllowSpecificOrigin");

app.UseHttpsRedirection();

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


app.MapSwagger();
app.MapControllers();

app.Run();