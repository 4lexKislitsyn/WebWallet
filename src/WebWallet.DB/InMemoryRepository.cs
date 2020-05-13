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
                    {
                        _transfers.Add(transfer);
                        if (!_balances.TryGetValue(transfer.UserWalletId ?? string.Empty, out var balances))
                        {
                            _hasSaveChangesErorr = true;
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(transfer.FromCurrencyId) && balances.Any(x => x.Currency == transfer.FromCurrencyId))
                            {
                                _hasSaveChangesErorr = true;
                            }
                        }
                    }
                    break;
                case CurrencyBalance balance:
                    {
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
        public bool DoesWalletContainsCurrency(string walletId, string currencyId)
        {
            return _balances.Any(x => x.Key == walletId && x.Value.Any(z => z.Currency == currencyId));
        }

        /// <inheritdoc/>
        public bool DoesWalletExist(string id)
        {
            return _balances.ContainsKey(id);
        }
        /// <inheritdoc/>
        public MoneyTransfer FindTransfer(string id)
        {
            return _transfers.FirstOrDefault(x => x.Id == id);
        }
        /// <inheritdoc/>
        public MoneyTransfer FindTransferWithCurrencies(string id)
        {
            var transfer = FindTransfer(id);
            var currencies = _balances[transfer.UserWalletId];
            transfer.FromCurrency = currencies.FirstOrDefault(x => x.Currency == transfer.FromCurrencyId);
            transfer.FromCurrency = currencies.FirstOrDefault(x => x.Currency == transfer.ToCurrencyId);
            return transfer;
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
