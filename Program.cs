using System.Collections.Generic;
using LittleBeaconAPI.Data;
using LittleBeaconAPI.Models;
using LittleBeaconAPI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure EF Core with SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register custom services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IShiftService, ShiftService>();

// Add CORS policy for Angular app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        builder => builder
            .WithOrigins("http://localhost:4201", "http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

var app = builder.Build();

// Apply migrations and seed initial data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if (!db.Users.Any())
    {
        var admin = new User
        {
            Username = "Admin",
            Password = "Admin",
            Role = "Admin"
        };

        db.Users.Add(admin);
        db.SaveChanges();

        var seedShifts = new List<Shift>
        {
            new Shift
            {
                Date = "1. November",
                Time = "08:00 - 16:00",
                Location = "Hjemme",
                UserId = admin.Id
            },
            new Shift
            {
                Date = "2. November",
                Time = "08:00 - 16:00",
                Location = "Kontoret",
                UserId = admin.Id
            },
            new Shift
            {
                Date = "3. November",
                Time = "10:00 - 14:00",
                Location = "Kontoret",
                UserId = admin.Id
            }
        };

        db.Shifts.AddRange(seedShifts);
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowAngularApp");

app.UseAuthorization();

app.MapControllers();

app.Run();

