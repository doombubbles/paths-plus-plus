## New functionality for Extended Vanilla Paths

- Better support for multiple extensions of the same vanilla path: you can now cycle to choose which path to use on a per-tower basis in game as well as in the upgrade screen (which sets the default).
- Extended paths can now begin before Tier 6 to create alternate branches of vanilla paths, using the same upgrade cycling to choose which path to use for each tower
  - These paths must add all upgrades from their starting tier through at least tier 5 to be valid
  - For Modders, all you have to do is give your `UpgradePlusPlus` classes a Tier <= 5, and if your `PathPlusPlus` class's `ExtendVanillaPath` value is properly set it'll do the rest
    - Optionally there's a `PathPlusPlus.UseUpgradedTowerModels` override that can be set to true to make the base TowerModel for the upgrades be the corresponding upgraded version on the original path, rather than the last TowerModel before the branching off point