using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using MEBservis.Application.Mapping;
using MEBservis.Application.Services;
using MEBservis.Domain.Interfaces;
using MEBservis.Infrastructure.Data;
using MEBservis.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// AutoMapper'� ekleyin
builder.Services.AddAutoMapper(typeof(MappingProfile)); // Profil t�r�n� belirtin

// Hizmetleri konteyn�ra ekle
builder.Services.AddControllers();

// Ba��ml�l�k Enjeksiyonu
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITrackingSessionRepository, TrackingSessionRepository>();
builder.Services.AddScoped<ITrackingSessionService, TrackingSessionService>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Sorun: �al��ma zaman�nda async eki kald�r�l�r. ��z�m: Bu kod blo�unu ekledim.
builder.Services.AddControllers(
    options => {
        options.SuppressAsyncSuffixInActionNames = false;
    }
);

// Veritaban� ba�lam�n� yap�land�r�n
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
{
    var connStr = builder.Configuration.GetConnectionString("ApplicationConnection");
    opt.UseSqlServer(connStr);
});

// Swagger jenerat�r�n� kaydet, bir veya daha fazla Swagger belgesi tan�mla
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = "MEBservis.xml"; // XML dosyan�z�n ad�
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MEBservis API", Version = "v1" });
});

// JWT do�rulama ve kimlik do�rulama hizmetlerini ekleyin
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        // JWT'nin verilece�i adres
        var issuer = builder.Configuration["Jwt:Issuer"];
        // JWT imzalama anahtar�
        var key = builder.Configuration["Jwt:Key"];
        // �mzalama anahtar�n� olu�turun
        var issuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

        // JWT do�rulama parametrelerini ayarlay�n
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            // Ge�erli issuer
            ValidIssuer = issuer,
            // �mzalama anahtar�
            IssuerSigningKey = issuerSigningKey,
            // Token ya�am s�resi kontrol�
            ValidateLifetime = true,
            // Issuer kontrol�
            ValidateIssuer = true,
            // �mzalama anahtar� kontrol�
            ValidateIssuerSigningKey = true,
            // Audience kontrol� (burada devre d���)
            ValidateAudience = false,
            // Rol i�in claim tipi
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            // �sim i�in claim tipi
            NameClaimType = System.Security.Claims.ClaimTypes.Name
        };

        // JWT olaylar�n� yap�land�r�n
        opt.Events = new JwtBearerEvents
        {
            // Kimlik do�rulama hatalar�n� ele alma
            OnChallenge = context =>
            {
                // �stemciye hatal� token g�nderildi�inde Authorization ba�l���n� d�nd�r
                context.Response.Headers["Authorization"] = context.Request.Headers["Authorization"];
                return Task.CompletedTask;
            }
        };
    });

// Uygulama yap�land�rmas�n� olu�turun
var app = builder.Build();

// HTTP istek boru hatt�n� yap�land�r
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MEBservis API V1");
        c.RoutePrefix = string.Empty; // Swagger UI k�k URL'de yer alacak
    });
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers(); // API denetleyicilerini y�nlendir

app.Run(); // Uygulamay� �al��t�r
