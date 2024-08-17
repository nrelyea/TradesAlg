using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace TradesAlg
{
    public class PathGeneration
    {
        public PathGeneration() { }

        public List<List<Trade>> FindTrades(List<Item> inventory, List<Trade> trades, string targetItemName, int depth, List<string> analyzedTargetItems = null)
        {          
            // stop path search if depth has been reached
            if (depth <= 0)
            {
                //Console.WriteLine($"Max depth hit while searching this branch for {targetItemName} Trades");
                return null;
            }    
            
            // start tracking already analyzed target items if needed
            if (analyzedTargetItems == null)
            {
                analyzedTargetItems = new List<string>();
            }

            List<Trade> targetTrades = new List<Trade> { };
            foreach (Trade trade in trades)   // find all trades that result in the current target item
            {
                foreach (Item resultItem in trade.ResultItems)
                {
                    if (resultItem.Name == targetItemName)
                    {                       
                        //Console.WriteLine($"- target trade found: {trade.StringSummary()}");
                        targetTrades.Add(trade);
                        break;
                    }
                }
            }

            List<List<Trade>> pathsFound = new List<List<Trade>>();

            if (targetTrades.Count > 0)  // if at least one trade is possible for the target item in question
            {
                foreach (Trade trade in targetTrades)
                {
                    //Console.WriteLine($"Considering trade '{trade.StringSummary()}' that could result in {targetItemName}");
                    if (trade.IsPossible(inventory))  // if trade is immediately possible, return true
                    {
                        //Console.WriteLine($"--- Direct trade possible for {targetItemName} -> {trade.StringSummary()}");                  
                        List<Trade> path = new List<Trade>();
                        path.Add(trade);
                        pathsFound.Add(path);
                    }
                    else   // otherwise determine if trade is possible via other connected trades
                    {
                        //Console.WriteLine($"--- Direct trade NOT possible for {targetItemName}... looking for indirect trades");
                        List<List<List<Trade>>> pathListList = new List<List<List<Trade>>>();

                        // for each cost Item for this trade, recursively call FindTrades on that item, and add the resulting list to the <<<List>>> pathListList
                        foreach (Item costItem in trade.CostItems)
                        {
                            string costItemName = costItem.Name;
                            //Console.WriteLine($"--- Checking trades for {targetItemName} cost item {costItemName} at depth {depth}");

                            // create a new list including the current target item and already analyzed target items to pass to the recurssive FindTrades call
                            List<string> updatedAnalyzedTargetItems = new List<string> { targetItemName };
                            updatedAnalyzedTargetItems.AddRange(analyzedTargetItems);

                            //Console.WriteLine("Already checked trades for: " + string.Join(", ", updatedAnalyzedTargetItems));

                            // skip recurssive FindTrades call for this cost item if it is already in the inventory
                            bool skipRecurssiveFindTradesCall = false;
                            foreach (Item item in inventory)
                            {
                                if (item.Name == costItemName && item.Quantity > 0)
                                {
                                    //Console.WriteLine($"--- Inventory contains {costItemName}, skipping further search for trades on this item");
                                    skipRecurssiveFindTradesCall = true;
                                    break;
                                }
                            }
                            if (skipRecurssiveFindTradesCall) { continue; }

                            // only consider trades for that cost item if it has not yet been analyzed as a target item
                            if (!analyzedTargetItems.Contains(costItemName))
                            {
                                //Console.WriteLine($"Number of items already analyzed: {analyzedTargetItems.Count}");
                                List<List<Trade>> costItemTrades = FindTrades(inventory, trades, costItemName, depth - 1, updatedAnalyzedTargetItems);
                                if (costItemTrades != null)
                                {
                                    pathListList.Add(costItemTrades);
                                }
                            }
                        }

                        // find all potential combinations of paths leading to the above costItems, and add any/all found to the pathsFound list
                        List<List<Trade>> indirectPaths = CombineAndConsolidateAllPossibleAltPathLists(pathListList);

                        // add the current trade to the start of each path in the indirect path list
                        foreach (List<Trade> path in indirectPaths)
                        {
                            path.Add(trade);
                        }

                        // add final set of indirect paths from this trade to the pathsFound list
                        pathsFound.AddRange(indirectPaths);
                    }
                }
            }
            else
            {
                //Console.WriteLine($"--- No trades possible for {targetItemName}");
                return null;   // no trades exist for the target item
            }

            return pathsFound;
        }

        private List<List<Trade>> CombineAndConsolidateAllPossibleAltPathLists(List<List<List<Trade>>> pathListList)
        {
            // base case for empty list:
            if (pathListList.Count == 0)
            {
                return new List<List<Trade>> { };
            }

            // base case for 1 element: if only one path in pathListList, return that path list alone in the <<List>>
            List<List<Trade>> firstPathList = (pathListList.First()).ToList();
            if (pathListList.Count <= 1)
            {
                return firstPathList;
            }

            // recursively call this method on the remainder of pathLists in the pathListList
            List<List<Trade>> remainderPathList = CombineAndConsolidateAllPossibleAltPathLists(pathListList.Skip(1).ToList());

            List<List<Trade>> consolidatedPathList = new List<List<Trade>>();

            // combine firstPathList and remainderPathList into one list based on all possible combinations between the lists

            foreach (List<Trade> pathA in firstPathList)
            {
                foreach (List<Trade> pathB in remainderPathList)
                {
                    List<Trade> combinedList = pathA.Concat(pathB).ToList();
                    consolidatedPathList.Add(combinedList);
                }
            }

            return consolidatedPathList;
        }

        public List<List<Trade>> RemoveDuplicateSteps(List<List<Trade>> pathList)
        {
            List<List<Trade>> distinctPathList = new List<List<Trade>>();

            foreach (List<Trade> path in pathList)
            {
                List<Trade> distinctPath = new List<Trade>();

                foreach (Trade trade in path)
                {
                    bool isDistinctTrade = true;

                    foreach (Trade distinctTrade in distinctPath)
                    {
                        if (trade.StringSummary() == distinctTrade.StringSummary())
                        {
                            isDistinctTrade = false;
                            break;
                        }
                    }

                    if (isDistinctTrade)
                    {
                        distinctPath.Add(trade);
                    }
                }

                distinctPathList.Add(distinctPath);
            }

            return distinctPathList;
        }

        public List<List<Trade>> RemoveGarbagePaths(List<List<Trade>> pathList)
        {
            Console.WriteLine("\n --- TAKING OUT THE TRASH ---");

            if (pathList == null) return pathList;

            // remove paths that include recycling and crafting of same item
            for(int i = pathList.Count - 1; i >= 0; i--)
            {              
                HashSet<string> recycledItems = new HashSet<string>();
                HashSet<string> craftedItems = new HashSet<string>();
                foreach(Trade trade in pathList[i])
                {
                    if(trade.Category == "Recycle")
                    {
                        if (craftedItems.Contains(trade.CostItems[0].Name))
                        {
                            //Console.WriteLine($"Redundant trade: {trade.StringSummary()}\nRemoving this garbage path:");
                            //PrintPath(pathList[i]);
                            pathList.RemoveAt(i);
                            break;
                        }
                        recycledItems.Add(trade.CostItems[0].Name);
                    }
                    else if (trade.Category == "Craft")
                    {
                        if (recycledItems.Contains(trade.ResultItems[0].Name))
                        {
                            //Console.WriteLine($"Redundant trade: {trade.StringSummary()}\nRemoving this garbage path:");
                            //PrintPath(pathList[i]);
                            pathList.RemoveAt(i);
                            break;
                        }
                        craftedItems.Add(trade.ResultItems[0].Name);
                    }
                }

            }

            return pathList;
        }

        public void PrintPathList(List<List<Trade>> pathList)
        {
            foreach (List<Trade> path in pathList)
            {
                PrintPath(path);
            }
        }

        private void PrintPath(List<Trade> path)
        {
            List<string> strList = new List<string>();
            foreach (Trade trade in path)
            {
                strList.Add(trade.StringSummary());
            }
            Console.WriteLine($"Trade Path: {string.Join(" -> ", strList)}");
        }
    }
}
