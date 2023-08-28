using HamidaniTree.Model;
using HamidaniTree.Tools;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace HamidaniTree
{
    public static class SwaggerGenOptionsExtension
    {
        public static void DescribeAllEnumsAsStrings(this SwaggerGenOptions options)
        {
        }
    }

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            ServicePointManager.DefaultConnectionLimit = 100;
            //TODO:below code is temp befor read connection from ini file (remove it)
            MySql.Data.MySqlClient.MySqlConnectionStringBuilder builder = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder();
            builder.ConnectionString = Configuration.GetConnectionString("DefaultConnection");
            //TODO:Load connection string configuration from ini file
            MySQLConnectionGenerator.Initialize(host: builder.Server, userName: builder.UserID, port: builder.Port.ToString(), password: builder.Password);

            //services.AddTransient<IPasswordHasher<AppUser>, MD5PasswordHasher>();

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            string connectionString = Configuration.GetConnectionString("DefaultConnection");

            services.AddDbContextFactory<AppDbContext>(options =>
            {
                options.UseMySQL(connectionString);
            });

            services.AddDbContext<AppDbContext>(options => options.UseMySQL(connectionString));

            services.AddScoped<DbContext, AppDbContext>();

            services.AddCors(options => options.AddPolicy("hamidani", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader().DisallowCredentials();
            }));
            services.AddCors(c =>
            {
                c.AddPolicy("AllowOrigin", options => options.AllowAnyOrigin());
            });

            services.AddControllers();

            var jwtSettings = new JwtSettings() { Secret = Configuration["JWT:Key"] };
            Configuration.Bind(nameof(jwtSettings), jwtSettings);
            services.AddSingleton(jwtSettings);

            //services.AddIdentity<AppUser, UserRole>(options =>
            //{
            //    options.Password.RequiredLength = 4;
            //}).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();

            //services.AddScoped<IIdentityService, IdentityService>();
            //services.Configure<IdentityOptions>(options =>
            //{
            //    options.Password.RequiredLength = 4;
            //    options.Password.RequireDigit = false;
            //    options.Password.RequireLowercase = false;
            //    options.Password.RequireUppercase = false;
            //    options.Password.RequiredUniqueChars = 0;
            //    options.Password.RequireNonAlphanumeric = false;
            //});

            #region inject JWT

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o =>
            {
                var Key = Encoding.UTF8.GetBytes(Configuration["JWT:Key"]);
                o.SaveToken = true;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = Configuration["JWT:Issuer"],
                    ValidAudience = Configuration["JWT:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Key)
                };
            });
            //services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("Admin",
            //        authBuilder =>
            //        {
            //            authBuilder.RequireRole("Admin");
            //        });
            //});

            #endregion inject JWT

            #region ENABLE CONTENT COMPRESSION

            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.MimeTypes = new[] { "text/plain", "text/json" };
            });

            #endregion ENABLE CONTENT COMPRESSION

            services.AddSwaggerGen(option =>
            {
                option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Scheme = "bearer",
                    Type = SecuritySchemeType.ApiKey,
                    BearerFormat = "JWT"
                });

                option.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                            },
                            new List<string>()
                        }
                    });

                option.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Hamidani Tree Service API",
                    Version = "v1",
                    Contact = new OpenApiContact
                    {
                        Name = "Hamidani Tree",
                        Email = "info@hamidanitree.com",
                        Url = new Uri("https://www.hamidanitree.com/in/ignaciojv/")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "MIT",
                        Url = new Uri("https://github.com/gamadev/hamidanitree/blob/master/LICENSE")
                    }
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();

            //app.UseAuthentication(); // This need to be added

            app.UseRouting();

            app.UseCors("hamidani");

           // app.UseAuthorization();

            app.UseSwagger();

            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hamidani Tree v1"));

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}