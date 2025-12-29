using AspNetCoreRateLimit;
using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Core.Services;
using Famick.HomeManagement.Infrastructure.Data;
using Famick.HomeManagement.Infrastructure.Services;
using Famick.HomeManagement.Web.Middleware;
using Famick.HomeManagement.Web.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Net;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add optional configuration from mounted volume (for Docker deployments)
// This allows users to override settings without rebuilding the image
var configPath = Path.Combine(builder.Environment.ContentRootPath, "config", "appsettings.json");
if (File.Exists(configPath))
{
    builder.Configuration.AddJsonFile(configPath, optional: true, reloadOnChange: true);
}

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/homemanagement-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllersWithViews();

// Add API controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Configure IP rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Configure forwarded headers for reverse proxy (nginx, etc.)
var trustedProxies = builder.Configuration.GetSection("ReverseProxy:TrustedProxies").Get<string[]>();
var trustedNetworks = builder.Configuration.GetSection("ReverseProxy:TrustedNetworks").Get<string[]>();

Log.Information("Reverse Proxy Configuration - TrustedProxies: {Proxies}, TrustedNetworks: {Networks}",
    trustedProxies != null ? string.Join(", ", trustedProxies) : "(none - trust all)",
    trustedNetworks != null ? string.Join(", ", trustedNetworks) : "(none)");

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;

    if (trustedProxies?.Length > 0 || trustedNetworks?.Length > 0)
    {
        // Use explicitly configured proxies/networks
        if (trustedProxies != null)
        {
            foreach (var proxy in trustedProxies)
            {
                if (IPAddress.TryParse(proxy, out var ip))
                    options.KnownProxies.Add(ip);
            }
        }

        if (trustedNetworks != null)
        {
            foreach (var network in trustedNetworks)
            {
                var parts = network.Split('/');
                if (parts.Length == 2 &&
                    IPAddress.TryParse(parts[0], out var ip) &&
                    int.TryParse(parts[1], out var prefix))
                {
#pragma warning disable ASPDEPR005 // KnownNetworks is obsolete
                    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(ip, prefix));
#pragma warning restore ASPDEPR005
                }
            }
        }
    }
    else
    {
        // Default: trust all proxies (for simple Docker setups)
#pragma warning disable ASPDEPR005 // KnownNetworks is obsolete
        options.KnownNetworks.Clear();
#pragma warning restore ASPDEPR005
        options.KnownProxies.Clear();
    }
});

// Configure database context
builder.Services.AddDbContext<HomeManagementDbContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsAssembly("Famick.HomeManagement.Infrastructure");
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
    });
});

// Register HttpContextAccessor for tenant resolution from HTTP context
builder.Services.AddHttpContextAccessor();

// Register TenantProvider (Fixed Tenant for self-hosted)
var fixedTenantId = builder.Configuration.GetValue<Guid>("FixedTenantId");
if (fixedTenantId == Guid.Empty)
{
    fixedTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
}
builder.Services.AddScoped<ITenantProvider>(sp =>
    new FixedTenantProvider(fixedTenantId, sp.GetRequiredService<IHttpContextAccessor>()));

// Configure Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Prevent automatic claim type mapping (keeps "sub" as-is instead of mapping to long URI)
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero,
        NameClaimType = "sub"  // Use "sub" claim as the user identifier
    };
});

builder.Services.AddAuthorization();

// Register authentication services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<ISetupService, SetupService>();

// Register file storage service (for product images)
builder.Services.AddSingleton<IFileStorageService>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var logger = sp.GetRequiredService<ILogger<LocalFileStorageService>>();
    var baseUrl = builder.Configuration["BaseUrl"] ?? "";
    // WebRootPath can be null if wwwroot doesn't exist, fall back to ContentRootPath/wwwroot
    var webRootPath = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
    Directory.CreateDirectory(webRootPath); // Ensure it exists
    return new LocalFileStorageService(webRootPath, baseUrl, logger);
});

// Register business services (from homemanagement-shared)
builder.Services.AddScoped<IProductGroupService, ProductGroupService>();
builder.Services.AddScoped<IShoppingLocationService, ShoppingLocationService>();
builder.Services.AddScoped<IShoppingListService, ShoppingListService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IChoreService, ChoreService>();
builder.Services.AddScoped<IProductsService, ProductsService>();
builder.Services.AddScoped<IStockService, StockService>();

// Register data seeder
builder.Services.AddScoped<DataSeeder>();

// Configure plugin system
builder.Services.Configure<Famick.HomeManagement.Infrastructure.Plugins.PluginLoaderOptions>(options =>
{
    options.PluginsPath = Path.Combine(builder.Environment.ContentRootPath, "plugins");
    options.LoadPluginsOnStartup = true;
});

// Register HttpClients for plugins
builder.Services.AddHttpClient<Famick.HomeManagement.Infrastructure.Plugins.Usda.UsdaFoodDataPlugin>();
builder.Services.AddHttpClient<Famick.HomeManagement.Infrastructure.Plugins.OpenFoodFacts.OpenFoodFactsPlugin>();

// Register built-in plugins (order matters for pipeline - first registered runs first)
builder.Services.AddSingleton<Famick.HomeManagement.Core.Interfaces.Plugins.IProductLookupPlugin,
    Famick.HomeManagement.Infrastructure.Plugins.Usda.UsdaFoodDataPlugin>();
builder.Services.AddSingleton<Famick.HomeManagement.Core.Interfaces.Plugins.IProductLookupPlugin,
    Famick.HomeManagement.Infrastructure.Plugins.OpenFoodFacts.OpenFoodFactsPlugin>();

// Register plugin loader and lookup service
builder.Services.AddSingleton<Famick.HomeManagement.Core.Interfaces.Plugins.IPluginLoader,
    Famick.HomeManagement.Infrastructure.Plugins.PluginLoader>();
builder.Services.AddScoped<IProductLookupService,
    Famick.HomeManagement.Infrastructure.Services.ProductLookupService>();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Famick.HomeManagement.Core.Mapping.ProductGroupMappingProfile).Assembly);

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Famick.HomeManagement.Core.Validators.ProductGroups.CreateProductGroupRequestValidator>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

// Add Swagger/OpenAPI for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Famick Home Management API",
        Version = "v1",
        Description = "Self-hosted home management API"
    });

    // JWT Bearer Authentication
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments if file exists
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Build the application
var app = builder.Build();

// Apply pending migrations on startup (configurable, default: true for self-hosted)
var autoMigrate = builder.Configuration.GetValue<bool>("Database:AutoMigrate", true);
if (autoMigrate)
{
    using var migrationScope = app.Services.CreateScope();
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<HomeManagementDbContext>();
    var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
    if (pendingMigrations.Any())
    {
        Log.Information("Applying {Count} pending database migration(s)...", pendingMigrations.Count());
        await dbContext.Database.MigrateAsync();
        Log.Information("Database migrations applied successfully");
    }
}

// Seed default data for the fixed tenant
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
    if (tenantProvider.TenantId.HasValue)
    {
        await seeder.SeedDefaultDataAsync(tenantProvider.TenantId.Value);
    }
}

// Load plugins on startup
var pluginLoader = app.Services.GetRequiredService<Famick.HomeManagement.Core.Interfaces.Plugins.IPluginLoader>();
await pluginLoader.LoadPluginsAsync();

// Configure the HTTP request pipeline

// Forwarded headers must be first for correct IP/protocol detection behind reverse proxy
app.UseForwardedHeaders();

// Global exception handling - must be early to catch all exceptions
app.UseExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Famick Home Management API v1");
    });
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Blazor WASM hosting
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// IP rate limiting - before routing
app.UseIpRateLimiting();

app.UseRouting();

// Tenant resolution middleware (for fixed tenant in self-hosted mode)
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Map health check endpoint
app.MapHealthChecks("/health");

// Map API controllers
app.MapControllers();

// Fallback to Blazor WASM for SPA routing
// This must be after MapControllers so API routes take precedence
app.MapFallbackToFile("index.html");

try
{
    Log.Information("Starting Famick Home Management application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible to integration tests
public partial class Program { }
