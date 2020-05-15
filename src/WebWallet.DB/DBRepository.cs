using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WebWallet.DB.Entities;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace WebWallet.DB
{
    public class DBRepository : IWebWalletRepository
    {
        private readonly WebWalletContext _context;
        /// <summary>
        /// Create an instance of <see cref="DBRepository"/>.
        /// </summary>
        /// <param name="context"></param>
        public DBRepository(WebWalletContext context)
        {
            _context = context;
        }
        /// <inheritdoc/>
        public bool AddEntity<T>(T entity)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public void Dispose() => _context.Dispose();
        /// <inheritdoc/>
        public bool DoesWalletContainsCurrency(string walletId, string currencyId)
            => _context.Currencies.Any(x => x.Currency == currencyId && x.WalletId == walletId);
        /// <inheritdoc/>
        public bool DoesWalletExist(string id)
            => _context.Wallets.Any(x => x.Id == id);
        /// <inheritdoc/>
        public CurrencyBalance FindCurrency(string walletId, string currency)
        {
            return _context.Currencies.Find(currency, walletId);
        }
        /// <inheritdoc/>
        public MoneyTransfer FindTransfer(string id)
            => _context.Transfers.Find(id);
        /// <inheritdoc/>
        public MoneyTransfer FindTransferWithCurrencies(string id)
        {
            return _context.Transfers
                .Include(x => x.FromCurrency)
                .Include(x => x.ToCurrency)
                .FirstOrDefault(x => x.Id == id);
        }
        /// <inheritdoc/>
        public UserWallet FindWallet(string id)
        {
            return _context.Wallets.Find(id);
        }
        /// <inheritdoc/>
        public UserWallet FindWalletWithCurrencies(string id)
        {
            return _context.Wallets.Include(x => x.Balances).FirstOrDefault(x => x.Id == id);
        }
        /// <inheritdoc/>
        public Task SaveAsync() => _context.SaveChangesAsync();
    }
}
