﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CryptoCurrencyFinderFunction
{
    public static class Finder
    {
        public static async Task<string> GetCurrenciesJson()
        {
            List<string> currenciesListedInLiqui = await CurrenciesListedInLiqui();
            List<string> currenciesListedInEtherDelta = await CurrenciesListedInEtherDelta();

            List<string> currenciesListedInBitRex = await CurrenciesListedInBitrex();
            List<string> currenciesListedInBitfinex = await CurrenciesListedInBitfinex();
            List<string> currenciesListedInPoloniex = await CurrenciesListedInPoloniex();
            List<string> currenciesListedInKraken = await CurrenciesListedInKraken();

            List<string> currenciesListedInDecentralizedExchanges = currenciesListedInLiqui.Union(currenciesListedInEtherDelta).ToList();

            List<string> currenciesListedInCentralizedExchanges = currenciesListedInBitRex.Union(currenciesListedInBitfinex).Union(currenciesListedInPoloniex).Union(currenciesListedInKraken).ToList();

            List<string> currenciesListedInDecentralizedButNotCentralized = currenciesListedInDecentralizedExchanges.Where(c => !currenciesListedInCentralizedExchanges.Contains(c)).OrderBy(x => x).ToList();

            List<CryptoCurrencyInfo> cryptoCurrencyInfoList = await GetCurrencyInfoList(currenciesListedInDecentralizedButNotCentralized);
            cryptoCurrencyInfoList = cryptoCurrencyInfoList.OrderByDescending(c => c.VolumeLast24HoursUsd).ToList();
            return JsonConvert.SerializeObject(cryptoCurrencyInfoList);
        }

        private static async Task<List<CryptoCurrencyInfo>> GetCurrencyInfoList(List<string> currencySymbols)
        {
            List<CryptoCurrencyInfo> cryptoCurrencyInfos = new List<CryptoCurrencyInfo>();
            string symbolsDelimited = string.Join("%2C", currencySymbols);
            string uri = $"http://coinmarketcap.northpole.ro/ticker.json?symbol={symbolsDelimited}&page=0&size={currencySymbols.Count}";

            var client = new WebClient();
            var json = await client.DownloadStringTaskAsync(new Uri(uri));
            JObject coinMarketCap = JObject.Parse(json);

            foreach (string symbol in currencySymbols)
            {
                var market = coinMarketCap["markets"].Where(m => m["symbol"].ToString() == symbol).FirstOrDefault();
                if (market != null)
                {
                    CryptoCurrencyInfo cryptoCurrencyInfo = new CryptoCurrencyInfo
                    {
                        Symbol = symbol,
                        Identifier = market["identifier"].ToString(),
                        CurrentPriceUsd = market["price"]["usd"].ToObject<double>(),
                        CurrentMarketCapUsd = market["marketCap"]["usd"].ToObject<double>()
                    };
                    cryptoCurrencyInfo = await PopulateCurrencyHistoricalInfo(cryptoCurrencyInfo);
                    if (cryptoCurrencyInfo.AtLeastOnePercentageIsPositive())
                    {
                        cryptoCurrencyInfos.Add(cryptoCurrencyInfo);
                    }
                }
            }

            return cryptoCurrencyInfos;
        }

        private async static Task<CryptoCurrencyInfo> PopulateCurrencyHistoricalInfo(CryptoCurrencyInfo cryptoCurrencyInfo)
        {
            CryptoCurrencyInfo result = new CryptoCurrencyInfo {
                Symbol = cryptoCurrencyInfo.Symbol,
                Identifier = cryptoCurrencyInfo.Identifier,
                CurrentPriceUsd = cryptoCurrencyInfo.CurrentPriceUsd,
                CurrentMarketCapUsd = cryptoCurrencyInfo.CurrentMarketCapUsd
            };

            var client = new WebClient();
            var json = await client.DownloadStringTaskAsync(new Uri($"http://coinmarketcap.northpole.ro/history.json?coin={cryptoCurrencyInfo.Identifier}&period={DateTime.UtcNow.Year}&format=array"));
            JObject coinMarketCapHistory = JObject.Parse(json);

            var reversedHistory = coinMarketCapHistory["history"].Reverse();
            long volumeLast24HoursUsd = reversedHistory.First()["volume24"]["usd"].ToObject<long>();
            long volumeSecondToLast24HoursUsd = reversedHistory.Count() <= 1 ? 0 : reversedHistory.Skip(1).Take(1).First()["volume24"]["usd"].ToObject<long>();
            long volumeLast72HoursUsd = reversedHistory.Count() <= 3 ? 0 : reversedHistory.Take(3).Sum(x => x["volume24"]["usd"].ToObject<long>());
            long volumePrevious72HoursUsd = reversedHistory.Count() <= 6 ? 0: reversedHistory.Skip(3).Take(3).Sum(x => x["volume24"]["usd"].ToObject<long>());
            double average24HourVolumePrevious72HoursUsd = reversedHistory.Count() <= 4 ? 0 : reversedHistory.Skip(1).Take(3).Average(x => x["volume24"]["usd"].ToObject<long>());
            double yesterdaysPriceUsd = reversedHistory.Count() <= 1 ? 0 : reversedHistory.Skip(1).Take(1).First()["price"]["usd"].ToObject<double>();
            double dayBeforeYesterdaysPriceUsd = reversedHistory.Count() <= 2 ? 0 : reversedHistory.Skip(2).Take(1).First()["price"]["usd"].ToObject<double>();

            result.VolumeLast24HoursUsd = volumeLast24HoursUsd;
            result.VolumePrevious24HoursUsd = volumeSecondToLast24HoursUsd;
            result.VolumeLast24HoursPercentChangeVsPrevious24Hours = GetPercentageChangeString(volumeSecondToLast24HoursUsd, volumeLast24HoursUsd);
            result.VolumeLast72HoursUsd = volumeLast72HoursUsd;
            result.VolumePrevious72HoursUsd = volumePrevious72HoursUsd;
            result.VolumeLast72HoursPercentChangeVsPrevious72Hours = GetPercentageChangeString(volumePrevious72HoursUsd, volumeLast72HoursUsd);
            result.Average24HourVolumePrevious72HoursUsd = average24HourVolumePrevious72HoursUsd;
            result.VolumeLast24HoursPercentChangeVsAverage24HourPrevious72Hours = GetPercentageChangeString(average24HourVolumePrevious72HoursUsd, volumeLast24HoursUsd);
            result.YesterdaysPriceUsd = yesterdaysPriceUsd;
            result.PricePercentChangeVsYesterday = GetPercentageChangeString(yesterdaysPriceUsd, result.CurrentPriceUsd);
            result.DayBeforeYesterdaysPriceUsd = dayBeforeYesterdaysPriceUsd;
            result.PricePercentChangeVs2DaysAgo = GetPercentageChangeString(dayBeforeYesterdaysPriceUsd, result.CurrentPriceUsd);

            return result;
        }

        private static string GetPercentageChangeString(double earllierValue, double laterValue)
        {
            return earllierValue == 0 ? "N/A" : String.Format("{0:P2}.", CalculatePercentageChange(earllierValue, laterValue));
        }

        private static double CalculatePercentageChange(double earllierValue, double laterValue)
        {
            return ((laterValue - earllierValue)) / earllierValue;
        }

        private async static Task<List<string>> CurrenciesListedInLiqui()
        {
            var client = new WebClient();
            var json = await client.DownloadStringTaskAsync(new Uri("https://api.liqui.io/api/3/info"));
            JObject liqui = JObject.Parse(json);
            return new HashSet<string>(liqui["pairs"].Select(pair => pair.Value<JProperty>().Name.Split('_')[0].ToUpper()).OrderBy(x => x)).ToList();
        }

        private async static Task<List<string>> CurrenciesListedInEtherDelta()
        {
            var client = new WebClient();
            var json = await client.DownloadStringTaskAsync(new Uri("https://etherdelta.github.io/config/main.json"));
            JObject etherDelta = JObject.Parse(json);
            return etherDelta["tokens"].Select(token => token["name"].ToString().ToUpper()).OrderBy(x => x).ToList();
        }

        private async static Task<List<string>> CurrenciesListedInBitrex()
        {
            var client = new WebClient();
            var json = await client.DownloadStringTaskAsync(new Uri("https://bittrex.com/api/v1.1/public/getmarkets"));
            JObject bitrex = JObject.Parse(json);
            return new HashSet<string>(bitrex["result"].Select(token => token["MarketCurrency"].ToString().ToUpper()).OrderBy(x => x)).ToList();
        }

        private async static Task<List<string>> CurrenciesListedInBitfinex()
        {
            var client = new WebClient();
            var json = await client.DownloadStringTaskAsync(new Uri("https://api.bitfinex.com/v1/symbols"));
            JArray bitFinex = JArray.Parse(json);
            return new HashSet<string>(bitFinex.ToObject<List<string>>().Select(x => x.Replace("btc", "").Replace("eth", "").Replace("usd", "").ToUpper()).OrderBy(x => x)).ToList();
        }

        private async static Task<List<string>> CurrenciesListedInPoloniex()
        {
            var client = new WebClient();
            var json = await client.DownloadStringTaskAsync(new Uri("https://poloniex.com/public?command=returnTicker"));
            JToken poloniex = JToken.Parse(json);
            return new HashSet<string>(poloniex.Select(pair => pair.Value<JProperty>().Name.Split('_')[1].ToUpper()).OrderBy(x => x)).ToList();
        }

        private async static Task<List<string>> CurrenciesListedInKraken()
        {
            var client = new WebClient();
            var json = await client.DownloadStringTaskAsync(new Uri("https://api.kraken.com/0/public/Assets"));
            JObject kraken = JObject.Parse(json);
            return kraken["result"].Children().Select(token => token.First()["altname"].ToString().ToUpper()).OrderBy(x => x).ToList();
        }
    }
}
