using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TradesAlg
{
    public class TradeGeneration
    {
        private int WorkBenchLevel;
        private bool SafeZoneRecycler;
        private bool OutPostTradesEnabled;
        private bool BanditTradesEnabled;
        private string ItemDataPath;
        private string MarketDataPath;
        private string GeneratedTradesPath;

        public List<Trade> GeneratedTrades;
        
        public TradeGeneration(int workBenchLevel, bool safeZoneRecycler, bool outpostTradesEnabled, bool banditTradesEnabled, string itemDataPath, string marketDataPath, string generatedTradesPath)
        {
            WorkBenchLevel = workBenchLevel;
            SafeZoneRecycler = safeZoneRecycler;
            OutPostTradesEnabled = outpostTradesEnabled;
            BanditTradesEnabled = banditTradesEnabled;
            ItemDataPath = itemDataPath;
            MarketDataPath = marketDataPath;
            GeneratedTradesPath = generatedTradesPath;

            GeneratedTrades = new List<Trade>();

            GenerateTradesPerItemData();

            GenerateTradesPerMarketData();
        }

        private void GenerateTradesPerMarketData()
        {
            // Don't bother parsing if no market trades are enabled per user criteria
            if(!OutPostTradesEnabled && !BanditTradesEnabled) return;
            
            // Parse the JSON content into JArray
            JArray tradesArray = JArray.Parse(File.ReadAllText(MarketDataPath));

            foreach (JObject tradeObj in tradesArray)
            {
                string market = tradeObj.Value<string>("market");

                // only add the Market Trade to the list if it matches user criteria for being in outpost or bandit camp
                if((OutPostTradesEnabled && market == "outpost") || (BanditTradesEnabled && market == "bandit-camp"))
                {
                    Trade newMarketTrade = new Trade("Sell", new List<Item> { }, new List<Item> { });

                    var costItem = tradeObj["cost"][0];
                    newMarketTrade.CostItems.Add(new Item(costItem.Value<string>("name"), costItem.Value<int>("quantity")));

                    var resultItem = tradeObj["result"][0];
                    newMarketTrade.ResultItems.Add(new Item(resultItem.Value<string>("name"), resultItem.Value<int>("quantity")));

                    //Console.WriteLine($"Market trade: " + newMarketTrade.AdvancedStringSummary(1));
                    GeneratedTrades.Add(newMarketTrade);
                }                
            }
        }

        private void GenerateTradesPerItemData()
        {
            JObject allItemsDataObj = JObject.Parse(File.ReadAllText(ItemDataPath));
            foreach(var property in allItemsDataObj.Properties())
            {
                JObject itemObj = (JObject)property.Value;

                // if item is craftable, generate a Craft Trade for it if user criteria allows
                var craftData = itemObj["craftData"] as JObject;
                if (craftData != null && craftData.HasValues)
                {
                    GenerateCraftTrade(itemObj, craftData);
                }

                // if item is recycleable, generate a Recycle Trade per user criteria
                var recycleData = itemObj["recycleData"] as JObject;
                if (recycleData != null && recycleData.HasValues)
                {
                    GenerateRecycleTrade(itemObj, recycleData);
                }
                
            }
        }

        private void GenerateCraftTrade(JObject item, JObject craftData)
        {
            // skip this Trade if it is above the user-specified Workbench Level
            if ((int)craftData["workbench"] > WorkBenchLevel) return;

            Trade newCraftTrade = new Trade("Craft", new List<Item> {}, new List<Item> {});

            JArray ingredientArray = craftData["recipie"]["craftCost"] as JArray;

            // add each ingredient to the trade
            foreach (var ingredient in ingredientArray)
            {
                newCraftTrade.CostItems.Add(new Item((string)ingredient["name"], (int)ingredient["amount"]));
            }

            // add resulting item and its yield to the trade
            newCraftTrade.ResultItems.Add(new Item((string)item["name"], (int)craftData["yield"]));

            // Add the completed Craft Trade to the GeneratedTrades List
            //Console.WriteLine(newCraftTrade.AdvancedStringSummary(1));
            GeneratedTrades.Add(newCraftTrade);
        }

        private void GenerateRecycleTrade(JObject item, JObject recycleData)
        {
            Trade newRecycleTrade = new Trade("Recycle", new List<Item> { }, new List<Item> { });

            // add 1 of the item which will be recycled as the single cost item
            newRecycleTrade.CostItems.Add(new Item((string)item["name"], 1));

            // change which set of yields to collect based on which type of recycle user has specified
            if (SafeZoneRecycler)
            {
                JArray recycleYieldArray = recycleData["recycleYieldsafe"] as JArray;

                // add each yielded item to the trade results
                foreach (var yieldedItem in recycleYieldArray)
                {
                    newRecycleTrade.ResultItems.Add(new Item((string)yieldedItem["name"], (int)yieldedItem["amount"]));
                }
            }
            else
            {
                JArray recycleYieldArray = recycleData["recycleYieldrad"] as JArray;

                // add each yielded item to the trade results
                foreach (var yieldedItem in recycleYieldArray)
                {
                    newRecycleTrade.ResultItems.Add(new Item((string)yieldedItem["name"], (int)yieldedItem["amount"]));
                }
            }

            // Add the completed Recycle Trade to the GeneratedTrades List
            //Console.WriteLine(newRecycleTrade.AdvancedStringSummary(1));
            GeneratedTrades.Add(newRecycleTrade);
        }


        private void WriteTradesToJSON()
        {
            List<Trade> trades = new List<Trade>
            {
                new Trade
                (
                    "Craft",
                    new List<Item>
                    {
                        new Item ("Wood", 200),
                        new Item ("Metal Fragments", 75),
                        new Item ("Rope", 2)
                    },
                    new List<Item>
                    {
                        new Item ("Crossbow", 1)
                    }
                )
            // You can add more Trade objects to this list
            };

            // Convert the list of trades to a JArray
            JArray jsonArray = JArray.FromObject(trades);

            // Convert JArray to JSON string
            string jsonString = jsonArray.ToString(Formatting.Indented);

            // Write JSON string to a file
            File.WriteAllText(GeneratedTradesPath, jsonString);

            Console.WriteLine($"JSON file saved to {GeneratedTradesPath}");
        }
    }
}
