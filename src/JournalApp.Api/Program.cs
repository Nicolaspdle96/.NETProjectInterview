using JournalApp.Api.Services;
using JournalApp.Application;
using JournalApp.Application.Common.Interfaces;
using JournalApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();

public partial class Program;
