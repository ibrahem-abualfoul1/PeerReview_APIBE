using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PeerReview.Application.Abstractions;
using PeerReview.Infrastructure.Files;
using PeerReview.Infrastructure.Identity;
using PeerReview.Infrastructure.Persistence;
using PeerReview.Infrastructure.Seed;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ================== EF Core ==================
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ================== JWT Auth =================
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        // للتطوير فقط (HTTP محلي): عطّل شرط HTTPS للميتا
        o.RequireHttpsMetadata = false;

        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["Key"]!)
            )
        };
    });

// ============ DI for abstractions ============
builder.Services.AddSingleton<IJwtTokenService>(_ =>
    new JwtTokenService(jwt["Issuer"]!, jwt["Audience"]!, jwt["Key"]!));

builder.Services.AddSingleton<IFileStorage>(_ =>
    new LocalFileStorage(Path.Combine(Directory.GetCurrentDirectory(),
        builder.Configuration["UploadRoot"]!)));

// ======== MVC + Validation + ProblemDetails ========
builder.Services.AddProblemDetails();

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = "Validation Failed",
            Status = StatusCodes.Status400BadRequest,
            Detail = "One or more validation errors occurred.",
            Instance = context.HttpContext.Request.Path
        };

        problem.Extensions["errors"] = context.ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            );

        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(problem);
    };
});

// ================= Swagger =================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PeerReview.Api", Version = "v1" });

    // JWT Bearer scheme
    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Paste your JWT here. Example: **Bearer eyJhbGciOi...**",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",      // must be "bearer"
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme // "Bearer"
        }
    };

    // Register the scheme
    c.AddSecurityDefinition(jwtScheme.Reference.Id, jwtScheme);

    // Require it by default for all operations
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtScheme, Array.Empty<string>() }
    });

    // (Optional) Include XML comments if you generate them
    // var xml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    // c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xml));
});

// ================== CORS ====================
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? new[] {    "http://localhost:4200",
    "https://localhost:4200",
    "https://localhost:57784",
    "http://localhost:57784" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("ng", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              // لو عندك تنزيل ملفات وتحتاج اسم الملف في الهيدر:
              .WithExposedHeaders("Content-Disposition")
              // Bearer فقط؟ لا تفعل الكريدنشلز.
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });
});

var app = builder.Build();

// ======== DB init/seed (تبديل EnsureCreated → Migrations بالإنتاج) ========
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated(); // يفضّل Migrate() مع المايجريشنز في الإنتاج
    await SeedData.Run(db);
}

// ================== Pipeline =================

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PeerReview.Api v1");
        c.DocumentTitle = "PeerReview API Docs";
        c.ConfigObject.PersistAuthorization = true; // ✅ keep token between refreshes
    });


app.UseExceptionHandler();     // ProblemDetails
app.UseStatusCodePages();

app.UseStaticFiles();

// ✅ CORS لازم يكون قبل المصادقة وقبل MapControllers
app.UseCors("ng");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
