using VSSAuthPrototype.Data;
using VSSAuthPrototype.Repositories;
using VSSAuthPrototype.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var clerkAuthority = builder.Configuration["CLERK_AUTHORITY"];

// ───── Services ─────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ───── Database (Supabase PostgreSQL) ─────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ───── Repositories ─────
builder.Services.AddScoped<IStreamRepository, StreamRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// ───── Application Services ─────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IClerkService, ClerkService>();
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddHttpClient();

// ───── Authentication (Clerk JWKS) ─────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = clerkAuthority;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// ───── Pipeline ─────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();