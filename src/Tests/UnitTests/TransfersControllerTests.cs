using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebWallet.API.AutomapperProfiles;
using WebWallet.API.ExternalAPI.Interfaces;
using WebWallet.API.v1.Controllers;
using WebWallet.API.v1.DTO;
using WebWallet.API.v1.Models;
using WebWallet.DB;
using WebWallet.DB.Entities;

namespace UnitTests
{
    [TestFixture]
    [Order(1)]
    public class TransfersControllerTests
    {
        private readonly string _emptyGuid = Guid.Empty.ToString();
        private readonly Randomizer _generator = new Randomizer(DateTime.UtcNow.Minute);
        private readonly IMapper _mapper;
        private TransferController _transfersController;

        private ICollection<MoneyTransfer> _transferWithoutId = new List<MoneyTransfer>();

        public TransfersControllerTests()
        {
            _mapper = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<EntityToModelProfile>();
                cfg.AddProfile<ModelToEntityProfile>();
            }).CreateMapper();
        }

        [SetUp]
        public void CreateController() => InitController();
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
                    WalletId = walletGuid,
                    State = TransferState.Active,
                    ToCurrencyId = currencyBalance.Currency,
                    ToCurrency = currencyBalance,
                });

            var rateService = Mock.Of<ICurrencyRateService>(MockBehavior.Strict);

            var confirmation = new TransferActionRequest { WalletId = walletGuid };

            InitController(repo.Object, rateService: rateService);
            var result = await _transfersController.ConfirmTransfer(_emptyGuid, confirmation);
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

            var transfer = CreateTransferInfo(type);

            var rate = type == TransferType.Transfer 
                ? _generator.NextDecimal((decimal)double.Epsilon, 10)
                : (decimal?)null;

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
                if (rate.HasValue)
                {
                    Assert.AreEqual((double)rate.Value, entity.ActualCurrencyRate);
                }
                else
                {
                    Assert.IsNull(entity.ActualCurrencyRate);
                }
                moneyTransfer = entity;
            });

            AddWallets(repo, _emptyGuid);

            repo.Setup(x => x.DoesWalletContainsCurrency(_emptyGuid, transfer.To))
                .Returns(true);
            AddCurrency(repo, transfer);

            InitController(repo.Object, rateService: rateServiceMoq.Object);
            var result = await _transfersController.CreateTransfer(transfer);
            var content = result.IsCreatedWithContent<TransferInfo>(locationEndsWith: moneyTransfer.Id);
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

            var transferInfo = CreateTransferInfo(type, 1);

            InitController(repo.Object, rateService: Mock.Of<ICurrencyRateService>(MockBehavior.Strict));
            var result = await _transfersController.CreateTransfer(transferInfo);
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

            var transferInfo = CreateTransferInfo(type);
            repo.Setup(x => x.DoesWalletContainsCurrency(It.IsNotNull<string>(), transferInfo.From))
                .Returns(false);

            var rateService = new Mock<ICurrencyRateService>();
            rateService.Setup(x => x.GetCurrencyRate(It.IsNotNull<string>(), It.IsNotNull<string>()))
                .Returns(Task.FromResult<decimal?>(_generator.NextDecimal(1, 100)));

            InitController(repo.Object, rateService: rateService.Object);
            var result = await _transfersController.CreateTransfer(transferInfo);
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

            InitController(repo.Object, rateService: rateService.Object);
            var result = await _transfersController.CreateTransfer(transferInfo);
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

            InitController(repo.Object, rateService: rateService.Object);
            var result = await _transfersController.CreateTransfer(transferInfo);
            result.IsCreatedWithContent<TransferInfo>(x => x.Id);
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
            AddCurrency(repo, transferInfo);

            InitController(repo.Object, rateService: Mock.Of<ICurrencyRateService>(MockBehavior.Loose));
            var result = await _transfersController.CreateTransfer(transferInfo);
            result.IsResult<BadRequestObjectResult>(HttpStatusCode.BadRequest);
        }
        /// <summary>
        /// Check exception handling when rate service is unavailable.
        /// </summary>
        /// <returns></returns>
        [Test]
        public void CreateTransferRateServiceUnavailable()
        {
            var transferInfo = CreateTransferInfo(TransferType.Transfer);
            var repo = new Mock<IWebWalletRepository>(MockBehavior.Strict);
            AddWallets(repo, transferInfo.WalletId.ToString());
            AddCurrency(repo, transferInfo);

            var rateService = new Mock<ICurrencyRateService>();
            rateService.Setup(x => x.GetCurrencyRate(It.IsAny<string>(), It.IsAny<string>()))
                .Throws<Exception>();
            IActionResult result = null;

            InitController(repo.Object, rateService: rateService.Object);
            Assert.DoesNotThrowAsync(async () =>
            {
                result = await _transfersController.CreateTransfer(transferInfo);
            }, "Action shouldn't throw exception if rate service is unavailable.");
            var objectResult = result.IsResult<ObjectResult>(HttpStatusCode.ServiceUnavailable);
            objectResult.HasContent<ErrorModel>();
        }

        /// <summary>
        /// Delete transfer belongs to another wallet.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task DeleteAnotherWalletTransfer()
        {
            var repo = new Mock<IWebWalletRepository>(MockBehavior.Strict);
            repo.Setup(x => x.FindTransfer(It.IsNotNull<string>()))
                .Returns<string>(id => new MoneyTransfer
                {
                    WalletId = _generator.NextGuid().ToString(),
                    Id = id
                });

            InitController(repo.Object);
            var result = await _transfersController.DeleteTransfer(_generator.NextGuid().ToString(), new TransferActionRequest
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
            repo.Setup(x => x.FindTransfer(It.IsNotNull<string>()))
                .Returns((MoneyTransfer)null);

            var request = new TransferActionRequest
            {
                WalletId = _emptyGuid
            };
            InitController(repo.Object);
            var result = await _transfersController.DeleteTransfer(_generator.NextGuid().ToString(), request);
            var notFoundResult = result.IsResult<NotFoundObjectResult>(HttpStatusCode.NotFound);
            notFoundResult.HasContent<ErrorModel>();
        }

        /// <summary>
        /// Delete completed transfer.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task DeleteCompletedTransfer()
        {
            var repo = new Mock<IWebWalletRepository>(MockBehavior.Strict);
            repo.Setup(x => x.FindTransfer(It.IsNotNull<string>()))
                .Returns<string>(id => new MoneyTransfer
                {
                    Id = id,
                    State = TransferState.Completed
                });

            InitController(repo.Object);
            var request = new TransferActionRequest
            {
                WalletId = _emptyGuid
            };
            var result = await _transfersController.DeleteTransfer(_generator.NextGuid().ToString(), request);
            var notFoundResult = result.IsResult<NotFoundObjectResult>(HttpStatusCode.NotFound);
            notFoundResult.HasContent<ErrorModel>();
        }

        /// <summary>
        /// Delete removed transfer.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task DeleteRemovedTransfer()
        {
            var repo = new Mock<IWebWalletRepository>(MockBehavior.Strict);

            var walletId = _emptyGuid;

            repo.Setup(x => x.FindTransfer(It.IsNotNull<string>()))
                .Returns<string>(id => new MoneyTransfer
                {
                    Id = id,
                    State = TransferState.Deleted,
                    WalletId = walletId
                });

            var request = new TransferActionRequest
            {
                WalletId = walletId
            };
            InitController(repo.Object);
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

            var walletId = _emptyGuid;

            var transferId = _generator.NextGuid().ToString();
            var transfer = new MoneyTransfer
            {
                Id = transferId,
                State = TransferState.Active,
                WalletId = walletId
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

            var request = new TransferActionRequest
            {
                WalletId = walletId
            };
            InitController(repo.Object);
            var result = await _transfersController.DeleteTransfer(transferId, request);
            result.IsResult<OkResult>(HttpStatusCode.OK);
            Assert.AreEqual(TransferState.Deleted, lastSavedState);
        }
        /*
         *  TODO:
         *  - Confirm transfer to nonexistent currency balance - 500 or create balance;
         *  - Confirm deleted transfer - 404;
         *  - Confirm completed transfer - 200;
         *  - Confirm active transfer - 200;
         *  - Confirm transfer when amount of transfer grater than balance - 402;
         *  - Confirm transfer belongs to another wallet - 403;
         */

        private void InitController(IWebWalletRepository repository = null, IUrlHelper urlHelper = null, ICurrencyRateService rateService = null)
        {
            _transfersController = new TransferController(repository ?? new InMemoryRepository(), _mapper, rateService ?? Mock.Of<ICurrencyRateService>(), Mock.Of<ILogger<TransferController>>())
            {
                Url = urlHelper ?? Mock.Of<IUrlHelper>()
            };
        }

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
