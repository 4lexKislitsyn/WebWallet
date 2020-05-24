using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebWallet.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
            .ConfigureAppConfiguration((context, config) =>
            {
                var isProduction = context.HostingEnvironment.IsProduction();
                config.AddJsonFile(System.IO.Path.Combine(context.HostingEnvironment.ContentRootPath, "..", "WebWallet.DB", "Connections.json"), optional: isProduction, reloadOnChange: true);
                config.AddJsonFile("Connections.json", optional: !isProduction);
            });
    }
}
