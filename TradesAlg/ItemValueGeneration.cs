using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradesAlg
{
    public class ItemValueGeneration
    {
        private string ProgramDir;
        private List<Trade> TradesList;
        
        private string ItemValuesJSONPath;
        private string ItemDataPath;
        private Dictionary<string, double> ItemValues;

        public ItemValueGeneration(string programDir, List<Trade> tradesList)
        {
            ProgramDir = programDir;
            TradesList = tradesList;

            ItemValuesJSONPath = Path.Combine(ProgramDir, Criteria.ItemValuesJSON);
            ItemDataPath = Path.Combine(ProgramDir, Criteria.ItemsJSON);
        }

        public void GenerateAllItemValues()
        {            
            ItemValues = LoadItemValues();

            // get item names to have value calculated
            List<string> allItemNames = GetItemsForEvaluation();

            // determine and update item value for each item
            for (int i = 0; i < allItemNames.Count; i++)
            {
                string itemName = allItemNames[i];
                double itemValue = CalculateItemValue(itemName, i + 1, allItemNames.Count, TradesList);

                SaveItemValue(itemName, itemValue);
            }

            SortItemsByValueInFile();
        }

        private void SortItemsByValueInFile()
        {
            Dictionary<string, double> updatedItemValues = LoadItemValues();
            ItemValues = updatedItemValues.OrderBy(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            foreach(KeyValuePair<string, double> kvp in ItemValues)
            {
                SaveItemValue(kvp.Key, kvp.Value);
            }
        }

        private double CalculateItemValue(string itemName, int itemNumber, int totalItemCount, List<Trade> tradesList)
        {
            // based on set properties for how value will be determined, create a baseline inv setup using that item
            List<Item> baseLineInv = new List<Item> { new Item(Criteria.ItemValueBaseline, 99) };
            
            List<List<Trade>> pathList = GeneratePathsForCalculation(baseLineInv, itemName, tradesList);
            if(pathList == null) pathList = new List<List<Trade>>();

            // This mess just to log progress of item valuation progress cuz it will take forever
            string progressStr = $"Calculating item value for '{itemName}'...";
            double percentageComplete = ((double)(itemNumber) / (double)totalItemCount) * 100;
            Console.WriteLine($"{progressStr.PadRight(70)}\tItem #{itemNumber}/{totalItemCount}\t({Math.Round(percentageComplete, 2)}%)    \t({pathList.Count} paths)");

            if (pathList.Count == 0)
            {
                //Console.WriteLine($"No path(s) exist to obtain {itemName} from {Criteria.ItemValueBaseline}, saving value as 0.0");
                return 0;
            }
          
            // calculate all upfront costs possible for this item using baseline item to determine cheapest path
            PathListAnalysis3 pla3 = new PathListAnalysis3(this);
            List<OptionPackage> optionPackageList = pla3.AllOptionPackages(baseLineInv, pathList, itemName, Criteria.ItemValueTargetQuantity);

            // return cheapest upfront cost as a fraction of ItemValueTargetQuantity to form a more general cost / value basis for this item
            OptionPackage cheapestOption = optionPackageList[0];
            return cheapestOption.UpfrontCost[Criteria.ItemValueBaseline] / Criteria.ItemValueTargetQuantity;
        }

        private List<List<Trade>> GeneratePathsForCalculation(List<Item> baseLineInv, string itemName, List<Trade> tradesList)
        {
            PathGeneration pg = new PathGeneration();
            List<List<Trade>> pathList = pg.FindTrades(baseLineInv, tradesList, itemName, Criteria.SearchDepth);
            pathList = pg.RemoveGarbagePaths(pathList);

            if (pathList != null && pathList.Count > 0)
            {
                pathList = pg.RemoveDuplicateSteps(pathList);
            }
            else
            {
                return null;
            }

            return pathList;
        }

        public Dictionary<string, double> LoadItemValues()
        {
            if (File.Exists(ItemValuesJSONPath))
            {
                string json = File.ReadAllText(ItemValuesJSONPath);
                return JsonConvert.DeserializeObject<Dictionary<string, double>>(json) ?? new Dictionary<string, double>();
            }

            return new Dictionary<string, double>();
        }

        private void SaveItemValue(string itemName, double itemValue)
        {
            ItemValues[itemName] = itemValue;
            UpdateJsonFile();
        }

        private void UpdateJsonFile()
        {
            string json = JsonConvert.SerializeObject(ItemValues, Formatting.Indented);
            File.WriteAllText(ItemValuesJSONPath, json);
        }

        private List<string> GetItemsForEvaluation()
        {
            HashSet<string> allItems = GetAllItemNames();
            Dictionary<string, double> alreadyEvaluatedItems = LoadItemValues();

            List<string> itemsToEvaluate = new List<string>();
            foreach(string itemName in allItems)
            {
                if (!alreadyEvaluatedItems.ContainsKey(itemName))
                {
                    itemsToEvaluate.Add(itemName);
                }
            }

            return itemsToEvaluate;
        }

        private HashSet<string> GetAllItemNames()
        {
            HashSet<string> itemNames = new HashSet<string>();

            JObject allItemsDataObj = JObject.Parse(File.ReadAllText(ItemDataPath));
            foreach (var property in allItemsDataObj.Properties())
            {
                string itemName = property.Name;
                itemNames.Add(itemName);
            }

            return itemNames;
        }
    }
}
