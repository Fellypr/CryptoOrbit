using CryptoOrbit.Configurations;
using CryptoOrbit.Interfaces;
using CryptoOrbit.Services;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "AllowFrontend";

// Registra classes de configuração tipadas
builder.Services.Configure<ExternalServicesOptions>(
    builder.Configuration.GetSection(ExternalServicesOptions.SectionName));
builder.Services.Configure<CacheOptions>(
    builder.Configuration.GetSection(CacheOptions.SectionName));

var externalServices = builder.Configuration
    .GetSection(ExternalServicesOptions.SectionName)
    .Get<ExternalServicesOptions>() ?? new ExternalServicesOptions();

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
            .WithHeaders("Content-Type", "Authorization", "X-AI-Key", "X-Groq-Key", "x-cg-demo-api-key");
    });
});


builder.Services
    .AddHttpClient<IAiService, NineRouterService>(client =>
    {
        var baseUrl = !string.IsNullOrWhiteSpace(externalServices.NineRouter?.BaseUrl)
            ? externalServices.NineRouter.BaseUrl
            : "http://localhost:20128/";

        client.BaseAddress = new Uri(baseUrl);
    });


builder.Services.AddScoped<IGroqInterfece>(sp => (NineRouterService)sp.GetRequiredService<IAiService>());


builder.Services
    .AddHttpClient<ICripto, CriptoService>(client =>
    {
        var baseUrl = !string.IsNullOrWhiteSpace(externalServices.CoinGecko?.BaseUrl)
            ? externalServices.CoinGecko.BaseUrl
            : "https://api.coingecko.com/api/v3/";

        client.BaseAddress = new Uri(baseUrl);

        client.DefaultRequestHeaders.UserAgent.ParseAdd("CryptoOrbit/1.0 (Windows NT 10.0; Win64; x64)");
    });

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
