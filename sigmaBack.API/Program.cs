/*using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using sigmaBack.Application.Services;
using SigmaBack.Application.Services;
using sigmaBack.Domain.Interfaces;
using SigmaBack.Domain.Interfaces;
using SigmaBack.Infra.Data.Repositories;
using SigmaBack.Infrastructure.Repositories;
using System;
using System.Text;
using sigmaBack.Infra.Data.Repositories;

namespace sigmaBack.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configura o servidor para ouvir na porta 5001
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(5001);  // Escuta na porta 5001
            });

            ConfigureServices(builder.Services);

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseCors("CorsPolicy");

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SigmaBack v1");
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                                      .AllowAnyMethod()
                                      .AllowAnyHeader());
            });

            services.AddControllersWithViews();

            // Registro do contexto do banco de dados
            services.AddDbContext<sigmaBack.Infra.Data.Contexts.SigmaDbContext>(options =>
                options.UseSqlServer(GetConnectionString(), sqlServerOptions =>
                {
                    sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null
                    );
                })
            );

            // Configuração dos cookies
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.None; // Define o atributo SameSite como None
                options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
                options.Secure = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always; // Certifique-se de que os cookies são enviados apenas em conexões seguras (HTTPS)
            });

            // Configuração apis correios
            services.AddHttpClient<CorreiosService>();
            services.AddControllers();

            // Registro do serviço AvaliacaoService e outros serviços
            services.AddScoped<IAvaliacaoService, AvaliacaoService>();
            services.AddScoped<ICarrinhoCompraService, CarrinhoCompraService>();
            services.AddScoped<ICarrinhoCompraRepository, CarrinhoCompraRepository>();
            services.AddScoped<IUsuarioService, UsuarioService>();
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IProdutoRepository, ProdutoRepository>();
            services.AddScoped<IProdutoService, ProdutoService>();
            services.AddScoped<IPedidoService, PedidoService>();
            services.AddScoped<IPedidoRepository, PedidoRepository>();
            services.AddScoped<IItemPedidoService, ItemPedidoService>();
            services.AddScoped<IItemPedidoRepository, ItemPedidoRepository>();
            services.AddScoped<IItemCarrinhoService, ItemCarrinhoService>();
            services.AddScoped<IItemCarrinhoRepository, ItemCarrinhoRepository>();
            services.AddScoped<IEnderecoService, EnderecoService>();
            services.AddScoped<IEnderecoRepository, EnderecoRepository>();
            services.AddScoped<ICategoriaService, CategoriaService>();
            services.AddScoped<ICategoriaRepository, CategoriaRepository>();
            services.AddScoped<IAnuncioService, AnuncioService>();
            services.AddScoped<IAnuncioRepository, AnuncioRepository>();
            services.AddScoped<IUsuarioJogoRepository, UsuarioJogoRepository>();
            services.AddScoped<IUsuarioJogoService, UsuarioJogoService>();
            services.AddScoped<IJogoService, JogoService>();
            services.AddScoped<IJogoRepository, JogoRepository>();
            services.AddScoped<IFavoritoService, FavoritoService>();
            services.AddScoped<IFavoritoRepository, FavoritoRepository>();

            // Configuração do JWT
            var jwtSection = GetJwtSection();
            var jwtKey = jwtSection["Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new ArgumentNullException(nameof(jwtKey), "Chave JWT não está configurada.");
            }

            var key = Encoding.ASCII.GetBytes(jwtKey);
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "sigmaBack", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme (Example: 'Bearer 12345abcdef')",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Id = "Bearer",
                                Type = ReferenceType.SecurityScheme
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
        }

        private static string GetConnectionString()
        {
            // Obtém a string de conexão do arquivo appsettings.Development.json
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Development.json")
                .Build();

            return config.GetConnectionString("DefaultConnection") ?? "DefaultConnectionString";
        }

        private static IConfigurationSection GetJwtSection()
        {
            // Carrega a seção Jwt do arquivo appsettings.Development.json
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Development.json")
                .Build();

            return config.GetSection("Jwt");
        }
    }
}
*/

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using sigmaBack.Application.Services;
using sigmaBack.Domain.Interfaces;
using sigmaBack.Infra.Data.Contexts;
using sigmaBack.Infra.Data.Repositories;
using SigmaBack.Application.Services;
using SigmaBack.Domain.Interfaces;
using SigmaBack.Infra.Data.Repositories;
using SigmaBack.Infrastructure.Repositories;
using System.Text;

namespace sigmaBack.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ==============================
            // KESTREL (SWARM / DOCKER)
            // ==============================
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(5001);
            });

            var configuration = builder.Configuration;

            // ==============================
            // CORS
            // ==============================
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    p => p.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
            });

            // ==============================
            // CONTROLLERS
            // ==============================
            builder.Services.AddControllersWithViews();
            builder.Services.AddControllers();

            // ==============================
            // DB CONTEXT (SWARM READY)
            // ==============================
            builder.Services.AddDbContext<SigmaDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null
                        );
                    })
            );


            // ==============================
            // HTTP CLIENT
            // ==============================
            builder.Services.AddHttpClient<CorreiosService>();

            // ==============================
            // DEPENDENCY INJECTION
            // ==============================
            builder.Services.AddScoped<IAvaliacaoService, AvaliacaoService>();
            builder.Services.AddScoped<ICarrinhoCompraService, CarrinhoCompraService>();
            builder.Services.AddScoped<ICarrinhoCompraRepository, CarrinhoCompraRepository>();
            builder.Services.AddScoped<IUsuarioService, UsuarioService>();
            builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
            builder.Services.AddScoped<IProdutoService, ProdutoService>();
            builder.Services.AddScoped<IPedidoService, PedidoService>();
            builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
            builder.Services.AddScoped<IItemPedidoService, ItemPedidoService>();
            builder.Services.AddScoped<IItemPedidoRepository, ItemPedidoRepository>();
            builder.Services.AddScoped<IItemCarrinhoService, ItemCarrinhoService>();
            builder.Services.AddScoped<IItemCarrinhoRepository, ItemCarrinhoRepository>();
            builder.Services.AddScoped<IEnderecoService, EnderecoService>();
            builder.Services.AddScoped<IEnderecoRepository, EnderecoRepository>();
            builder.Services.AddScoped<ICategoriaService, CategoriaService>();
            builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
            builder.Services.AddScoped<IAnuncioService, AnuncioService>();
            builder.Services.AddScoped<IAnuncioRepository, AnuncioRepository>();
            builder.Services.AddScoped<IUsuarioJogoRepository, UsuarioJogoRepository>();
            builder.Services.AddScoped<IUsuarioJogoService, UsuarioJogoService>();
            builder.Services.AddScoped<IJogoService, JogoService>();
            builder.Services.AddScoped<IJogoRepository, JogoRepository>();
            builder.Services.AddScoped<IFavoritoService, FavoritoService>();
            builder.Services.AddScoped<IFavoritoRepository, FavoritoRepository>();

            // ==============================
            // SWAGGER
            // ==============================
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "sigmaBack",
                    Version = "v1"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Bearer {token}"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                        new string[] {}
                    }
                });
            });

            var app = builder.Build();

            // ==============================
            // PIPELINE
            // ==============================
            app.UseCors("CorsPolicy");

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}