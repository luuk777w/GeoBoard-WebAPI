using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using GeoBoardWebAPI.DAL;
using GeoBoardWebAPI.DAL.Entities;
using GeoBoardWebAPI.DAL.Repositories;
using GeoBoardWebAPI.Extensions.Authorization;
using GeoBoardWebAPI.Extensions.Authorization.Claims;
using GeoBoardWebAPI.Hubs;
using GeoBoardWebAPI.Models.Options;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GeoBoardWebAPI.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.OpenApi.Models;
using System.IO;
using System.Reflection;

namespace GeoBoardWebAPI
{
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
            // Configure the settings
            services.Configure<EmailSettings>(Configuration.GetSection("EmailSettings"));
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

            // Register the repository services.
            services.AddScoped(typeof(CountryRepository), typeof(CountryRepository));
            services.AddScoped(typeof(UserRepository), typeof(UserRepository));
            services.AddScoped(typeof(Repository<User>), typeof(UserRepository));

            // Register tge services.
            services.AddScoped(typeof(ITemplateService), typeof(TemplateService));
            services.AddScoped(typeof(IEmailService), typeof(EmailService));

            // Register the managers.
            services.AddScoped(typeof(AppUserManager), typeof(AppUserManager));

            // Configure the database context.
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            // Add the identity service.
            services.AddIdentity<User, Role>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Configure the identity service.
            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireDigit = true;
                options.User.RequireUniqueEmail = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
                options.Lockout.MaxFailedAccessAttempts = 7;
                options.Lockout.AllowedForNewUsers = true;
                options.SignIn.RequireConfirmedEmail = false;
            });

            // Register the Swagger generator.
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "GeoBoard API"
                });

                c.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    BearerFormat = "JWT",
                    Scheme = "bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearer" }
                        },
                        new List<string>()
                    }
                });


                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            // Configure JWT authentication.
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(cfg =>
            {
                cfg.RequireHttpsMetadata = false;
                cfg.SaveToken = true;

                cfg.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidIssuer = Configuration["Tokens:Issuer"],
                    ValidAudience = Configuration["Tokens:Issuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Configuration["Tokens:Key"])),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

            });

            // Setup CORS.
            services.AddCors(o => o.AddPolicy("CorsPolicy", builder =>
            {
                builder
                    .WithOrigins(new[] { "http://localhost", "https://geoboard.app" })
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            }));

            // Setup AutoMapper
            services.AddAutoMapper(typeof(Startup));

            // Add SignalR.
            services.AddSignalR();

            // Add and configure Hangfire.
            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    UsePageLocksOnDequeue = true,
                    DisableGlobalLocks = true
                }));

            // Add the processing server as IHostedService
            services.AddHangfireServer();

            // Add the localisation service.
            services.AddLocalization(options => options.ResourcesPath = "Resources");

            // Add and configure authorization.
            services.AddAuthorization(options =>
            {

                #region User
                options.AddPolicy("app.user.view", policy => policy.RequireClaim(AppClaimTypes.Permission, "app.user.view"));
                options.AddPolicy("app.user.create", policy => policy.RequireClaim(AppClaimTypes.Permission, "app.user.create"));
                options.AddPolicy("app.user.update", policy => policy.RequireClaim(AppClaimTypes.Permission, "app.user.update"));
                options.AddPolicy("app.user.delete", policy => policy.RequireClaim(AppClaimTypes.Permission, "app.user.delete"));
                options.AddPolicy("app.user.lockout", policy => policy.RequireClaim(AppClaimTypes.Permission, "app.user.lockout"));
                #endregion

            });

            // Enable controllers and views.
            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider services)
        {
            // Configure error handling depending on environment.
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Configure the Hangfire dashboard.
            app.UseHangfireDashboard("/hangfire", new DashboardOptions()
            {
                AppPath = env.IsDevelopment() ? "https://localhost:5001" : "https://geoboard.app" 
            });

            // Activate the CorsPolicy
            app.UseCors("CorsPolicy");

            // Enable static files.
            app.UseStaticFiles();

            // Enable routing.
            app.UseRouting();

            // Enable authentication and authorization.
            app.UseAuthentication();
            app.UseAuthorization();

            // Configure Swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "GeoBoard API V1");
            });

            // Setup the endpoints.
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action}/{id?}");
                endpoints.MapHub<HomeHub>("/homehub");
            });

            //SeedDatabase(services).GetAwaiter().GetResult();
        }

        public async Task SeedDatabase(IServiceProvider services)
        {
            using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                #region Countries
                try
                {
                    if (!(await dbContext.Countries.AnyAsync()))
                    {
                        await dbContext.Countries.AddRangeAsync(
                        new Country() { ShortTerm = "Afghanistan", LongTerm = "Transitional Islamic State of Afghanistan", ISOCode = 4, ShortCode = "AF", LongCode = "AFG", Capital = "Ka¢bul", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Albania", LongTerm = "Republic of Albania", ISOCode = 8, ShortCode = "AL", LongCode = "ALB", Capital = "Tirana", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Algeria", LongTerm = "Democratic and Popular Republic of Algeria", ISOCode = 12, ShortCode = "DZ", LongCode = "DZA", Capital = "Algiers", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "American Samoa", LongTerm = "Territory of American Samoa", ISOCode = 16, ShortCode = "AS", LongCode = "ASM", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Andorra", LongTerm = "Principality of Andorra", ISOCode = 20, ShortCode = "AD", LongCode = "AND", Capital = "Andorra la Vella", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Angola", LongTerm = "Republic of Angola", ISOCode = 24, ShortCode = "AO", LongCode = "AGO", Capital = "Luanda", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Anguilla", LongTerm = " ", ISOCode = 660, ShortCode = "AI", LongCode = "AIA", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Antarctica", LongTerm = " ", ISOCode = 10, ShortCode = "AQ", LongCode = "ATA", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Antigua and Barbuda", LongTerm = "Antigua and Barbuda", ISOCode = 28, ShortCode = "AG", LongCode = "ATG", Capital = "St. John's", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Argentina", LongTerm = "Argentine Republic", ISOCode = 32, ShortCode = "AR", LongCode = "ARG", Capital = "Buenos Aires", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Armenia", LongTerm = "Republic of Armenia", ISOCode = 51, ShortCode = "AM", LongCode = "ARM", Capital = "Yerevan", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Aruba", LongTerm = " ", ISOCode = 533, ShortCode = "AW", LongCode = "ABW", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Australia", LongTerm = "Commonwealth of Australia", ISOCode = 36, ShortCode = "AU", LongCode = "AUS", Capital = "Canberra", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Austria", LongTerm = "Republic of Austria", ISOCode = 40, ShortCode = "AT", LongCode = "AUT", Capital = "Vienna", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Azerbaijan", LongTerm = "Azerbaijani Republic", ISOCode = 31, ShortCode = "AZ", LongCode = "AZE", Capital = "Baku", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Bahamas; The", LongTerm = "The Commonwealth of The Bahamas", ISOCode = 44, ShortCode = "BS", LongCode = "BHS", Capital = "Nassau", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Bahrain", LongTerm = "State of Bahrain", ISOCode = 48, ShortCode = "BH", LongCode = "BHR", Capital = "Manama", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Bangladesh", LongTerm = "People's Republic of Bangladesh", ISOCode = 50, ShortCode = "BD", LongCode = "BGD", Capital = "Dhaka", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Barbados", LongTerm = " ", ISOCode = 52, ShortCode = "BB", LongCode = "BRB", Capital = "Bridgetown", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Belarus", LongTerm = "Republic of Belarus", ISOCode = 112, ShortCode = "BY", LongCode = "BLR", Capital = "Minsk", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Belgium", LongTerm = "Kingdom of Belgium", ISOCode = 56, ShortCode = "BE", LongCode = "BEL", Capital = "Brussels", LanguageCode = "nl-NL" },
                        new Country() { ShortTerm = "Belize", LongTerm = " ", ISOCode = 84, ShortCode = "BZ", LongCode = "BLZ", Capital = "Belmopan", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Benin", LongTerm = "Republic of Benin", ISOCode = 204, ShortCode = "BJ", LongCode = "BEN", Capital = "Porto-Novo", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Bermuda", LongTerm = " ", ISOCode = 60, ShortCode = "BM", LongCode = "BMU", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Bhutan", LongTerm = "Kingdom of Bhutan", ISOCode = 64, ShortCode = "BT", LongCode = "BTN", Capital = "Thimphu", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Bolivia", LongTerm = "Republic of Bolivia", ISOCode = 68, ShortCode = "BO", LongCode = "BOL", Capital = "Sucre", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Bosnia and Herzegovina", LongTerm = "Republic of Bosnia and Herzegovina", ISOCode = 70, ShortCode = "BA", LongCode = "BIH", Capital = "Sarajevo", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Botswana", LongTerm = "Republic of Botswana", ISOCode = 72, ShortCode = "BW", LongCode = "BWA", Capital = "Gaborone", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Bouvet Island", LongTerm = " ", ISOCode = 74, ShortCode = "BV", LongCode = "BVT", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Brazil", LongTerm = "Federative Republic of Brazil", ISOCode = 76, ShortCode = "BR", LongCode = "BRA", Capital = "Brasa­lia", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "British Indian Ocean Territory", LongTerm = "British Indian Ocean Territory", ISOCode = 86, ShortCode = "IO", LongCode = "IOT", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Brunei", LongTerm = "Negara Brunei Darussalam", ISOCode = 96, ShortCode = "BN", LongCode = "BRN", Capital = "Bandar seri Begawan", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Bulgaria", LongTerm = "Republic of Bulgaria", ISOCode = 100, ShortCode = "BG", LongCode = "BGR", Capital = "Sofia", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Burkina Faso", LongTerm = " ", ISOCode = 854, ShortCode = "BF", LongCode = "BFA", Capital = "Ouagadougou", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Burundi", LongTerm = "Republic of Burundi", ISOCode = 108, ShortCode = "BI", LongCode = "BDI", Capital = "Bujumbura", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Ca´te d'Ivoire", LongTerm = "Republic of Ca´te d'Ivoire", ISOCode = 384, ShortCode = "CI", LongCode = "CIV", Capital = "Yamoussoukro", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Cambodia", LongTerm = "Kingdom of Cambodia", ISOCode = 116, ShortCode = "KH", LongCode = "KHM", Capital = "Phnom Penh", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Cameroon", LongTerm = "Republic of Cameroon", ISOCode = 120, ShortCode = "CM", LongCode = "CMR", Capital = "Yaoundac", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Canada", LongTerm = " ", ISOCode = 124, ShortCode = "CA", LongCode = "CAN", Capital = "Ottawa", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Cape Verde", LongTerm = "Republic of Cape Verde", ISOCode = 132, ShortCode = "CV", LongCode = "CPV", Capital = "Praia", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Cayman Islands", LongTerm = " ", ISOCode = 136, ShortCode = "KY", LongCode = "CYM", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Central African Republic", LongTerm = "Central African Republic", ISOCode = 140, ShortCode = "CF", LongCode = "CAF", Capital = "Bangui", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Chad", LongTerm = "Republic of Chad", ISOCode = 148, ShortCode = "TD", LongCode = "TCD", Capital = "N'Djamena", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Channel Islands", LongTerm = " ", ISOCode = 0, ShortCode = "GB-CHA", LongCode = "-", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Chile", LongTerm = "Republic of Chile", ISOCode = 152, ShortCode = "CL", LongCode = "CHL", Capital = "Santiago", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "China", LongTerm = "People's Republic of China", ISOCode = 156, ShortCode = "CN", LongCode = "CHN", Capital = "Beijing", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Christmas Island", LongTerm = "Territory of Christmas Island", ISOCode = 162, ShortCode = "CX", LongCode = "CXR", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Cocos (Keeling) Islands", LongTerm = "Territory of Cocos (Keeling) Islands", ISOCode = 166, ShortCode = "CC", LongCode = "CCK", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Colombia", LongTerm = "Republic of Colombia", ISOCode = 170, ShortCode = "CO", LongCode = "COL", Capital = "Santa Fe de Bogota¡", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Comoros", LongTerm = "Federal Islamic Republic of the Comoros", ISOCode = 174, ShortCode = "KM", LongCode = "COM", Capital = "Moroni", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Congo", LongTerm = "Republic of the Congo", ISOCode = 178, ShortCode = "CG", LongCode = "COG", Capital = "Brazzaville", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Congo (DRC)", LongTerm = "Democratic Republic of the Congo", ISOCode = 180, ShortCode = "CD", LongCode = "COD", Capital = "Kinshasa", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Cook Islands", LongTerm = " ", ISOCode = 184, ShortCode = "CK", LongCode = "COK", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Coral Sea Islands", LongTerm = "Coral Sea Islands Territory", ISOCode = 0, ShortCode = "AU", LongCode = "-", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Costa Rica", LongTerm = "Republic of Costa Rica", ISOCode = 188, ShortCode = "CR", LongCode = "CRI", Capital = "San Josac", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Croatia", LongTerm = "Republic of Croatia", ISOCode = 191, ShortCode = "HR", LongCode = "HRV", Capital = "Zagreb", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Cuba", LongTerm = "Republic of Cuba", ISOCode = 192, ShortCode = "CU", LongCode = "CUB", Capital = "Havana", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Cyprus", LongTerm = "Republic of Cyprus", ISOCode = 196, ShortCode = "CY", LongCode = "CYP", Capital = "Nicosia", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Czech Republic", LongTerm = "Czech Republic", ISOCode = 203, ShortCode = "CZ", LongCode = "CZE", Capital = "Prague", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Denmark", LongTerm = "Kingdom of Denmark", ISOCode = 208, ShortCode = "DK", LongCode = "DNK", Capital = "Copenhagen", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Djibouti", LongTerm = "Republic of Djibouti", ISOCode = 262, ShortCode = "DJ", LongCode = "DJI", Capital = "Djibouti", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Dominica", LongTerm = "Commonwealth of Dominica", ISOCode = 212, ShortCode = "DM", LongCode = "DMA", Capital = "Roseau", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Dominican Republic", LongTerm = "Dominican Republic", ISOCode = 214, ShortCode = "DO", LongCode = "DOM", Capital = "Santo Domingo", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Ecuador", LongTerm = "Republic of Ecuador", ISOCode = 218, ShortCode = "EC", LongCode = "ECU", Capital = "Quito", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Egypt", LongTerm = "Arab Republic of Egypt", ISOCode = 818, ShortCode = "EG", LongCode = "EGY", Capital = "Cairo", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "El Salvador", LongTerm = "Republic of El Salvador", ISOCode = 222, ShortCode = "SV", LongCode = "SLV", Capital = "San Salvador", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Equatorial Guinea", LongTerm = "Republic of Equatorial Guinea", ISOCode = 226, ShortCode = "GQ", LongCode = "GNQ", Capital = "Malabo", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Eritrea", LongTerm = "State of Eritrea", ISOCode = 232, ShortCode = "ER", LongCode = "ERI", Capital = "Asmara", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Estonia", LongTerm = "Republic of Estonia", ISOCode = 233, ShortCode = "EE", LongCode = "EST", Capital = "Tallinn", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Ethiopia", LongTerm = "Federal Democratic Republic of Ethiopia", ISOCode = 231, ShortCode = "ET", LongCode = "ETH", Capital = "Addis Ababa", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Falkland Islands (Islas Malvinas)", LongTerm = "Falkland Islands (Islas Malvinas)", ISOCode = 238, ShortCode = "FK", LongCode = "FLK", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Faroe Islands", LongTerm = " ", ISOCode = 234, ShortCode = "FO", LongCode = "FRO", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Fiji Islands", LongTerm = "Republic of the Fiji Islands", ISOCode = 242, ShortCode = "FJ", LongCode = "FJI", Capital = "Suva", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Finland", LongTerm = "Republic of Finland", ISOCode = 246, ShortCode = "FI", LongCode = "FIN", Capital = "Helsinki", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "France", LongTerm = "French Republic", ISOCode = 250, ShortCode = "FR", LongCode = "FRA", Capital = "Paris", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "France; Metropolitan", LongTerm = " ", ISOCode = 249, ShortCode = "FX", LongCode = "FXX", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "French Guiana", LongTerm = "Department of Guiana", ISOCode = 254, ShortCode = "GF", LongCode = "GUF", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "French Polynesia", LongTerm = "Territory of French Polynesia", ISOCode = 258, ShortCode = "PF", LongCode = "PYF", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "French Southern and Antarctic Lands", LongTerm = "Territory of the French Southern and Antarctic Lands", ISOCode = 260, ShortCode = "TF", LongCode = "ATF", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Gabon", LongTerm = "Gabonese Republic", ISOCode = 266, ShortCode = "GA", LongCode = "GAB", Capital = "Libreville", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Gambia; The", LongTerm = "Republic of The Gambia", ISOCode = 270, ShortCode = "GM", LongCode = "GMB", Capital = "Banjul", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Georgia", LongTerm = "Republic of Georgia", ISOCode = 268, ShortCode = "GE", LongCode = "GEO", Capital = "T'bilisi", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Germany", LongTerm = "Federal Republic of Germany", ISOCode = 276, ShortCode = "DE", LongCode = "DEU", Capital = "Berlin", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Ghana", LongTerm = "Republic of Ghana", ISOCode = 288, ShortCode = "GH", LongCode = "GHA", Capital = "Accra", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Gibraltar", LongTerm = " ", ISOCode = 292, ShortCode = "GI", LongCode = "GIB", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Greece", LongTerm = "Hellenic Republic", ISOCode = 300, ShortCode = "GR", LongCode = "GRC", Capital = "Athens", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Greenland", LongTerm = " ", ISOCode = 304, ShortCode = "GL", LongCode = "GRL", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Grenada", LongTerm = " ", ISOCode = 308, ShortCode = "GD", LongCode = "GRD", Capital = "St. George's", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Guadeloupe", LongTerm = "Department of Guadeloupe", ISOCode = 312, ShortCode = "GP", LongCode = "GLP", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Guam", LongTerm = "Territory of Guam", ISOCode = 316, ShortCode = "GU", LongCode = "GUM", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Guatemala", LongTerm = "Republic of Guatemala", ISOCode = 320, ShortCode = "GT", LongCode = "GTM", Capital = "Guatemala", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Guernsey", LongTerm = "Balliwick of Guernsey", ISOCode = 0, ShortCode = "GB-GSY", LongCode = "-", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Guinea", LongTerm = "Republic of Guinea", ISOCode = 324, ShortCode = "GN", LongCode = "GIN", Capital = "Conakry", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Guinea-Bissau", LongTerm = "Republic of Guinea-Bissau", ISOCode = 624, ShortCode = "GW", LongCode = "GNB", Capital = "Bissau", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Guyana", LongTerm = "Cooperative Republic of Guyana", ISOCode = 328, ShortCode = "GY", LongCode = "GUY", Capital = "Georgetown", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Haiti", LongTerm = "Republic of Haiti", ISOCode = 332, ShortCode = "HT", LongCode = "HTI", Capital = "Port-au-Prince", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Heard Island and McDonald Islands", LongTerm = "Territory of Heard Island and McDonald Islands", ISOCode = 334, ShortCode = "HM", LongCode = "HMD", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Honduras", LongTerm = "Republic of Honduras", ISOCode = 340, ShortCode = "HN", LongCode = "HND", Capital = "Tegucigalpa", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Hong Kong SAR", LongTerm = "Hong Kong Special Administrative Region", ISOCode = 344, ShortCode = "HK", LongCode = "HKG", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Hungary", LongTerm = "Republic of Hungary", ISOCode = 348, ShortCode = "HU", LongCode = "HUN", Capital = "Budapest", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Iceland", LongTerm = "Republic of Iceland", ISOCode = 352, ShortCode = "IS", LongCode = "ISL", Capital = "Reykjava­k", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "India", LongTerm = "Republic of India", ISOCode = 356, ShortCode = "IN", LongCode = "IND", Capital = "New Delhi", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Indonesia", LongTerm = "Republic of Indonesia", ISOCode = 360, ShortCode = "ID", LongCode = "IDN", Capital = "Jakarta", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Iran", LongTerm = "Islamic Republic of Iran", ISOCode = 364, ShortCode = "IR", LongCode = "IRN", Capital = "Tehran", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Iraq", LongTerm = "Republic of Iraq", ISOCode = 368, ShortCode = "IQ", LongCode = "IRQ", Capital = "Baghdad", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Ireland", LongTerm = "Republic of Ireland", ISOCode = 372, ShortCode = "IE", LongCode = "IRL", Capital = "Dublin", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Israel", LongTerm = "State of Israel", ISOCode = 376, ShortCode = "IL", LongCode = "ISR", Capital = "Jerusalem", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Italy", LongTerm = "Italian Republic", ISOCode = 380, ShortCode = "IT", LongCode = "ITA", Capital = "Rome", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Jamaica", LongTerm = " ", ISOCode = 388, ShortCode = "JM", LongCode = "JAM", Capital = "Kingston", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Japan", LongTerm = " ", ISOCode = 392, ShortCode = "JP", LongCode = "JPN", Capital = "Tokyo", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Jersey", LongTerm = "Balliwick of Jersey", ISOCode = 0, ShortCode = "GB-JSY", LongCode = "-", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Jordan", LongTerm = "Hashemite Kingdom of Jordan", ISOCode = 400, ShortCode = "JO", LongCode = "JOR", Capital = "Amman", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Kazakhstan", LongTerm = "Republic of Kazakhstan", ISOCode = 398, ShortCode = "KZ", LongCode = "KAZ", Capital = "Astana", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Kenya", LongTerm = "Republic of Kenya", ISOCode = 404, ShortCode = "KE", LongCode = "KEN", Capital = "Nairobi", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Kiribati", LongTerm = "Republic of Kiribati", ISOCode = 296, ShortCode = "KI", LongCode = "KIR", Capital = "Bairiki", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Korea", LongTerm = "Republic of Korea", ISOCode = 410, ShortCode = "KR", LongCode = "KOR", Capital = "Seoul", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Kuwait", LongTerm = "State of Kuwait", ISOCode = 414, ShortCode = "KW", LongCode = "KWT", Capital = "Kuwait", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Kyrgyzstan", LongTerm = "Kyrgyz Republic", ISOCode = 417, ShortCode = "KG", LongCode = "KGZ", Capital = "Bishkek", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Laos", LongTerm = "Lao People's Democratic Republic", ISOCode = 418, ShortCode = "LA", LongCode = "LAO", Capital = "Vientiane", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Latvia", LongTerm = "Republic of Latvia", ISOCode = 428, ShortCode = "LV", LongCode = "LVA", Capital = "Riga", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Lebanon", LongTerm = "Republic of Lebanon", ISOCode = 422, ShortCode = "LB", LongCode = "LBN", Capital = "Beirut", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Lesotho", LongTerm = "Kingdom of Lesotho", ISOCode = 426, ShortCode = "LS", LongCode = "LSO", Capital = "Maseru", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Liberia", LongTerm = "Republic of Liberia", ISOCode = 430, ShortCode = "LR", LongCode = "LBR", Capital = "Monrovia", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Libya", LongTerm = "Socialist People's Libyan Arab Jamahiriya", ISOCode = 434, ShortCode = "LY", LongCode = "LBY", Capital = "Tripoli", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Liechtenstein", LongTerm = "Principality of Liechtenstein", ISOCode = 438, ShortCode = "LI", LongCode = "LIE", Capital = "Vaduz", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Lithuania", LongTerm = "Republic of Lithuania", ISOCode = 440, ShortCode = "LT", LongCode = "LTU", Capital = "Vilnius", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Luxembourg", LongTerm = "Grand Duchy of Luxembourg", ISOCode = 442, ShortCode = "LU", LongCode = "LUX", Capital = "Luxembourg", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Macau SAR", LongTerm = "Macau Special Administrative Region", ISOCode = 446, ShortCode = "MO", LongCode = "MAC", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Macedonia; Former Yugoslav Republic of", LongTerm = "The Former Yugoslav Republic of Macedonia", ISOCode = 807, ShortCode = "MK", LongCode = "MKD", Capital = "Skopje", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Madagascar", LongTerm = "Republic of Madagascar", ISOCode = 450, ShortCode = "MG", LongCode = "MDG", Capital = "Antananarivo", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Malawi", LongTerm = "Republic of Malawi", ISOCode = 454, ShortCode = "MW", LongCode = "MWI", Capital = "Lilongwe", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Malaysia", LongTerm = "Federation of Malaysia", ISOCode = 458, ShortCode = "MY", LongCode = "MYS", Capital = "Kuala Lumpur", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Maldives", LongTerm = "Republic of Maldives", ISOCode = 462, ShortCode = "MV", LongCode = "MDV", Capital = "Malac", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Mali", LongTerm = "Republic of Mali", ISOCode = 466, ShortCode = "ML", LongCode = "MLI", Capital = "Bamako", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Malta", LongTerm = "Republic of Malta", ISOCode = 470, ShortCode = "MT", LongCode = "MLT", Capital = "Valletta", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Man; Isle of", LongTerm = " ", ISOCode = 0, ShortCode = "GB-IOM", LongCode = "-", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Marshall Islands", LongTerm = "Republic of the Marshall Islands", ISOCode = 584, ShortCode = "MH", LongCode = "MHL", Capital = "Majuro", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Martinique", LongTerm = "Department of Martinique", ISOCode = 474, ShortCode = "MQ", LongCode = "MTQ", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Mauritania", LongTerm = "Islamic Republic of Mauritania", ISOCode = 478, ShortCode = "MR", LongCode = "MRT", Capital = "Nouakchott", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Mauritius", LongTerm = "Republic of Mauritius", ISOCode = 480, ShortCode = "MU", LongCode = "MUS", Capital = "Port Louis", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Mayotte", LongTerm = "Territorial Collectivity of Mayotte", ISOCode = 175, ShortCode = "YT", LongCode = "MYT", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Mexico", LongTerm = "United Mexican States", ISOCode = 484, ShortCode = "MX", LongCode = "MEX", Capital = "Mexico City", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Micronesia", LongTerm = "Federated States of Micronesia", ISOCode = 583, ShortCode = "FM", LongCode = "FSM", Capital = "Palikir", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Moldova", LongTerm = "Republic of Moldova", ISOCode = 498, ShortCode = "MD", LongCode = "MDA", Capital = "ChiÂºina£u", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Monaco", LongTerm = "Principality of Monaco", ISOCode = 492, ShortCode = "MC", LongCode = "MCO", Capital = "Monaco", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Mongolia", LongTerm = " ", ISOCode = 496, ShortCode = "MN", LongCode = "MNG", Capital = "Ulaanbaatar", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Montserrat", LongTerm = " ", ISOCode = 500, ShortCode = "MS", LongCode = "MSR", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Morocco", LongTerm = "Kingdom of Morocco", ISOCode = 504, ShortCode = "MA", LongCode = "MAR", Capital = "Rabat", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Mozambique", LongTerm = "Republic of Mozambique", ISOCode = 508, ShortCode = "MZ", LongCode = "MOZ", Capital = "Maputo", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Myanmar", LongTerm = "Union of Myanmar", ISOCode = 104, ShortCode = "MM", LongCode = "MMR", Capital = "Yangon (Rangoon)", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Namibia", LongTerm = "Republic of Namibia", ISOCode = 516, ShortCode = "NA", LongCode = "NAM", Capital = "Windhoek", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Nauru", LongTerm = "Republic of Nauru", ISOCode = 520, ShortCode = "NR", LongCode = "NRU", Capital = "Yaren", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Nepal", LongTerm = "Kingdom of Nepal", ISOCode = 524, ShortCode = "NP", LongCode = "NPL", Capital = "Kathmandu", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Netherlands Antilles", LongTerm = " ", ISOCode = 530, ShortCode = "AN", LongCode = "ANT", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Netherlands; The", LongTerm = "Kingdom of the Netherlands", ISOCode = 528, ShortCode = "NL", LongCode = "NLD", Capital = "Amsterdam", LanguageCode = "nl-NL" },
                        new Country() { ShortTerm = "New Caledonia", LongTerm = "Territory of New Caledonia and Dependencies", ISOCode = 540, ShortCode = "NC", LongCode = "NCL", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "New Zealand", LongTerm = " ", ISOCode = 554, ShortCode = "NZ", LongCode = "NZL", Capital = "Wellington", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Nicaragua", LongTerm = "Republic of Nicaragua", ISOCode = 558, ShortCode = "NI", LongCode = "NIC", Capital = "Managua", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Niger", LongTerm = "Republic of Niger", ISOCode = 562, ShortCode = "NE", LongCode = "NER", Capital = "Niamey", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Nigeria", LongTerm = "Federal Republic of Nigeria", ISOCode = 566, ShortCode = "NG", LongCode = "NGA", Capital = "Abuja", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Niue", LongTerm = " ", ISOCode = 570, ShortCode = "NU", LongCode = "NIU", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Norfolk Island", LongTerm = "Territory of Norfolk Island", ISOCode = 574, ShortCode = "NF", LongCode = "NFK", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "North Korea", LongTerm = "Democratic People's Republic of Korea", ISOCode = 408, ShortCode = "KP", LongCode = "PRK", Capital = "Pyongyang", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Northern Mariana Islands", LongTerm = "Commonwealth of the Northern Mariana Islands", ISOCode = 580, ShortCode = "MP", LongCode = "MNP", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Norway", LongTerm = "Kingdom of Norway", ISOCode = 578, ShortCode = "NO", LongCode = "NOR", Capital = "Oslo", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Oman", LongTerm = "Sultanate of Oman", ISOCode = 512, ShortCode = "OM", LongCode = "OMN", Capital = "Masqaa¾", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Pakistan", LongTerm = "Islamic Republic of Pakistan", ISOCode = 586, ShortCode = "PK", LongCode = "PAK", Capital = "Isla¢ma¢ba¢d", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Palau", LongTerm = "Republic of Palau", ISOCode = 585, ShortCode = "PW", LongCode = "PLW", Capital = "Koror", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Panama", LongTerm = "Republic of Panama", ISOCode = 591, ShortCode = "PA", LongCode = "PAN", Capital = "Panama City", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Papua New Guinea", LongTerm = "Independent State of Papua New Guinea", ISOCode = 598, ShortCode = "PG", LongCode = "PNG", Capital = "Port Moresby", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Paraguay", LongTerm = "Republic of Paraguay", ISOCode = 600, ShortCode = "PY", LongCode = "PRY", Capital = "Asuncia³n", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Peru", LongTerm = "Republic of Peru", ISOCode = 604, ShortCode = "PE", LongCode = "PER", Capital = "Lima", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Philippines", LongTerm = "Republic of the Philippines", ISOCode = 608, ShortCode = "PH", LongCode = "PHL", Capital = "Manila", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Pitcairn Islands", LongTerm = "Pitcairn; Henderson; Ducie; and Oeno Islands", ISOCode = 612, ShortCode = "PN", LongCode = "PCN", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Poland", LongTerm = "Republic of Poland", ISOCode = 616, ShortCode = "PL", LongCode = "POL", Capital = "Warsaw", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Portugal", LongTerm = "Portuguese Republic", ISOCode = 620, ShortCode = "PT", LongCode = "PRT", Capital = "Lisbon", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Puerto Rico", LongTerm = "Commonwealth of Puerto Rico", ISOCode = 630, ShortCode = "PR", LongCode = "PRI", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Qatar", LongTerm = "State of Qatar", ISOCode = 634, ShortCode = "QA", LongCode = "QAT", Capital = "Doha", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Reunion", LongTerm = "Department of Reunion", ISOCode = 638, ShortCode = "RE", LongCode = "REU", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Romania", LongTerm = " ", ISOCode = 642, ShortCode = "RO", LongCode = "ROM", Capital = "Bucharest", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Russia", LongTerm = "Russian Federation", ISOCode = 643, ShortCode = "RU", LongCode = "RUS", Capital = "Moscow", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Rwanda", LongTerm = "Rwandese Republic", ISOCode = 646, ShortCode = "RW", LongCode = "RWA", Capital = "Kigali", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Sa£o Tomac and Pra­ncipe", LongTerm = "Democratic Republic of Sa£o Tomac and Pra­ncipe", ISOCode = 678, ShortCode = "ST", LongCode = "STP", Capital = "Sa£o Tomac", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Samoa", LongTerm = "Independent State of Samoa", ISOCode = 882, ShortCode = "WS", LongCode = "WSM", Capital = "Apia", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "San Marino", LongTerm = "Republic of San Marino", ISOCode = 674, ShortCode = "SM", LongCode = "SMR", Capital = "San Marino", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Saudi Arabia", LongTerm = "Kingdom of Saudi Arabia", ISOCode = 682, ShortCode = "SA", LongCode = "SAU", Capital = "Riyadh", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Senegal", LongTerm = "Republic of Senegal", ISOCode = 686, ShortCode = "SN", LongCode = "SEN", Capital = "Dakar", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Serbia and Montenegro", LongTerm = "Serbia and Montenegro", ISOCode = 891, ShortCode = "YU", LongCode = "YUG", Capital = "Belgrade", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Seychelles", LongTerm = "Republic of Seychelles", ISOCode = 690, ShortCode = "SC", LongCode = "SYC", Capital = "Victoria", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Sierra Leone", LongTerm = "Republic of Sierra Leone", ISOCode = 694, ShortCode = "SL", LongCode = "SLE", Capital = "Freetown", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Singapore", LongTerm = "Republic of Singapore", ISOCode = 702, ShortCode = "SG", LongCode = "SGP", Capital = "Singapore", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Slovakia", LongTerm = "Slovak Republic", ISOCode = 703, ShortCode = "SK", LongCode = "SVK", Capital = "Bratislava", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Slovenia", LongTerm = "Republic of Slovenia", ISOCode = 705, ShortCode = "SI", LongCode = "SVN", Capital = "Ljubljana", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Solomon Islands", LongTerm = " ", ISOCode = 90, ShortCode = "SB", LongCode = "SLB", Capital = "Honiara", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Somalia", LongTerm = "Somali Democratic Republic", ISOCode = 706, ShortCode = "SO", LongCode = "SOM", Capital = "Mogadishu", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "South Africa", LongTerm = "Republic of South Africa", ISOCode = 710, ShortCode = "ZA", LongCode = "ZAF", Capital = "Pretoria", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "South Georgia and the South Sandwich Islands", LongTerm = "South Georgia and the South Sandwich Islands", ISOCode = 239, ShortCode = "GS", LongCode = "SGS", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Spain", LongTerm = "Kingdom of Spain", ISOCode = 724, ShortCode = "ES", LongCode = "ESP", Capital = "Madrid", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Sri Lanka", LongTerm = "Democratic Socialist Republic of Sri Lanka", ISOCode = 144, ShortCode = "LK", LongCode = "LKA", Capital = "Sri Jayawardenepura", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "St. Helena", LongTerm = "Saint Helena", ISOCode = 654, ShortCode = "SH", LongCode = "SHN", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "St. Kitts and Nevis", LongTerm = "Federation of Saint Kitts and Nevis", ISOCode = 659, ShortCode = "KN", LongCode = "KNA", Capital = "Basseterre", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "St. Lucia", LongTerm = "Saint Lucia", ISOCode = 662, ShortCode = "LC", LongCode = "LCA", Capital = "Castries", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "St. Pierre and Miquelon", LongTerm = "Territorial Collectivity of Saint Pierre and Miquelon", ISOCode = 666, ShortCode = "PM", LongCode = "SPM", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "St. Vincent and the Grenadines", LongTerm = "Saint Vincent and the Grenadines", ISOCode = 670, ShortCode = "VC", LongCode = "VCT", Capital = "Kingstown", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Sudan", LongTerm = "Republic of the Sudan", ISOCode = 736, ShortCode = "SD", LongCode = "SDN", Capital = "Khartoum", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Suriname", LongTerm = "Republic of Suriname", ISOCode = 740, ShortCode = "SR", LongCode = "SUR", Capital = "Paramaribo", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Svalbard and Jan Mayen", LongTerm = " ", ISOCode = 744, ShortCode = "SJ", LongCode = "SJM", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Swaziland", LongTerm = "Kingdom of Swaziland", ISOCode = 748, ShortCode = "SZ", LongCode = "SWZ", Capital = "Mbabane", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Sweden", LongTerm = "Kingdom of Sweden", ISOCode = 752, ShortCode = "SE", LongCode = "SWE", Capital = "Stockholm", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Switzerland", LongTerm = "Swiss Confederation", ISOCode = 756, ShortCode = "CH", LongCode = "CHE", Capital = "Bern", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Syria", LongTerm = "Syrian Arab Republic", ISOCode = 760, ShortCode = "SY", LongCode = "SYR", Capital = "Damascus", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Tajikistan", LongTerm = "Republic of Tajikistan", ISOCode = 762, ShortCode = "TJ", LongCode = "TJK", Capital = "Dushanbe", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Tanzania", LongTerm = "United Republic of Tanzania", ISOCode = 834, ShortCode = "TZ", LongCode = "TZA", Capital = "Dar es Salaam", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Thailand", LongTerm = "Kingdom of Thailand", ISOCode = 764, ShortCode = "TH", LongCode = "THA", Capital = "Bangkok", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Timor-Leste", LongTerm = "Timor-Leste", ISOCode = 626, ShortCode = "TP", LongCode = "TMP", Capital = "Dili", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Togo", LongTerm = "Togolese Republic", ISOCode = 768, ShortCode = "TG", LongCode = "TGO", Capital = "Lomac", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Tokelau", LongTerm = " ", ISOCode = 772, ShortCode = "TK", LongCode = "TKL", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Tonga", LongTerm = "Kingdom of Tonga", ISOCode = 776, ShortCode = "TO", LongCode = "TON", Capital = "Nuku'alofa", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Trinidad and Tobago", LongTerm = "Republic of Trinidad and Tobago", ISOCode = 780, ShortCode = "TT", LongCode = "TTO", Capital = "Port-of-Spain", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Tunisia", LongTerm = "Republic of Tunisia", ISOCode = 788, ShortCode = "TN", LongCode = "TUN", Capital = "Tunis", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Turkey", LongTerm = "Republic of Turkey", ISOCode = 792, ShortCode = "TR", LongCode = "TUR", Capital = "Ankara", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Turkmenistan", LongTerm = "Republic of Turkmenistan", ISOCode = 795, ShortCode = "TM", LongCode = "TKM", Capital = "Ashgabat", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Turks and Caicos Islands", LongTerm = " ", ISOCode = 796, ShortCode = "TC", LongCode = "TCA", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Tuvalu", LongTerm = " ", ISOCode = 798, ShortCode = "TV", LongCode = "TUV", Capital = "Funafuti", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "U.S. Minor Outlying Islands", LongTerm = "United States Minor Outlying Islands", ISOCode = 581, ShortCode = "UM", LongCode = "UMI", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Uganda", LongTerm = "Republic of Uganda", ISOCode = 800, ShortCode = "UG", LongCode = "UGA", Capital = "Kampala", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Ukraine", LongTerm = " ", ISOCode = 804, ShortCode = "UA", LongCode = "UKR", Capital = "Kyiv", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "United Arab Emirates", LongTerm = " ", ISOCode = 784, ShortCode = "AE", LongCode = "ARE", Capital = "Abu Dhabi", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "United Kingdom", LongTerm = "United Kingdom of Great Britain and Northern Ireland", ISOCode = 826, ShortCode = "GB", LongCode = "GBR", Capital = "London", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "United States", LongTerm = "United States of America", ISOCode = 840, ShortCode = "US", LongCode = "USA", Capital = "Washington; D.C.", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Uruguay", LongTerm = "Oriental Republic of Uruguay", ISOCode = 858, ShortCode = "UY", LongCode = "URY", Capital = "Montevideo", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Uzbekistan", LongTerm = "Republic of Uzbekistan", ISOCode = 860, ShortCode = "UZ", LongCode = "UZB", Capital = "Toshkent", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Vanuatu", LongTerm = "Republic of Vanuatu", ISOCode = 548, ShortCode = "VU", LongCode = "VUT", Capital = "Port-Vila", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Vatican City", LongTerm = "State of Vatican City", ISOCode = 336, ShortCode = "VA", LongCode = "VAT", Capital = "Vatican City", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Venezuela", LongTerm = "Bolivarian Republic of Venezuela", ISOCode = 862, ShortCode = "VE", LongCode = "VEN", Capital = "Caracas", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Viet Nam", LongTerm = "Socialist Republic of Vietnam", ISOCode = 704, ShortCode = "VN", LongCode = "VNM", Capital = "Hanoi", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Virgin Islands", LongTerm = "Virgin Islands of the United States", ISOCode = 850, ShortCode = "VI", LongCode = "VIR", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Virgin Islands; British", LongTerm = " ", ISOCode = 92, ShortCode = "VG", LongCode = "VGB", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Wallis and Futuna", LongTerm = "Territory of the Wallis and Futuna Islands", ISOCode = 876, ShortCode = "WF", LongCode = "WLF", Capital = " ", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Yemen", LongTerm = "Republic of Yemen", ISOCode = 887, ShortCode = "YE", LongCode = "YEM", Capital = "Sana'a", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Zambia", LongTerm = "Republic of Zambia", ISOCode = 894, ShortCode = "ZM", LongCode = "ZMB", Capital = "Lusaka", LanguageCode = "en-US" },
                        new Country() { ShortTerm = "Zimbabwe", LongTerm = "Republic of Zimbabwe", ISOCode = 716, ShortCode = "ZW", LongCode = "ZWE", Capital = "Harare", LanguageCode = "en-US" });
                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (DbUpdateException ex)
                {
                    throw ex;
                }
                #endregion
                string[] userRoles = User.Roles;
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

                Func<Role, IEnumerable<Claim>, Task> addIdentityRoleClaims = async (Role identityRole, IEnumerable<Claim> claims) => {
                    var identityRoleClaims = await roleManager.GetClaimsAsync(identityRole);

                    foreach (var claim in claims)
                    {
                        var existingClaim = identityRoleClaims.FirstOrDefault(x => x.Type.Equals(claim.Type) && x.Value.Equals(claim.Value));
                        if (null == existingClaim)
                        {
                            await roleManager.AddClaimAsync(identityRole, claim);
                        }
                    }
                };

                foreach (var role in userRoles)
                {
                    var identityRole = new Role(role);
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(identityRole);
                    }

                    identityRole = await roleManager.FindByNameAsync(role);

                    foreach (var roleClaim in await roleManager.GetClaimsAsync(identityRole))
                    {
                        await roleManager.RemoveClaimAsync(identityRole, roleClaim);
                    }

                    if (identityRole.Name == "Administrator")
                    {
                        var identityRoleClaims = new Claim[] {
                        new Claim(AppClaimTypes.Permission, "app.user.view"),
                        new Claim(AppClaimTypes.Permission, "app.user.create"),
                        new Claim(AppClaimTypes.Permission, "app.user.update"),
                        new Claim(AppClaimTypes.Permission, "app.user.delete"),
                        new Claim(AppClaimTypes.Permission, "app.user.lockout"),
                        };

                        await addIdentityRoleClaims(identityRole, identityRoleClaims);
                    }

                    if (identityRole.Name == "User")
                    {
                        var identityRoleClaims = new Claim[] {
                        new Claim(AppClaimTypes.Permission, "app.user.view"),
                        new Claim(AppClaimTypes.Permission, "app.user.update"),
                        new Claim(AppClaimTypes.Permission, "app.home.view"),
                        new Claim(AppClaimTypes.Permission, "app.game.view"),
                        };

                        await addIdentityRoleClaims(identityRole, identityRoleClaims);
                    }
                }

                #region Users
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

                var user = await userManager.FindByNameAsync("luuk");
                if (user == null)
                {
                    var email = "luuk.wuijster@hotmail.com";
                    var userToInsert = new User
                    {
                        Email = email,
                        UserName = "luuk",
                        EmailConfirmed = true,
                        Person = new Person
                        {
                            Id = new Guid("07e35556-54f2-4975-a563-417eb5fbfa9f"),
                            LastName = "Wuijster",
                            FirstName = "Luuk",
                            Country = await dbContext.Countries.FirstAsync(x => x.LanguageCode == "nl-NL"),
                        },
                        Settings = new UserSetting
                        {
                            Language = dbContext.Countries.FirstOrDefault(x => x.LongCode.Equals("NLD")),
                        },
                        Id = new Guid("07e35556-54f2-4975-a563-417eb5fbfa7f")
                    };
                    var result = await userManager.CreateAsync(userToInsert, "Wachtwoord12");

                    //Add roles
                    await userManager.AddToRoleAsync(await userManager.FindByNameAsync("luuk"), "User");
                }

                var user2 = await userManager.FindByNameAsync("matthijs");
                if (user2 == null)
                {
                    var email = "m.sixma@outlook.com";
                    var userToInsert = new User
                    {
                        Email = email,
                        UserName = "matthijs",
                        EmailConfirmed = true,
                        Person = new Person
                        {
                            Id = new Guid("BCFF11D5-BAB4-4D2E-9C16-08218BD8D201"),
                            LastName = "Matthijs",
                            FirstName = "Sixma",
                            Country = await dbContext.Countries.FirstAsync(x => x.LanguageCode == "nl-NL"),
                        },
                        Settings = new UserSetting
                        {
                            Language = dbContext.Countries.FirstOrDefault(x => x.LongCode.Equals("NLD")),
                        },
                        Id = new Guid("ABCF11D5-BAB4-4D2E-9C16-08218BD8D102")
                    };
                    var result = await userManager.CreateAsync(userToInsert, "Wachtwoord12");

                    //Add roles
                    await userManager.AddToRoleAsync(await userManager.FindByNameAsync("matthijs"), "User");
                }

                await dbContext.SaveChangesAsync();
                #endregion

            }
        }
    }
}
