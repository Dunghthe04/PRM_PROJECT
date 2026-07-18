using System.Security.Claims;
using System.Text;
using Api.Common;
using Api.Models;
using Api.Repositories;
using Api.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Cho phép client gửi enum dạng chuỗi ("Class", "Teacher"…) hoặc số.
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
// Flutter Web (localhost:xxxx) gọi API cổng khác → browser bắt buộc CORS.
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;
                return uri.Host is "localhost" or "127.0.0.1";
            })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
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
builder.Services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
builder.Services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IFeeCategoryRepository, FeeCategoryRepository>();
builder.Services.AddScoped<IFeeInvoiceRepository, FeeInvoiceRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAuthService, AuthService>();
// OTP quên MK gửi Gmail thật (Smtp); OTP đăng ký SĐT vẫn log console.
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IOtpDeliveryService, SmtpOtpDeliveryService>();
builder.Services.AddScoped<ISemesterService, SemesterService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<ITeacherAssignmentService, TeacherAssignmentService>();
builder.Services.AddScoped<ITimetableService, TimetableService>();
builder.Services.AddScoped<IGradeService, GradeService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<ILeaveRequestService, LeaveRequestService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();

// Push notification (FR1.4): nếu có file service account của Firebase thì gửi
// push THẬT qua Firebase Admin SDK; không có thì fallback log ra console (dev).
var firebaseCredPath = builder.Configuration["Firebase:CredentialPath"];
var firebaseCredFullPath = string.IsNullOrWhiteSpace(firebaseCredPath)
    ? null
    : Path.Combine(builder.Environment.ContentRootPath, firebaseCredPath);

if (firebaseCredFullPath != null && File.Exists(firebaseCredFullPath))
{
    // Khởi tạo FirebaseApp mặc định 1 lần khi app khởi động.
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(firebaseCredFullPath),
    });
    builder.Services.AddScoped<IPushNotificationService, FirebaseAdminPushNotificationService>();
}
else
{
    builder.Services.AddScoped<IPushNotificationService, ConsolePushNotificationService>();
}
builder.Services.AddScoped<IFeeService, FeeService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddHttpClient();

// wwwroot phải tồn tại trước Build để StaticFiles phục vụ /avatars, /uploads
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "avatars"));
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads"));

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

// Dev: KHÔNG redirect http→https để mobile (Android emulator) gọi http trực tiếp được.
// Production vẫn bắt buộc https.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseCors("DevCors");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed dữ liệu mẫu ở Development (idempotent) — tài khoản, phụ huynh-con, bảng tin.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db, app.Configuration);
}

app.Run();
