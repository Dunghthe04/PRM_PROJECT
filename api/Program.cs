using System.Security.Claims;
using System.Text;
using Api.Common;
using Api.Models;
using Api.Repositories;
using Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT token. Ví dụ: eyJhbGciOiJIUzI1NiIs..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtSettingsSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtSettings>(jwtSettingsSection);
var jwtSettings = jwtSettingsSection.Get<JwtSettings>();

if (jwtSettings != null && !string.IsNullOrEmpty(jwtSettings.Key))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                RoleClaimType = ClaimTypes.Role
            };
        });

    builder.Services.AddAuthorization();
}

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordResetOtpRepository, PasswordResetOtpRepository>();
builder.Services.AddScoped<ISemesterRepository, SemesterRepository>();
builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
builder.Services.AddScoped<IClassRepository, ClassRepository>();
builder.Services.AddScoped<ITeacherAssignmentRepository, TeacherAssignmentRepository>();
builder.Services.AddScoped<ITimetableRepository, TimetableRepository>();
builder.Services.AddScoped<IGradeRepository, GradeRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOtpDeliveryService, ConsoleOtpDeliveryService>();
builder.Services.AddScoped<ISemesterService, SemesterService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<ITeacherAssignmentService, TeacherAssignmentService>();
builder.Services.AddScoped<ITimetableService, TimetableService>();
builder.Services.AddScoped<IGradeService, GradeService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();

// wwwroot phải tồn tại trước Build để StaticFiles phục vụ /avatars
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "avatars"));

var app = builder.Build();

// Swagger chỉ bật ở Development — cần ASPNETCORE_ENVIRONMENT=Development (xem launchSettings.json)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FSchool API v1");
        options.RoutePrefix = "swagger";
    });

    // Truy cập https://localhost:xxxx/ → tự chuyển sang Swagger UI
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
