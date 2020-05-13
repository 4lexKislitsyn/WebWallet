using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebWallet.DB.Entities;

namespace WebWallet.DB
{
    public class InMemoryRepository : IWebWalletRepository
    {
        private IDictionary<string, ICollection<CurrencyBalance>> _balances = new Dictionary<string, ICollection<CurrencyBalance>>();
        private ICollection<MoneyTransfer> _transfers = new List<MoneyTransfer>();
        private bool _hasSaveChangesErorr = false;

        /// <inheritdoc/>
        public bool AddEntity<T>(T entity)
        {
            switch (entity)
            {
                case MoneyTransfer transfer:
                    _transfers.Add(transfer);
                    break;
                case CurrencyBalance balance:
                    if (_balances.TryGetValue(balance.WalletId ?? string.Empty, out var balances))
                    {
                        if (balances.Any(x => x.Currency == balance.Currency))
                        {
                            _hasSaveChangesErorr = true;
                        }
                        else
                        {
                            balances.Add(balance);
                        }
                    }
                    else
                    {
                        _hasSaveChangesErorr = true;
                    }
                    break;
                case UserWallet wallet when !string.IsNullOrWhiteSpace(wallet.Id) && _balances.ContainsKey(wallet.Id):
                    _hasSaveChangesErorr = true;
                    break;
                case UserWallet wallet:
                    wallet.Id ??= Guid.NewGuid().ToString();
                    _balances[wallet.Id] = new List<CurrencyBalance>();
                    break;
            }
            return true;
        }
        /// <inheritdoc/>
        public void Dispose()
        {

        }
        /// <inheritdoc/>
        public UserWallet FindWallet(string id)
        {
            return _balances.ContainsKey(id)
                ? new UserWallet { Id = id }
                : null;
        }
        /// <inheritdoc/>
        public UserWallet FindWalletWithCurrencies(string id)
        {
            return !_balances.TryGetValue(id, out var balances)
                ? null
                : new UserWallet
                {
                    Id = id,
                    Balances = balances
                };
        }
        /// <inheritdoc/>
        public Task SaveAsync()
        {
            if (_hasSaveChangesErorr)
            {
                throw new Exception();
            }
            return Task.CompletedTask;
        }
    }
}
