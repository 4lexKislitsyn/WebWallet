using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WebWallet.DB.Entities;

namespace WebWallet.DB
{
    public interface IWebWalletRepository : IDisposable
    {
        /// <summary>
        /// Add entity to database.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        bool AddEntity<T>(T entity);
        /// <summary>
        /// Save changes.
        /// </summary>
        /// <returns></returns>
        public Task SaveAsync();
        /// <summary>
        /// Find wallet with passed identifier.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        UserWallet FindWallet(string id);
        /// <summary>
        /// Find wallet with passed identifier and include all currency balances.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        UserWallet FindWalletWithCurrencies(string id);
        /// <summary>
        /// Find transfer by identifier.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        MoneyTransfer FindTransfer(string id);
        /// <summary>
        /// Find transfer by identifier.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        MoneyTransfer FindTransferWithCurrencies(string id);
        /// <summary>
        /// Check that wallet exists.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        bool DoesWalletExist(string id);
        /// <summary>
        /// Check that wallet contains currency balance.
        /// </summary>
        /// <param name="walletId"></param>
        /// <param name="currencyId"></param>
        /// <returns></returns>
        bool DoesWalletContainsCurrency(string walletId, string currencyId);
        /// <summary>
        /// Find wallet currency balance.
        /// </summary>
        /// <param name="emptyGuid"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        CurrencyBalance FindCurrency(string walletId, string currency);
    }
}
