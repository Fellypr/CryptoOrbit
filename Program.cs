using CryptoOrbit.Interfaces;
using CryptoOrbit.Services;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "AllowFrontend";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .WithHeaders("Content-Type", "X-Groq-Key", "x-cg-demo-api-key");
    });
});

builder.Services
    .AddHttpClient<IGroqInterfece, GroqServices>(client =>
    {
        client.BaseAddress = new Uri("https://api.groq.com/");
    });

builder.Services
    .AddHttpClient<ICripto, CriptoService>(client =>
    {
        client.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
    });

builder.Services.AddHostedService<CriptoCacheBackgroundService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicyName);
app.MapControllers();

app.Run();
