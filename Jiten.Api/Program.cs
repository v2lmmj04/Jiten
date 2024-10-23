using Jiten.Core;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<JitenDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("JitenDatabase")));

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

app.UseHttpsRedirection();
app.UseStaticFiles();

app.MapSwagger();
app.MapControllers();

app.Run();