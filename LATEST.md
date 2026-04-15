## New functionality for Extended Vanilla Paths

- Better support for multiple extensions of the same vanilla path: you can now cycle upgrades to choose which path to use in game on a per tower basis
- Extended paths can now begin earlier to create alternate branches of vanilla paths, using the same upgrade cycling to choose which path to use for each tower
  - These paths must add all upgrades from their starting tier through tier 5
  - For Modders, all you have to do is give your `UpgradePlusPlus` classes a Tier <= 5, and if your `PathPlusPlus` class's `ExtendVanillaPath` value is properly set it'll do the rest
    - Optionally there's a `PathPlusPlus.UseUpgradedTowerModels` override that can be set to true to make the base TowerModel for the upgrades be the corresponding upgraded version on the original path, rather than the last TowerModel before the branching off point