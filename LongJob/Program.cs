using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


const string FrontendCors = "FrontendCors";
builder.Services.AddCors(o =>
{
    o.AddPolicy(FrontendCors, p => p
        .WithOrigins(" http://127.0.0.1:50813")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseCors(FrontendCors);

app.MapControllers();

app.Run();

//Run("dotnet", "run", @"C:\Users\elsho\source\repos\LongJob\LongJob.Api");

//Run("ng", "serve", @"C:\Users\elsho\source\repos\LongJob\Client");

//static void Run(string cmd, string args, string workingDir)
//{
//    Process.Start(new ProcessStartInfo
//    {
//        FileName = cmd,
//        Arguments = args,
//        WorkingDirectory = workingDir,
//        UseShellExecute = true
//    });
//}