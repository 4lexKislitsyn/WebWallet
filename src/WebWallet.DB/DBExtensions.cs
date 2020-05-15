using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace WebWallet.DB
{
    public static class DBExtensions
    {
        public const string DatabaseConenctionName = "web-wallet";
        /// <summary>
        /// If environment is development in-memory repository will be added else MySql EF context.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="isDevelopment"></param>
        /// <returns></returns>
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
        {
            if (isDevelopment)
            {
                services.AddSingleton<IWebWalletRepository, InMemoryRepository>();
            }
            else
            {
                var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
                services.AddScoped<IWebWalletRepository, DBRepository>();
                var connectionString = configuration.GetConnectionString(DatabaseConenctionName);
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new ArgumentOutOfRangeException($"Connection string is empty. Please provide connection string with name '{DatabaseConenctionName}'");
                }
                services.AddDbContext<WebWalletContext>(optionsAction =>
                {
                    optionsAction.UseMySql(connectionString,mySqlOptionsAction => mySqlOptionsAction.MigrationsAssembly(assemblyName));
                });
            }
            return services;
        }
    }
}
