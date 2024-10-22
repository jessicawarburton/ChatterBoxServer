using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Server.Data;
using Server.Data.Models;
using Server.Services;

var builder = WebApplication.CreateBuilder(args);


var tokenSettings = builder.Configuration.GetSection("TokenSettings").Get<TokenSettings>() ?? default;

ConfigureServices(builder.Services, builder.Configuration, tokenSettings);
var app = builder.Build();
ConfigureMiddleware(app);

app.Run();


void ConfigureServices(IServiceCollection services, IConfiguration configuration, TokenSettings tokenSettings)
{
    ConfigureDbContext(services, configuration);
    ConfigureIdentity(services);
    ConfigureJwtAuthentication(services, tokenSettings);
    ConfigureSwagger(services);
    ConfigureCors(services, tokenSettings);

    services.AddSignalR();
    services.AddScoped<DbInitializer>();
    services.AddSingleton<TokenSettings>(tokenSettings);
    services.AddSignalR();
    services.AddTransient<UserService>();
    services.AddTransient<MessageService>();
    services.AddControllers();
}

void ConfigureDbContext(IServiceCollection services, IConfiguration configuration)
{
    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(configuration.GetConnectionString("DefaultConnection"),
            x => x.MigrationsAssembly("Server.Data")));
}

void ConfigureIdentity(IServiceCollection services)
{
    services.AddIdentityCore<ApplicationUser>()
        .AddRoles<IdentityRole>()
        .AddSignInManager()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddTokenProvider<DataProtectorTokenProvider<ApplicationUser>>("REFRESHTOKENPROVIDER");

    services.Configure<DataProtectionTokenProviderOptions>(options =>
    {
        options.TokenLifespan = TimeSpan.FromSeconds(3500);
    });
}

void ConfigureJwtAuthentication(IServiceCollection services, TokenSettings tokenSettings)
{
    services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RequireExpirationTime = true,
                ValidIssuer = tokenSettings.Issuer,
                ValidAudience = tokenSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSettings.SecretKey)),
                ClockSkew = TimeSpan.Zero
            };
            options.IncludeErrorDetails = true;

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var error = context.Exception;
                    Console.WriteLine($"Token validation failed: {error.Message}");
                    return Task.CompletedTask;
                }
            };
        });
}

void ConfigureSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(config =>
        {
            config.SwaggerDoc("v1", new OpenApiInfo { Title = "App Api", Version = "v1" });
            config.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "bearer"
            });
            config.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }

    void ConfigureCors(IServiceCollection services, TokenSettings tokenSettings)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("ApiRequests", builder =>
            {
                builder.AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithOrigins("http://localhost:4200")
                    .AllowCredentials();
            });
        });
    }

    void ConfigureMiddleware(WebApplication app)
    {
        // Enable Swagger in development
        if (app.Environment.IsDevelopment())
        {
            app.UseCors("ApiRequests");
            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapHub<ChatHub>("/chatHub");
            // Seed the database
            using var scope = app.Services.CreateScope();
            var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
            initializer.SeedAsync().Wait(); // Ensure seed happens
        }
        
        
        // Add middleware
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

    }