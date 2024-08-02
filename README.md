Current known bugs:

- PathFinding: Lingering "dumb" trade paths with redundant steps (ex. see targeting "G" with "A" & "D" in inventory, random paths with extra A->B / D->B trades)
- PathListAnalysis: StackOverFlow Exceptions when targeting high quantities (i.e. 12345 Cloth)
  - Will likely need to be mitigated by refactoring TraverseAndSimulateForCost() to utilize a single loop with state tracking rather than current recursive implementation (sadface) 
- general lack of input safety
  - targeting an item already in player inventory breaks everything
  - targeting items that dont exist results in runtime errors
