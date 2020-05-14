using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WebWallet.API;
using WebWallet.API.v1.Controllers;
using WebWallet.DB;
using WebWallet.DB.Entities;

namespace UnitTests
{
    [TestFixture]
    public class WalletsControllerTests
    {
        private readonly Randomizer _generator = new Randomizer(DateTime.UtcNow.Minute);
        private WalletController _walletController;
        private IWebWalletRepository _repository;

        [SetUp]
        public void CreateController()
        {
            _repository = new InMemoryRepository();
            _walletController = new WalletController(_repository, Mock.Of<IMapper>());
        }
        /// <summary>
        /// Check create wallet method.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task CreateWallet()
        {
            var urlMock = new Mock<IUrlHelper>(MockBehavior.Strict);
            urlMock.Setup(x => x.RouteUrl(It.Is<UrlRouteContext>(x=> x.RouteName == ApiConstants.WalletRoute)))
                .Returns("api/wallet");
            _walletController.Url = urlMock.Object;

            var result = await _walletController.CreateWallet();
            result.IsCreatedWithContent<UserWallet>(x=> x.Id.Replace("-", string.Empty));
        }

        /// <summary>
        /// Get nonexistent wallet.
        /// </summary>
        /// <returns></returns>
        [Test]
        public void GetNonExistentWallet()
        {
            var result = _walletController.GetWalletInfo(Guid.Empty);
            Assert.IsInstanceOf<NotFoundResult>(result);
            Assert.AreEqual((int)HttpStatusCode.NotFound, ((NotFoundResult)result).StatusCode);
        }

        /// <summary>
        /// Get existent wallet.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task GetExistentWallet()
        {
            var emptyGuid = Guid.Empty.ToString();
            _repository.AddEntity(new UserWallet
            {
                Id = emptyGuid,
            });
            var currency = new CurrencyBalance
            {
                WalletId = emptyGuid,
                Currency = _generator.GetString(3),
                Balance = _generator.NextDouble(0, double.MaxValue)
            };
            _repository.AddEntity(currency);
            await _repository.SaveAsync();

            var result = _walletController.GetWalletInfo(Guid.Empty);
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.AreEqual((int)HttpStatusCode.OK, okResult.StatusCode);
            Assert.IsInstanceOf<UserWallet>(okResult.Value);
            var wallet = okResult.Value as UserWallet;
            Assert.AreEqual(emptyGuid, wallet.Id);
            Assert.AreEqual(1, wallet.Balances.Count());
            Assert.AreEqual(currency.Currency, wallet.Balances.First().Currency);
            Assert.AreEqual(currency.Balance, wallet.Balances.First().Balance);
        }
    }
}
