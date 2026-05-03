using BFF.API.Business.Implementations;
using BFF.API.Business.Interfaces;
using BFF.API.Hubs;
using BFF.API.ServiceClient.Analytics;
using BFF.API.ServiceClient.Order;
using BFF.API.ServiceClient.Product;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Shared;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BFF.API", Version = "v1" });
    c.SwaggerDoc("notification", new OpenApiInfo { Title = "BFF.Notification", Version = "notification" });
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        var groupName = apiDesc.GroupName ?? "v1";
        return groupName == docName;
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-Api-Key"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };

        // Allow JWT via SignalR query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// CORS for Angular
builder.Services.AddCors(opts => opts.AddPolicy("Angular", p =>
    p.WithOrigins("http://localhost:4200")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

// Rate Limiting
builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter("fixed", o =>
    {
        o.PermitLimit = 60;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 5;
    });
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// SignalR 
builder.Services.AddSignalR();

// Auth service 
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ICryptoService, CryptoService>();

// Named HttpClients
foreach (var (name, keyKey) in new[]
{
    ("ProductApi",   "Services:ProductApi:ApiKey"),
    ("AnalyticsApi", "Services:AnalyticsApi:ApiKey"),
    ("OrderApi",     "Services:OrderApi:ApiKey"),
})
{
    builder.Services.AddHttpClient(name, (sp, client) =>
    {
        client.DefaultRequestHeaders.Add("X-Api-Key",
            sp.GetRequiredService<IConfiguration>()[keyKey]);
    }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });
}

// Generated API clients
builder.Services.AddScoped<IProductApiClient>(sp =>
    new ProductApiClient(
        sp.GetRequiredService<IConfiguration>()["Services:ProductApi:BaseUrl"]!,
        sp.GetRequiredService<IHttpClientFactory>().CreateClient("ProductApi")));

builder.Services.AddScoped<IAnalyticsApiClient>(sp =>
    new AnalyticsApiClient(
        sp.GetRequiredService<IConfiguration>()["Services:AnalyticsApi:BaseUrl"]!,
        sp.GetRequiredService<IHttpClientFactory>().CreateClient("AnalyticsApi")));

builder.Services.AddScoped<IOrderApiClient>(sp =>
    new OrderApiClient(
        sp.GetRequiredService<IConfiguration>()["Services:OrderApi:BaseUrl"]!,
        sp.GetRequiredService<IHttpClientFactory>().CreateClient("OrderApi")));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("Angular");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// /api/notification → API key (called by Order.API, no JWT)
app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api/notification"),
    branch => branch.UseMiddleware<ApiKeyMiddleware>());

app.MapControllers().RequireRateLimiting("fixed");
app.MapHub<OrderStatusHub>("/hubs/order-status");

app.Run();

