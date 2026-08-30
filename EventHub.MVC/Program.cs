using EventHub.MVC.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMvcPresentation(builder.Configuration);

var app = builder.Build();

app.UseMvcPresentation();

app.Run();
