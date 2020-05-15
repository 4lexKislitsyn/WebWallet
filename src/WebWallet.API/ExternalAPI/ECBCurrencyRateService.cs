using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;
using WebWallet.API.ExternalAPI.Interfaces;

namespace WebWallet.API.ExternalAPI
{
    /// <summary>
    /// Service that uses European Central Bank dayli rates.
    /// </summary>
    public class ECBCurrencyRateService : ICurrencyRateService
    {
        private readonly ECBCurrencyConfiguration _configuration;

        /// <summary>
        /// Create an instance of <see cref="ECBCurrencyRateService"/>.
        /// </summary>
        /// <param name="options"></param>
        public ECBCurrencyRateService(IOptions<ECBCurrencyConfiguration> options)
        {
            _configuration = options.Value;
        }
        /// <summary>
        /// Name of currency identifier attribute.
        /// </summary>
        public const string CurrencyAttributeName = "currency";
        /// <inheritdoc/>
        public async Task<decimal?> GetCurrencyRate(string fromCurrency, string toCurrency)
        {
            if (fromCurrency == toCurrency)
            {
                return 1;
            }
            using var client = new HttpClient()
            {
                BaseAddress = new Uri(_configuration.BaseUrl)
            };

            var response = await client.GetAsync(_configuration.RatePath);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();

            var xml = XElement.Parse(responseContent);
            var rates = xml.XPathSelectElements($".//*[local-name()='Cube' and (@{CurrencyAttributeName} = '{fromCurrency}' or @{CurrencyAttributeName} = '{toCurrency}') and @rate]");
            var parsedRates = DeserializeElements(rates).ToArray();

            var fromParsedRate = parsedRates.FirstOrDefault(x => x.Currency == fromCurrency);
            var toParsedRate = parsedRates.FirstOrDefault(x => x.Currency == toCurrency);

            if (fromParsedRate == null || toParsedRate == null)
            {
                return null;
            }

            return toParsedRate.Rate / fromParsedRate.Rate;
        }


        private IEnumerable<ECBCurrencyEntity> DeserializeElements(IEnumerable<XElement> elements)
        {
            if (!elements.Any())
            {
                yield break;
            }
            var serializer = new XmlSerializer(typeof(ECBCurrencyEntity));
            foreach (var item in elements)
            {
                using var reader = item.CreateReader();
                if (!serializer.CanDeserialize(reader))
                {
                    continue;
                }
                yield return serializer.Deserialize(reader) as ECBCurrencyEntity;
            }
        }
        /// <summary>
        /// Model of currency's rate from ECB.
        /// </summary>
        [XmlRoot("Cube", Namespace = "http://www.ecb.int/vocabulary/2002-08-01/eurofxref")]
        public class ECBCurrencyEntity
        {
            /// <summary>
            /// Currency identifier.
            /// </summary>
            [XmlAttribute("currency")]
            public string Currency { get; set; }
            /// <summary>
            /// Currency rate to 1 magic unit.
            /// </summary>
            [XmlAttribute("rate")]
            public decimal Rate { get; set; }
        }
    }
}
