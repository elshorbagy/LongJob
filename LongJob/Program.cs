using LongJob.Application.Abstractions;
using LongJob.Endpoints;
using LongJob.Infrastructure;
using LongJob.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ILongJobService, LongJobService>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

app.MapJobEndpoints();

app.UseGlobalExceptionHandling();

app.Run();
