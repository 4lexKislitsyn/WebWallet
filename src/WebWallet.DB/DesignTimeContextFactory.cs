using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WebWallet.DB
{
    public class DesignTimeContextFactory : IDesignTimeDbContextFactory<WebWalletContext>
    {
        public WebWalletContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(Path.GetFullPath(@"../WebWallet.API/appsettings.json"), optional: false)
                .Build();
            var builder = new DbContextOptionsBuilder<WebWalletContext>()
                .UseMySql(configuration.GetConnectionString(DBExtensions.DatabaseConenctionName));
            return new WebWalletContext(builder.Options, null);
        }
    }
}
