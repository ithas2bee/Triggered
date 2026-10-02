using Triggered.Application.UseCases;
using Triggered.Infrastructure;
using Triggered.Presentation;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<CreateObservationUseCase>();
builder.Services.AddScoped<CreateFoodUseCase>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapObservationEndpoints();

app.Run();
