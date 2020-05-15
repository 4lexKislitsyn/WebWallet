using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using WebWallet.API.ExternalAPI;

namespace UnitTests
{
    [TestFixture]
    [Order(2)]
    public class ECBCurrencyRateServiceTests
    {
        public static readonly string[] Currencies = GetCurrencies().ToArray();

        public static readonly Randomizer Generator = new Randomizer(DateTime.UtcNow.Minute);
        /// <summary>
        /// Get random elements from <see cref="Currencies"/>.
        /// </summary>
        public static IEnumerable<string> RandomCurrencies
        {
            get
            {
                const int size = 5;
                var startIndex = Generator.Next(0, Currencies.Length - size);
                return Currencies.Skip(startIndex).Take(size);
            }
        }

        /// <summary>
        /// Check that rate will be calculated for known currencies.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        [Test]
        public async Task AllCurrenciesRateCalculated([ValueSource(nameof(RandomCurrencies))] string from,
            [ValueSource(nameof(RandomCurrencies))] string to)
        {
            var service = new ECBCurrencyRateService();
            var rate = await service.GetCurrencyRate(from, to);

            Assert.IsTrue(rate.HasValue);
            Assert.IsTrue(rate.Value > 0);
        }

        /// <summary>
        /// Check that rate cannot be calculated when from currency is <see langword="null"/>.
        /// </summary>
        /// <param name="to"></param>
        /// <returns></returns>
        [Test]
        public async Task NullFromCurrencyIdentifier([ValueSource(nameof(RandomCurrencies))] string to)
        {
            var service = new ECBCurrencyRateService();
            var rate = await service.GetCurrencyRate(null, to);
            Assert.IsFalse(rate.HasValue);
        }
        /// <summary>
        /// Check that rate cannot be calculated when to currency is <see langword="null"/>.
        /// </summary>
        /// <param name="from"></param>
        /// <returns></returns>
        [Test]
        public async Task NullToCurrencyIdentifier([ValueSource(nameof(RandomCurrencies))] string from)
        {
            var service = new ECBCurrencyRateService();
            var rate = await service.GetCurrencyRate(from, null);
            Assert.IsFalse(rate.HasValue);
        }
        /// <summary>
        /// Check that rate cannot be calculated if one of the currencies is unknown.
        /// </summary>
        /// <param name="knownCurrency"></param>
        /// <returns></returns>
        [Test]
        public async Task UnknownCurrencyIdentifier([ValueSource(nameof(RandomCurrencies))] string knownCurrency)
        {
            var service = new ECBCurrencyRateService();
            var rate = await service.GetCurrencyRate(knownCurrency, Generator.GetString(5));
            Assert.IsFalse(rate.HasValue);
            rate = await service.GetCurrencyRate(Generator.GetString(5), knownCurrency);
            Assert.IsFalse(rate.HasValue);
        }

        private static IEnumerable<string> GetCurrencies()
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri("https://www.ecb.europa.eu/")
            };

            var response = client.GetAsync("stats/eurofxref/eurofxref-daily.xml").GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            Assert.IsFalse(string.IsNullOrWhiteSpace(responseContent));

            var contentElement = XElement.Parse(responseContent);

            return contentElement.XPathSelectElements($".//*[local-name()='Cube' and @{ECBCurrencyRateService.CurrencyAttributeName}]")
                .Select(x=> x.Attribute(ECBCurrencyRateService.CurrencyAttributeName).Value);
        }
    }
}
