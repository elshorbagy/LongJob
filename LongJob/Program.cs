using LongJob.Application.Abstractions;
using LongJob.Endpoints;
using LongJob.Infrastructure;
using LongJob.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ILongJobService, LongJobService>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.MapJobEndpoints();

app.UseGlobalExceptionHandling();

app.Run();