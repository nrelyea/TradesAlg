using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradesAlg
{
    public static class Criteria
    {
        // user criteria for search
        public static readonly List<string> InventoryItemNames = new List<string>
        {
            "scrap"
        };
        public const string TargetItemName = "crossbow";
        public const int TargetItemAmount = 4;

        public const int WorkBenchLevel = 3;
        public const bool SafeZoneRecycler = true;
        public const bool OutPostTradesEnabled = true;
        public const bool BanditTradesEnabled = true;



        // source files for parsing / gathering item & trade data
        public const string ItemsJSON = "itemsbyname.json";
        public const string MarketTradesJSON = "marketTrades.json";

        // operation & debugging settings
        public const int SearchDepth = 4;
        public const int MaxOptionsListed = 1;
        public const bool LoggingEnabled = false;

        // item valuation settings
        public const bool SkipPreviouslyGeneratedValues = true;
        public const string ItemValueBaseline = "scrap";
        public const int ItemValueTargetQuantity = 100;
        public const string ItemValuesJSON = "itemValues.json";
    }
}
