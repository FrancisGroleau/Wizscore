using Microsoft.AspNet.SignalR;
using Microsoft.EntityFrameworkCore;
using Wizscore.Configuration;
using Wizscore.Extensions;
using Wizscore.Hubs;
using Wizscore.Managers;
using Wizscore.Persistence;
using Wizscore.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<Settings>(builder.Configuration.GetSection("Settings"));

builder.SetupPersistence();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();


// Register services
builder.Services.AddTransient<IGameManager, GameManager>();
builder.Services.AddTransient<IGameRepository, GameRepository>();
builder.Services.AddTransient<IPlayerRepository, PlayerRepository>();
builder.Services.AddTransient<IRoundRepository, RoundRepository>(); 
builder.Services.AddTransient<IBidRepository, BidRepository>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.ApplyMigrations();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<WaitingRoomHub>("/waitingRoomHub");
app.MapHub<BidHub>("/bidHub");
app.MapHub<BidWaitingRoomHub>("/bidWaitingRoomHub");
app.MapHub<ScoreHub>("/scoreHub");

app.Run();
