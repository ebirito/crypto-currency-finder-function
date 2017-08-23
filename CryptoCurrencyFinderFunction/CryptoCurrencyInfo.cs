namespace CryptoCurrencyFinderFunction
{
    public class CryptoCurrencyInfo
    {
        public string Symbol { get; set; }
        public string Identifier { get; set; }
        public long VolumeLast24HoursUsd { get; set; }
        public long VolumePrevious24HoursUsd { get; set; }
        public string VolumeLast24HoursPercentChangeVsPrevious24Hours { get; set; }
        public long VolumeLast72HoursUsd { get; set; }
        public long VolumePrevious72HoursUsd { get; set; }
        public string VolumeLast72HoursPercentChangeVsPrevious72Hours { get; set; }
        public double Average24HourVolumePrevious72HoursUsd { get; set; }
        public string VolumeLast24HoursPercentChangeVsAverage24HourPrevious72Hours { get; set; }
        public double CurrentPriceUsd { get; set; }
        public double YesterdaysPriceUsd { get; set; }
        public string PricePercentChangeVsYesterday { get; set; }
        public double DayBeforeYesterdaysPriceUsd { get; set; }
        public string PricePercentChangeVs2DaysAgo { get; set; }
        public double CurrentMarketCapUsd { get; set; }

        public bool AtLeastOnePercentageIsPositive()
        {
            return !VolumeLast24HoursPercentChangeVsPrevious24Hours.Contains("-") ||
                !VolumeLast72HoursPercentChangeVsPrevious72Hours.Contains("-") ||
                !VolumeLast24HoursPercentChangeVsAverage24HourPrevious72Hours.Contains("-");
        }
    }
}
