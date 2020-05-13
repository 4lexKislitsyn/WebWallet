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
        /// <inheritdoc/>
        public async Task<decimal?> GetCurrencyRate(string fromCurrency, string toCurrency)
        {
            using var client = new HttpClient()
            {
                BaseAddress = new Uri("https://www.ecb.europa.eu/")
            };

            var response = await client.GetAsync("stats/eurofxref/eurofxref-daily.xml");

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();

            var xml = XElement.Parse(responseContent);
            var rates = xml.XPathSelectElements($".//*[local-name()='Cube' and (@currency = '{fromCurrency}' or @currency = '{toCurrency}') and @rate]");
            var parsedRates = DeserializeElements(rates);

            var fromParsedRate = parsedRates.FirstOrDefault(x => x.Currency == fromCurrency);
            var toParsedRate = parsedRates.FirstOrDefault(x => x.Currency == toCurrency);

            if (fromParsedRate == null || toParsedRate == null)
            {
                return null;
            }

            return toParsedRate.Rate / fromParsedRate.Rate;
        }


        private IEnumerable<CurrencyEntity> DeserializeElements(IEnumerable<XElement> elements)
        {
            if (!elements.Any())
            {
                yield break;
            }
            var serializer = new XmlSerializer(typeof(CurrencyEntity));
            foreach (var item in elements)
            {
                using var reader = item.CreateReader();
                if (!serializer.CanDeserialize(reader))
                {
                    continue;
                }
                yield return serializer.Deserialize(reader) as CurrencyEntity;
            }
        }

        [XmlRoot("Cube")]
        private class CurrencyEntity
        {
            [XmlAttribute("currency")]
            public string Currency { get; set; }

            [XmlAttribute("rate")]
            public decimal Rate { get; set; }
        }
    }
}
