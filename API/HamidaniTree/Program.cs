using HamidaniTree.Model;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HamidaniTree
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                IWebHost host = CreateWebHostBuilder(args).Build();
                using (var scope = host.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    try
                    {
                        using (var context = services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext())
                        {
                            //create db and appply migrations..
                            await context.Database.MigrateAsync();
                            if (!File.Exists("__Seed.txt"))
                            {
                                await new SeedData(services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext()).Seed();
                                File.Create("__Seed.txt");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        var logger = services.GetRequiredService<ILogger<Program>>();
                        logger.LogError(ex, "An error occurred while seeding the database.");
                    }
                    finally
                    {
                    }
                }

                host.Run();
            }
            catch (Exception)
            {
                throw;
            }
            //CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
          Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder =>
        {
            
        });

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
           WebHost.CreateDefaultBuilder(args)
               .UseStartup<Startup>();
    }
}