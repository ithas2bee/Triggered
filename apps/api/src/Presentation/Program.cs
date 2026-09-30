using Triggered.Application.UseCases;
using Triggered.Infrastructure;
using Triggered.Presentation;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<CreateObservationUseCase>();

var app = builder.Build();

app.MapObservationEndpoints();

app.Run();
