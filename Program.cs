using CryptoOrbit.Configurations;
using CryptoOrbit.Interfaces;
using CryptoOrbit.Services;
using Microsoft.Extensions.Options;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

builder.Services.AddHostedService<CriptoService>();

builder.Services.AddHttpClient<IGroqInterfece, GroqServices>();






builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy.WithOrigins(
            "http://localhost:3000"
        )
            .AllowAnyMethod()
            .AllowAnyHeader());
});



builder.Services.Configure<ExternalServicesOptions>
(
    builder.Configuration.GetSection("ExternalServices")
);

builder.Services.AddHttpClient("CryptoApi", (servicesProvider,client) =>
{
    var options = servicesProvider.GetRequiredService<IOptions<ExternalServicesOptions>>();
    client.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
    client.DefaultRequestHeaders.Add("x-cg-demo-api-key", options.Value.ApiKeyCoin);
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();   
}
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

