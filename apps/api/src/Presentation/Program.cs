using Triggered.Application.UseCases;
using Triggered.Infrastructure;
using Triggered.Presentation;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<CreateObservationUseCase>();
builder.Services.AddScoped<CreateFoodUseCase>();
builder.Services.AddScoped<GetFoodsUseCase>();
builder.Services.AddScoped<GetFoodByIdUseCase>();
builder.Services.AddScoped<GetObservationsUseCase>();
builder.Services.AddScoped<UpdateObservationUseCase>();
builder.Services.AddScoped<GetFoodObservationsUseCase>();
builder.Services.AddScoped<CalculateFoodTriggerPatternUseCase>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapObservationEndpoints();
app.MapFoodEndpoints();

app.Run();
