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
    }
}
