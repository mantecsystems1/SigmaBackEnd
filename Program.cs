using Microsoft.AspNetCore.Authentication.JwtBearer;
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
                options.ListenAnyIP(5001); // HTTP público
		options.ListenAnyIP(5002, listenOptions =>
		{
    		listenOptions.UseHttps(); // HTTPS público
		});
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

            // Configura  o dos cookies
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.None; // Define o atributo SameSite como None
                options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
                options.Secure = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always; // Certifique-se de que os cookies s o enviados apenas em conex es seguras (HTTPS)
            });

            // Configura  o apis correios
            services.AddHttpClient<CorreiosService>();
            services.AddControllers();

            // Registro do servi o AvaliacaoService e outros servi os
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

            // Configura  o do JWT
            var jwtSection = GetJwtSection();
            var jwtKey = jwtSection["Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new ArgumentNullException(nameof(jwtKey), "Chave JWT n o est  configurada.");
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
            // Obt m a string de conex o do arquivo appsettings.Development.json
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Development.json")
                .Build();

            return config.GetConnectionString("DefaultConnection") ?? "DefaultConnectionString";
        }

        private static IConfigurationSection GetJwtSection()
        {
            // Carrega a se  o Jwt do arquivo appsettings.Development.json
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Development.json")
                .Build();

            return config.GetSection("Jwt");
        }
    }
}
