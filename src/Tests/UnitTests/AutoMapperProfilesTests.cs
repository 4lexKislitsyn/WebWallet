using AutoMapper;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebWallet.API.AutomapperProfiles;
using WebWallet.API.v1.DTO;
using WebWallet.DB.Entities;

namespace UnitTests
{
    [TestFixture]
    [Order(0)]
    public class AutoMapperProfilesTests
    {
        private readonly IMapper _modelToEntitytMapper;
        private readonly IMapper _entityToModelMapper;
        private readonly Randomizer _generator = new Randomizer(DateTime.UtcNow.Minute);

        public AutoMapperProfilesTests()
        {
            _entityToModelMapper = new MapperConfiguration(cfg => cfg.AddProfile<EntityToModelProfile>())
                .CreateMapper();
            _modelToEntitytMapper = new MapperConfiguration(cfg => cfg.AddProfile<ModelToEntityProfile>())
                .CreateMapper();
        }

        /// <summary>
        /// Map database entity of wallet to DTO.
        /// </summary>
        [Test]
        public void WalletToModel()
        {
            var balances = new List<CurrencyBalance>();
            var wallet = new UserWallet
            {
                Id = Guid.Empty.ToString(),
                Balances = balances
            };

            var model = _entityToModelMapper.Map<WalletInfo>(wallet);

            Assert.AreEqual(wallet.Id, model.Id);
            Assert.AreEqual(0, model.Balances.Count());

            balances.Add(GenerateBalance(wallet));
            model = _entityToModelMapper.Map<WalletInfo>(wallet);

            Assert.AreEqual(wallet.Id, model.Id);
            Assert.AreEqual(1, model.Balances.Count());

            // 0 balance should be ignored
            balances.Add(GenerateBalance(wallet, balance: 0));
            model = _entityToModelMapper.Map<WalletInfo>(wallet);

            Assert.AreEqual(wallet.Id, model.Id);
            Assert.AreEqual(1, model.Balances.Count());
        }
        /// <summary>
        /// Map database entity of currency balance to DTO.
        /// </summary>
        [Test]
        public void CurrencyBalanceToModel([Random(5)] double balance)
        {
            var currencyBalance = GenerateBalance(balance: balance);

            var model = _entityToModelMapper.Map<BalanceInfo>(currencyBalance);

            Assert.AreEqual(currencyBalance.Currency, model.Currency);
            Assert.AreEqual(balance, model.Balance);
        }

        /// <summary>
        /// Map all transfer types to model.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="state"></param>
        [Test]
        public void TransferToModel([Values] TransfersControllerTests.TransferType type, [Values] TransferState state)
        {
            var fromCurrency = GenerateBalance();
            var toCurrency = GenerateBalance();
            var transfer = new MoneyTransfer
            {
                Id = _generator.NextGuid().ToString(),
                Amount = _generator.NextDouble(),
                State = state,
            };

            if (type != TransfersControllerTests.TransferType.Replenish)
            {
                transfer.ToCurrency = toCurrency;
                transfer.ToCurrencyId = toCurrency.Currency;
            }
            if (type != TransfersControllerTests.TransferType.Withdraw)
            {
                transfer.FromCurrency = fromCurrency;
                transfer.FromCurrencyId = fromCurrency.Currency;
            }

            var model = _entityToModelMapper.Map<TransferInfo>(transfer);

            Assert.AreEqual(transfer.Id, model.Id);
            Assert.AreEqual(transfer.Amount, model.Amount);
            Assert.AreEqual(transfer.State.ToString(), model.State);

            if (transfer.FromCurrency != null)
            {
                Assert.AreEqual(transfer.FromCurrencyId, model.From);
            }
            else
            {
                Assert.IsNull(model.From);
            }
            
            if (transfer.ToCurrency != null)
            {
                Assert.AreEqual(transfer.ToCurrencyId, model.To);
            }
            else
            {
                Assert.IsNull(model.To);
            }
            
            if (transfer.ActualCurrencyRate > 0)
            {
                Assert.AreEqual(transfer.ActualCurrencyRate, model.Rate);
            }
            else
            {
                Assert.IsNull(model.Rate);
            }
        }


        private CurrencyBalance GenerateBalance(UserWallet userWallet, string currency = null, double? balance = null)
        {
            return new CurrencyBalance
            {
                Balance = balance ?? _generator.NextDouble(double.Epsilon, 10000),
                Wallet = userWallet,
                WalletId = userWallet.Id,
                Currency = currency ?? _generator.GetString(3)
            };
        }

        private CurrencyBalance GenerateBalance(string currency = null, double? balance = null)
        {
            return new CurrencyBalance
            {
                Balance = balance ?? _generator.NextDouble(double.Epsilon, 10000),
                WalletId = _generator.NextGuid().ToString(),
                Currency = currency ?? _generator.GetString(3)
            };
        }
    }
}
