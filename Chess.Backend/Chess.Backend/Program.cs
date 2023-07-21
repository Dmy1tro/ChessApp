using System.Security.Claims;
using Chess.Backend.HubFilters;
using Chess.Backend.Hubs;
using Chess.Backend.Services;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddSignalR(options =>
{
    options.AddFilter<GetUserFromAuthHubFilter>();
});
builder.Services.AddCors(setup =>
{
    setup.AddPolicy(
        "AngularApp",
        policy => policy.WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
});

builder.Services.AddTransient<GameManager>();
builder.Services.AddScoped<UserProvider>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AngularApp");
app.UseHttpsRedirection();
app.UseAuthentication();
app.Use((context, next) =>
{
    if (context.Request.Headers.TryGetValue("Authorization", out var bearerToken))
    {
        SetUser(bearerToken.ToString().Split(' ').Last());
    }

    if (context.Request.Query.TryGetValue("access_token", out var token))
    {
        SetUser(token);
    }

    void SetUser(string token)
    {
        var identity = new ClaimsIdentity("IdToken");
        identity.AddClaim(new Claim(ClaimTypes.Upn, token));

        context.User = new ClaimsPrincipal(identity);

        var userProvider = context.RequestServices.GetRequiredService<UserProvider>();
        userProvider.SetUser(identity);
    }

    return next(context);
});
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChessHub>("api/chess");

app.Run();
