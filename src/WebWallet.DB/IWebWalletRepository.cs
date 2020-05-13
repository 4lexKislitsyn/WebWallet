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
    }
}
