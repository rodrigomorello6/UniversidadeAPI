using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using System.Data;
using System.Text;
using UniversidadeAPI.Repositories;
using UniversidadeAPI.Repositories.Interfaces;
using UniversidadeAPI.Services;
using UniversidadeAPI.Services.Interfaces;

namespace UniversidadeAPI
{
    public class Program
    {
        private const string AngularCorsPolicy = "AngularApp";

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("A string de conexão 'DefaultConnection' não foi encontrada.");

            builder.Services.AddControllers();

            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<Program>();

            var corsOrigins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>()
                ?? new[]
                {
                    "http://localhost:4200",
                    "https://localhost:4200",
                    "http://127.0.0.1:4200",
                    "https://127.0.0.1:4200"
                };

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(AngularCorsPolicy, policy =>
                {
                    policy.WithOrigins(corsOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            builder.Services.AddScoped<IDbConnection>(_ => new MySqlConnection(connectionString));

            builder.Services.AddSingleton<ITokenService, TokenService>();

            builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            builder.Services.AddScoped<IDepartamentoRepository, DepartamentoRepository>();
            builder.Services.AddScoped<IAlunoRepository, AlunoRepository>();
            builder.Services.AddScoped<IProfessorRepository, ProfessorRepository>();
            builder.Services.AddScoped<ICursoRepository, CursoRepository>();
            builder.Services.AddScoped<IDisciplinaRepository, DisciplinaRepository>();
            builder.Services.AddScoped<ISalaDeAulaRepository, SalaDeAulaRepository>();
            builder.Services.AddScoped<IHorarioRepository, HorarioRepository>();
            builder.Services.AddScoped<ITurmaRepository, TurmaRepository>();
            builder.Services.AddScoped<IMatriculaRepository, MatriculaRepository>();
            builder.Services.AddScoped<INotaRepository, NotaRepository>();
            builder.Services.AddScoped<IPrerequisitoRepository, PrerequisitoRepository>();
            builder.Services.AddScoped<IGradeCurricularRepository, GradeCurricularRepository>();

            builder.Services.AddScoped<ITurmaService, TurmaService>();

            builder.Services.AddAutoMapper(typeof(Program));

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { Title = "UniversidadeAPI", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header usando o esquema Bearer. Exemplo: 'Bearer {seu token}'",
                    Name = "Authorization",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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
            });

            var jwtSettings = builder.Configuration.GetSection("Jwt");
            _ = jwtSettings["Key"] ?? throw new InvalidOperationException("Chave JWT não configurada.");

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
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                    };
                });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseCors(AngularCorsPolicy);

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
