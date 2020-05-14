using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebWallet.API.ExternalAPI.Interfaces;
using WebWallet.API.v1.Controllers;
using WebWallet.API.v1.Models;
using WebWallet.DB;
using WebWallet.DB.Entities;

namespace UnitTests
{
    [TestFixture]
    public class TransfersControllerTests
    {
        private readonly string _emptyGuid = Guid.Empty.ToString();
        private readonly Randomizer _generator = new Randomizer(DateTime.UtcNow.Minute);
        private TransferController _transfersController;

        private ICollection<MoneyTransfer> _transferWithoutId = new List<MoneyTransfer>();

        [SetUp]
        public void CreateController() => InitTest();
        [TearDown]
        public void ClearTransferCollection() => _transferWithoutId.Clear();
        /// <summary>
        /// balance overflow to <see cref="double.PositiveInfinity"/>.
        /// </summary>
        /// <returns></returns>
        [Ignore("This error has not yet been processed.")]
        [Test]
        public async Task BalanceOverflow()
        {
            var repo = new Mock<IWebWalletRepository>();
            InitTest(repo.Object);

            var walletGuid = _generator.NextGuid().ToString();

            var currencyBalance = new CurrencyBalance
            {
                Balance = double.MaxValue,
                Currency = _generator.GetString(3),
            };

            repo.Setup(x => x.FindTransferWithCurrencies(_emptyGuid))
                .Returns(new MoneyTransfer
                {
                    Id = _emptyGuid,
                    Amount = double.MaxValue,
                    UserWalletId = walletGuid,
                    State = TransferState.Active,
                    ToCurrencyId = currencyBalance.Currency,
                    ToCurrency = currencyBalance,
                });

            var rateService = Mock.Of<ICurrencyRateService>(MockBehavior.Strict);

            var confirmation = new TransferConfirmation { WalletId = walletGuid };

            var result = await _transfersController.ConfirmTransfer(_emptyGuid, confirmation, rateService);
            result.IsResult<OkResult>(HttpStatusCode.OK);
            Assert.Positive(currencyBalance.Balance);
            Assert.AreNotEqual(double.PositiveInfinity, currencyBalance.Balance);
        }

        /// <summary>
        /// Create transfer to existing currency.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task CreateTransfer([Values] TransferType type)
        {
            var repo = new Mock<IWebWalletRepository>(MockBehavior.Strict);
            InitTest(repo.Object);

            var transfer = CreateTransferInfo(type);

            var rate = type == TransferType.Transfer 
                ? _generator.NextDecimal((decimal)double.Epsilon, 10)
                : 0;

            var rateServiceMoq = new Mock<ICurrencyRateService>(MockBehavior.Strict);
            if (type == TransferType.Transfer)
            {
                rateServiceMoq.Setup(x => x.GetCurrencyRate(transfer.From, transfer.To))
                    .Returns(Task.FromResult<decimal?>(rate));
            }

            MoneyTransfer moneyTransfer = null;
            AddIdBehaviour(repo, entity =>
            {
                Assert.True(string.IsNullOrWhiteSpace(entity.Id));
                Assert.AreEqual(transfer.From, entity.FromCurrencyId);
                Assert.AreEqual(transfer.To, entity.ToCurrencyId);
                Assert.AreEqual(transfer.Amount, entity.Amount);
                Assert.AreEqual(rate, entity.ActualCurrencyRate);
                moneyTransfer = entity;
            });

            AddWallets(repo, _emptyGuid);

            repo.Setup(x => x.DoesWalletContainsCurrency(_emptyGuid, transfer.To))
                .Returns(true);
            AddCurrency(repo, transfer);

            var result = await _transfersController.CreateTransfer(transfer, rateServiceMoq.Object);
            var content = result.IsCreatedWithContent<MoneyTransfer>(locationEndsWith: Guid.Parse(moneyTransfer.Id).ToString("N"));
            Assert.AreEqual(moneyTransfer.Id, content.Id);
            Assert.IsFalse(_transferWithoutId.Any(), "Save changes before transfer created.");
        }

        /// <summary>
        /// Trying to make transfer for unknown wallet.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [Test]
        public async Task CreateTransferForNonExistentWallet([Values] TransferType type)
        {
            var repo = new Mock<IWebWalletRepository>(MockBehavior.Strict);
            repo.Setup(x => x.FindWallet(It.IsAny<string>()))
                .Returns((UserWallet)null);
            repo.Setup(x => x.DoesWalletExist(It.IsAny<string>()))
                .Returns(false);

            InitTest(repo.Object);

            var transferInfo = CreateTransferInfo(type, 1);
            var result = await _transfersController.CreateTransfer(transferInfo, Mock.Of<ICurrencyRateService>(MockBehavior.Strict));
            result.IsResult<NotFoundObjectResult>(HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Create transfer from nonexistent currency balance.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [Test]
        [TestCase(TransferType.Transfer)]
        [TestCase(TransferType.Withdraw)]
        public async Task CreateTransferForNonExistentCurrency(TransferType type)
        {
            var repo = new Mock<IWebWalletRepository>(MockBehavior.Loose);
            AddIdBehaviour(repo);
            InitTest(repo.Object);

            var transferInfo = CreateTransferInfo(type);
            repo.Setup(x => x.DoesWalletContainsCurrency(It.IsNotNull<string>(), transferInfo.From))
                .Returns(false);

            var rateService = new Mock<ICurrencyRateService>();
            rateService.Setup(x => x.GetCurrencyRate(It.IsNotNull<string>(), It.IsNotNull<string>()))
                .Returns(Task.FromResult<decimal?>(_generator.NextDecimal(1, 100)));

            var result = await _transfersController.CreateTransfer(transferInfo, rateService.Object);
            result.IsResult<NotFoundObjectResult>(HttpStatusCode.NotFound);
        }
        /// <summary>
        /// Create transfer from not enough balance.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [Test]
        [TestCase(TransferType.Transfer)]
        [TestCase(TransferType.Withdraw)]
        public async Task CreateTransferNotEnoughMoney(TransferType type)
        {
            var transferInfo = CreateTransferInfo(type, 1);

            var repo = new Mock<IWebWalletRepository>(MockBehavior.Strict);
            InitTest(repo.Object);

            AddIdBehaviour(repo);
            AddWallets(repo, transferInfo.WalletId.ToString());

            repo.Setup(x => x.DoesWalletContainsCurrency(It.IsNotNull<string>(), transferInfo.From))
                .Returns(true);
            repo.Setup(x => x.FindCurrency(_emptyGuid, transferInfo.From))
                .Returns<string, string>((walletId, currency) => new CurrencyBalance
                {
                    Balance = 0,
                    Currency = currency,
                    WalletId = walletId,
                })
                .Verifiable();

            var rateService = new Mock<ICurrencyRateService>();
            rateService.Setup(x => x.GetCurrencyRate(It.IsNotNull<string>(), It.IsNotNull<string>()))
                .Returns(Task.FromResult<decimal?>(_generator.NextDecimal(1, 100)));

            var result = await _transfersController.CreateTransfer(transferInfo, rateService.Object);
            result.IsResult<ObjectResult>(HttpStatusCode.PaymentRequired);
            repo.Verify();
        }

        [Test]
        [TestCase(TransferType.Transfer)]
        [TestCase(TransferType.Replenish)]
        public async Task CreateTransferToNonexistentCurrency(TransferType type)
        {
            var repo = new Mock<IWebWalletRepository>(MockBehavior.Strict);
            AddIdBehaviour(repo);
            InitTest(repo.Object);

            var transferInfo = CreateTransferInfo(type, 1);

            AddWallets(repo, transferInfo.WalletId.ToString());
            AddCurrency(repo, transferInfo);

            repo.Setup(x => x.DoesWalletContainsCurrency(transferInfo.WalletId.ToString(), transferInfo.To))
                .Returns(false);

            repo.Setup(x => x.AddEntity(It.IsNotNull<CurrencyBalance>()))
                .Callback<CurrencyBalance>((currencyBalance) =>
                {
                    Assert.AreEqual(0, currencyBalance.Balance);
                    Assert.AreEqual(transferInfo.To, currencyBalance.Currency);
                    Assert.AreEqual(transferInfo.WalletId.ToString(), currencyBalance.WalletId);
                })
                .Returns(true)
                .Verifiable();

            var rateService = new Mock<ICurrencyRateService>();
            rateService.Setup(x => x.GetCurrencyRate(It.IsNotNull<string>(), It.IsNotNull<string>()))
                .Returns(Task.FromResult<decimal?>(_generator.NextDecimal(1, 100)));

            var result = await _transfersController.CreateTransfer(transferInfo, rateService.Object);
            result.IsCreatedWithContent((Func<MoneyTransfer, string>)null);
            repo.Verify();
        }

        /// <summary>
        /// Create transfer to unknown currency for rate service.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [Test]
        public async Task CreateCurrencyTransferWithoutRate()
        {
            var transferInfo = CreateTransferInfo(TransferType.Transfer);
            var repo = new Mock<IWebWalletRepository>(MockBehavior.Strict);
            AddWallets(repo, transferInfo.WalletId.ToString());
            InitTest(repo.Object);
            AddCurrency(repo, transferInfo);

            var result = await _transfersController.CreateTransfer(transferInfo, Mock.Of<ICurrencyRateService>(MockBehavior.Loose));
            result.IsResult<BadRequestObjectResult>(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Delete transfer belongs to another wallet.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task DeleteAnotherWalletTransfer()
        {
            var repo = new Mock<IWebWalletRepository>(MockBehavior.Strict);
            InitTest(repo.Object);
            repo.Setup(x => x.FindTransfer(It.IsNotNull<string>()))
                .Returns<string>(id => new MoneyTransfer
                {
                    UserWalletId = _generator.NextGuid().ToString(),
                    Id = id
                });

            var result = await _transfersController.DeleteTransfer(_generator.NextGuid().ToString(), new DeleteTransferRequest
            {
                WalletId = _emptyGuid
            });
            result.IsResult<ForbidResult>();
        }

        /// <summary>
        /// Delete nonexistent transfer.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task DeleteNonexistentTransfer()
        {
            var repo = new Mock<IWebWalletRepository>(MockBehavior.Strict);
            InitTest(repo.Object);
            repo.Setup(x => x.FindTransfer(It.IsNotNull<string>()))
                .Returns((MoneyTransfer)null);

            var request = new DeleteTransferRequest
            {
                WalletId = _emptyGuid
            };
            var result = await _transfersController.DeleteTransfer(_generator.NextGuid().ToString(), request);
            result.IsResult<NotFoundResult>(HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Delete completed transfer.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task DeleteCompletedTransfer()
        {
            var repo = new Mock<IWebWalletRepository>(MockBehavior.Strict);
            InitTest(repo.Object);
            repo.Setup(x => x.FindTransfer(It.IsNotNull<string>()))
                .Returns<string>(id => new MoneyTransfer
                {
                    Id = id,
                    State = TransferState.Completed
                });

            var request = new DeleteTransferRequest
            {
                WalletId = _emptyGuid
            };
            var result = await _transfersController.DeleteTransfer(_generator.NextGuid().ToString(), request);
            result.IsResult<NotFoundResult>(HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Delete removed transfer.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task DeleteRemovedTransfer()
        {
            var repo = new Mock<IWebWalletRepository>(MockBehavior.Strict);
            InitTest(repo.Object);

            var walletId = _emptyGuid;

            repo.Setup(x => x.FindTransfer(It.IsNotNull<string>()))
                .Returns<string>(id => new MoneyTransfer
                {
                    Id = id,
                    State = TransferState.Deleted,
                    UserWalletId = walletId
                });

            var request = new DeleteTransferRequest
            {
                WalletId = walletId
            };
            var result = await _transfersController.DeleteTransfer(_generator.NextGuid().ToString(), request);
            result.IsResult<OkResult>(HttpStatusCode.OK);
        }

        /// <summary>
        /// Delete removed transfer.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task DeleteActiveTransfer()
        {
            var repo = new Mock<IWebWalletRepository>(MockBehavior.Strict);
            InitTest(repo.Object);

            var walletId = _emptyGuid;

            var transferId = _generator.NextGuid().ToString();
            var transfer = new MoneyTransfer
            {
                Id = transferId,
                State = TransferState.Active,
                UserWalletId = walletId
            };

            var lastSavedState = transfer.State;

            repo.Setup(x => x.FindTransfer(It.IsNotNull<string>()))
                .Returns(transfer);
            repo.Setup(x => x.SaveAsync())
                .Callback(() =>
                {
                    lastSavedState = transfer.State;
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            var request = new DeleteTransferRequest
            {
                WalletId = walletId
            };
            var result = await _transfersController.DeleteTransfer(transferId, request);
            result.IsResult<OkResult>(HttpStatusCode.OK);
            Assert.AreEqual(TransferState.Deleted, lastSavedState);
        }
        /*
         *  TODO:
         *  + Create transfer from/to - 200;
         *  + Create transfer from - 200;
         *  + Create transfer to - 200;
         *  + Create transfer nonexistent wallet - 404/400;
         *  + Create transfer from nonexistent currency balance - 404/400;
         *  + Create transfer from not enough balance - 402;
         *  + Create transfer to nonexistent currency balance - 201 balance should be created;
         *  + Create transfer to unknown currency - 400;
         *  - Confirm transfer to nonexistent currency balance - 500 or create balance;
         *  - Confirm deleted transfer - 404;
         *  - Confirm completed transfer - 200;
         *  - Confirm active transfer - 200;
         *  - Confirm transfer when amount of transfer grater than balance - 402;
         *  - Confirm transfer belongs to another wallet - 403;
         *  + Delete transfer belongs to another wallet - 403;
         *  + Delete nonexistent transfer - 404;
         *  + Delete completed transfer - 404;
         *  + Delete deleted earlier transfer - 200; 
         */

        private void InitTest(IWebWalletRepository repository = null, IUrlHelper urlHelper = null)
        {
            _transfersController = new TransferController(repository ?? new InMemoryRepository())
            {
                Url = urlHelper ?? Mock.Of<IUrlHelper>()
            };
        }
        
        //private T IsResult<T>(IActionResult result, HttpStatusCode? status = null) where T : class
        //{
        //    Assert.IsInstanceOf<T>(result);
        //    var convertedResult = result as T;
        //    if (status.HasValue)
        //    {
        //        int? statusCode = null;
        //        switch (convertedResult)
        //        {
        //            case ObjectResult objectResult:
        //                statusCode = objectResult.StatusCode;
        //                break;
        //            case StatusCodeResult codeResult:
        //                statusCode = codeResult.StatusCode;
        //                break;
        //            default:
        //                Assert.Fail("Cannot check passed status code.");
        //                break;
        //        }
        //        Assert.AreEqual((int)status.Value, statusCode);
        //    }
        //    return convertedResult;
        //}

        //private T HasContent<T>(ObjectResult objectResult)
        //{
        //    Assert.IsNotNull(objectResult.Value);
        //    Assert.IsInstanceOf<T>(objectResult.Value);
        //    return (T)objectResult.Value;
        //}

        //private T IsCreatedWithContent<T>(IActionResult result, string locationEndsWith)
        //{
        //    var createdResult = IsResult<CreatedResult>(result, HttpStatusCode.Created);
        //    Assert.IsFalse(string.IsNullOrWhiteSpace(createdResult.Location));
        //    if (!string.IsNullOrWhiteSpace(locationEndsWith))
        //    {
        //        Assert.IsTrue(createdResult.Location.EndsWith(locationEndsWith));
        //    }
        //    var content = HasContent<T>(createdResult);
        //    return content;
        //}

        private CreateTransfer CreateTransferInfo(TransferType type, double? amount = null, string walletGuid = null)
        {
            return new CreateTransfer
            {
                From = type != TransferType.Replenish ? _generator.GetString(3) : null,
                To = type != TransferType.Withdraw ? _generator.GetString(3) : null,
                Amount = amount ?? _generator.NextDouble(double.Epsilon, 100),
                WalletId = walletGuid ?? _emptyGuid,
            };
        }

        private void AddIdBehaviour(Mock<IWebWalletRepository> mock, Action<MoneyTransfer> callback = null)
        {
            mock.Setup(x => x.AddEntity(It.IsNotNull<MoneyTransfer>()))
                .Callback<MoneyTransfer>(entity =>
                {
                    callback?.Invoke(entity);
                    lock (_transferWithoutId)
                    {
                        _transferWithoutId.Add(entity);
                    }
                })
                .Returns(true);
            mock.Setup(x => x.SaveAsync())
                .Callback(() =>
                {
                    lock (_transferWithoutId)
                    {
                        foreach (var item in _transferWithoutId)
                        {
                            item.Id = _generator.NextGuid().ToString();
                        }
                        _transferWithoutId.Clear();
                    }
                })
                .Returns(Task.CompletedTask);
        }

        private void AddWallets(Mock<IWebWalletRepository> mock, params string[] id)
        {
            mock.Setup(x => x.DoesWalletExist(It.IsNotNull<string>()))
                .Returns<string>(x => id.Contains(x));
        }

        private void AddCurrency(Mock<IWebWalletRepository> repo, CreateTransfer transfer)
        {
            repo.Setup(x => x.FindCurrency(transfer.WalletId.ToString(), transfer.From))
                .Returns<string, string>((wallet, currency) => new CurrencyBalance
                {
                    WalletId = wallet,
                    Currency = currency,
                    Balance = transfer.Amount + 1,
                });
        }

        public enum TransferType
        {
            /// <summary>
            /// Transfer money from one currency to another.
            /// </summary>
            Transfer,
            /// <summary>
            /// Get money from currency balance.
            /// </summary>
            Withdraw,
            /// <summary>
            /// Give money to currency balance.
            /// </summary>
            Replenish
        }
    }
}
