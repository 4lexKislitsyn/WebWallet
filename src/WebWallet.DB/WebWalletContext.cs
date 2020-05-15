using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using WebWallet.DB.Entities;

namespace WebWallet.DB
{
    public class WebWalletContext : DbContext
    {
        /// <summary>
        /// Create an instance of <see cref="WebWalletContext"/>
        /// </summary>
        /// <param name="options">Параметры инициализации</param>
        public WebWalletContext(DbContextOptions<WebWalletContext> options) : base(options)
        {
            Database.Migrate();
        }

        public DbSet<CurrencyBalance> Currencies { get; set; }
        public DbSet<MoneyTransfer> Transfers { get; set; }
        public DbSet<UserWallet> Wallets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CurrencyBalance>()
                .HasKey(balance => new { balance.Currency, balance.WalletId });

            modelBuilder.Entity<MoneyTransfer>()
                .HasOne(x => x.FromCurrency)
                .WithMany(x => x.FromTransfers)
                .HasForeignKey(x => new { x.FromCurrencyId, x.UserWalletId });

            modelBuilder.Entity<MoneyTransfer>()
                .HasOne(x => x.ToCurrency)
                .WithMany(x => x.ToTransfers)
                .HasForeignKey(x => new { x.ToCurrencyId, x.UserWalletId });
        }
    }
}
