Current known bugs:

- PathAnalysis: MASSIVE bug with Path analysis in which it over-counts costs involving crafting cost items who have redundant earlier steps to procure them
- PathFinding: Lingering "dumb" trade paths with redundant steps (ex. see targeting "G" with "A" & "D" in inventory, random paths with extra A->B / D->B trades)
- general lack of input safety
  - targeting an item already in player inventory breaks everything
  - targeting items that dont exist results in runtime errors
