Current known bugs:

- PathFinding: Lingering "dumb" trade paths with redundant steps (ex. see targeting "G" with "A" & "D" in inventory, random paths with extra A->B / D->B trades)
- general lack of input safety
  - targeting an item already in player inventory breaks everything
  - targeting items that dont exist results in runtime errors
