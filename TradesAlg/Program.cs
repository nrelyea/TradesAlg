using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using TradesAlg;

class Program
{
    static void Main(string[] args)
    {
        // Path to Program.cs class
        string programDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..");

        // Load Inventory data
        //List<Item> inventory = LoadInventoryFromPath(Path.Combine(programDir, "inventorySample.json"));
        List<Item> inventory = LoadInventoryFromCriteria(Criteria.InventoryItemNames);

       // generate full list of possible Trades
       TradeGeneration tg = new TradeGeneration(
            Criteria.WorkBenchLevel,
            Criteria.SafeZoneRecycler,
            Criteria.OutPostTradesEnabled,
            Criteria.BanditTradesEnabled,
            Path.Combine(programDir, Criteria.ItemsJSON),
            Path.Combine(programDir, Criteria.MarketTradesJSON)
        );
        List<Trade> allTradesList = tg.GeneratedTrades;
        Console.WriteLine($"Trades generated: {allTradesList.Count}");


        // Generate / Re-Generate All Item Values
        ItemValueGeneration ivg = new ItemValueGeneration(programDir, allTradesList);
        //ivg.GenerateAllItemValues();                                                  // UN-COMMENT THIS LINE TO RE-GENERATE ALL ITEM VALUES


        // find all possible trades!
        PathGeneration pg = new PathGeneration();
        List<List<Trade>> pathList = pg.FindTrades(inventory, allTradesList, Criteria.TargetItemName, Criteria.SearchDepth);       
        pathList = pg.RemoveGarbagePaths(pathList);
        
        if(pathList != null && pathList.Count > 0)
        {
            // Remove duplicate / redundant steps in paths
            pathList = pg.RemoveDuplicateSteps(pathList);
            
            Console.WriteLine($"\nTrade is possible!\nPaths found: {pathList.Count}\n");
            //pg.PrintPathList(pathList);
        }
        else
        {
            Console.WriteLine("\nTrade is NOT possible.");
            return;
        }

        // Determine full option packages with upfront costs of each trade path as a dictionary of cost items and their amount
        Console.WriteLine("Calculating upfront costs for all valid options...");
        PathListAnalysis3 pla3 = new PathListAnalysis3(ivg);
        List<OptionPackage> optionPackageList = pla3.AllOptionPackages(inventory, pathList, Criteria.TargetItemName, Criteria.TargetItemAmount);

        pla3.PrintOptionPackageList(optionPackageList, Criteria.MaxOptionsListed);








    }

    

    

    

    static List<Item> LoadInventoryFromPath(string jsonFilePath)
    {
        // Read the JSON file
        string jsonContent = File.ReadAllText(jsonFilePath);

        // Parse the JSON content into JArray
        JArray inventoryArray = JArray.Parse(jsonContent);

        List<Item> inventory = new List<Item>();

        foreach (JObject item in inventoryArray)
        {
            string itemName = item.Value<string>("Name");
            int itemQuantity = item.Value<int>("Quantity");
            Console.WriteLine($"Inventory item: {itemQuantity} {itemName}");
            inventory.Add(new Item(itemName, itemQuantity));
        }

        return inventory;
    }

    static List<Item> LoadInventoryFromCriteria(List<string> invStringList)
    {
        List<Item> inventory = new List<Item>();

        foreach(string itemName in invStringList)
        {
            inventory.Add(new Item(itemName, 99));
        }

        return inventory;
    }




    static List<Trade> LoadTradesList(string jsonFilePath)
    {
        // Read the JSON file
        string jsonContent = File.ReadAllText(jsonFilePath);

        // Parse the JSON content into JArray
        JArray tradesArray = JArray.Parse(jsonContent);

        List<Trade> trades = new List<Trade>();

        foreach(JObject tradeObj in tradesArray)
        {
            string category = tradeObj.Value<string>("category");
            
            List<Item> costItems = new List<Item>();
            foreach (var costItem in tradeObj["cost"])
            {
                string costItemName = costItem.Value<string>("name");
                int costItemQuantity = costItem.Value<int>("quantity");
                costItems.Add(new Item(costItemName, costItemQuantity));
            }

            List<Item> resultItems = new List<Item>();
            foreach (var resultItem in tradeObj["result"])
            {
                string resultItemName = resultItem.Value<string>("name");
                int resultItemQuantity = resultItem.Value<int>("quantity");
                resultItems.Add(new Item(resultItemName, resultItemQuantity));
            }

            trades.Add(new Trade(category, costItems, resultItems));
        }

        return trades;
    }

    


    
}
