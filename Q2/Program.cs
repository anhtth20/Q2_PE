using Microsoft.EntityFrameworkCore;
using Q2.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddDbContext<Prnsum25B123Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));

//Use the connection string below to connect to the database.
var connectionStr
    = builder.Configuration.GetConnectionString("MyCnn");

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapRazorPages();

app.Run();

