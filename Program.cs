using VSSAuthPrototype.Data;
using VSSAuthPrototype.Repositories;
using VSSAuthPrototype.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var clerkAuthority = builder.Configuration["CLERK_AUTHORITY"];
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "VarsitySportsShow2024SecretKeyForJWTAuthenticationSystem32Chars";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "VarsitySportsSystem";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "VarsitySportsUsers";

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

// ───── Authentication (Dual: Clerk JWKS + Local JWT) ─────
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "DualAuth";
        options.DefaultChallengeScheme = "DualAuth";
    })
    .AddJwtBearer("Clerk", options =>
    {
        options.Authority = clerkAuthority;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    })
    .AddJwtBearer("LocalJwt", options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    })
    .AddPolicyScheme("DualAuth", "Clerk or Local JWT", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            // Check the Authorization header for a Bearer token
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return "Clerk"; // default to Clerk

            var token = authHeader["Bearer ".Length..].Trim();

            // Local JWTs start with eyJ and contain our issuer
            // Clerk JWTs also start with eyJ but have a different issuer
            // Try to peek at the token to determine which scheme to use
            try
            {
                var parts = token.Split('.');
                if (parts.Length == 3)
                {
                    var payload = parts[1];
                    // Add padding if needed
                    payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                    var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));

                    if (json.Contains(jwtIssuer))
                        return "LocalJwt";
                }
            }
            catch
            {
                // If we can't parse, default to Clerk
            }

            return "Clerk";
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