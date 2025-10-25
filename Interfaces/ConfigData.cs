namespace CaliberSplitMagazineCases.Interfaces
{
    public class ConfigData
    {
        // Cases size

        public int CaseWidth { get; set; } = 8;
        public int CaseHeight { get; set; } = 8;

        // ### 1. Choose your own Trader!
        public bool CasesOnSkier { get; set; } = false;
        public int EuroPrice { get; set; } = 1100;

        public bool CasesOnPeacekeeper { get; set; } = true;
        public int USDPrice { get; set; } = 1500;

        public bool CasesOnRef { get; set; } = false;
        public int GpCoinPrice { get; set; } = 25;

        public bool CasesOnJaeger { get; set; } = false;
        public double RoublesPriceMultiplier { get; set; } = 1.25;

        public bool CasesOnPrapor { get; set; } = false;
        public string BarterType { get; set; } = "5c127c4486f7745625356c13";
        public int BarterPrice { get; set; } = 1;

        // ### 2. Generation settings
        public bool UseOnlyKnownCalibers { get; set; } = false;
        public bool RemoveBadCalibers { get; set; } = true;
        public List<string> BadCalibers { get; set; } = new()
        {
            "Caliber40mmRU",
            "Caliber30x29",
            "Caliber20x1mm"
        };

        // ### 4. Cases configuration
        public string BackgroundColor { get; set; } = "red";
        public string BackgroundColorColorConverterAPI { get; set; } = "#cf404e";
        public int Width { get; set; } = 1;
        public int Height { get; set; } = 1;
        public bool FleaMarketBlacklisted { get; set; } = true;
        public int HandbookPriceRoubles { get; set; } = 200000;
    }
}
