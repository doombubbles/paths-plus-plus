<h1 align="center">
<a href="https://github.com/doombubbles/paths-plus-plus/releases/latest/download/PathsPlusPlus.dll">
    <img align="left" alt="Icon" height="90" src="Icon.png">
    <img align="right" alt="Download" height="75" src="https://raw.githubusercontent.com/gurrenm3/BTD-Mod-Helper/master/BloonsTD6%20Mod%20Helper/Resources/DownloadBtn.png">
</a>

Paths++

</h1>

A helper mod allowing additional upgrade paths to be made for towers.

Toggle "Balanced Mode" to switch from being still only able get up to 5 upgrades in any one path and 2 in another, vs getting all vanilla upgrades you normally can as well as any/all available Paths++ upgrades.

Toggling off Balanced Mode can also function well with Ultimate Crosspathing, allowing you to get 5/5/5/5/... towers (assuming the Mod Creators were keeping it in mind while coding).

## Mods

### [Jonyboylovespie's 4th Paths](https://github.com/Jonyboylovespie/4thPaths/releases/latest) (Mod Jam First Place)

### [Hoo-Know's Tesla Coil Tack Shooter](https://github.com/Hoo-Knows/BTD6.TeslaCoil#readme) (Mod Jam Second Place)

### [Fishythecrafter's Bomb Shooter Path](https://github.com/Fishythecrafter/BombShooterPath-/releases/latest) (Mod Jam Third Place)

### [doombubbles' Tornado Wizards](https://github.com/doombubbles/tornado-wizards#readme) (Primary Example)

![Screenshot](Screenshot.png)

## For Modders: Creating your own Path++ mod

### Reference Paths++ in your mod

The easiest way to reference the PathsPlusPlus dll is to put the following within your .csproj file (BELOW where you import `btd6.targets`)

```xml
<ItemGroup>
    <Reference Include="PathsPlusPlus">
        <HintPath>$(BloonsTD6)\Mods\PathsPlusPlus.dll</HintPath>
    </Reference>
</ItemGroup>
```

<details>
<summary>Full .csproj example</summary>

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net6.0</TargetFramework>
        <RootNamespace>FourthPath</RootNamespace>
        <Configurations>Debug;Release</Configurations>
        <Nullable>enable</Nullable>
        <AssemblyName>FourthPath</AssemblyName>
        <LangVersion>latest</LangVersion>
        <Optimize>false</Optimize>
        <DebugType>embedded</DebugType>
    </PropertyGroup>
    
    <Import Project="..\btd6.targets" />
    
    <ItemGroup>
        <Reference Include="PathsPlusPlus">
            <HintPath>$(BloonsTD6)\Mods\PathsPlusPlus.dll</HintPath>
        </Reference>
    </ItemGroup>
    
</Project>
```

</details>

<details>
<summary>GitHub Actions</summary>

To download PathsPlusPlus within GitHub actions, add the following step: 

```yaml
- name: Download PathsPlusPlus
  uses: dawidd6/action-download-artifact@v6
  with:
    github_token: ${{ secrets.GITHUB_TOKEN }}
    workflow: build.yml
    branch: main
    name: PathsPlusPlus.dll
    repo: doombubbles/paths-plus-plus
    path: ${{ env.BLOONSTD6 }}/Mods/
```

</details>

You should also list Paths++ as a dependency in your ModHelperData, that will look something like this

```cs
public static class ModHelperData
{
    /* ... */
        
    public const string Dependencies = "doombubbles/paths-plus-plus";
}
```

### Create your PathPlusPlus

Each Path++ path begins with creating a very simple class just to register your path.
All it needs to specify is which tower the path is for, and how many upgrades your adding.

```cs
public class DartMonkeyFourthPath : PathPlusPlus
{
    public override string Tower => TowerType.DartMonkey;

    public override int UpgradeCount => 0; // Increase this up to 5 as you create your Upgrades
}
```

Similarly to `ModTowers` from Mod Helper, your `UpgradeCount` should reflect how many upgrades you've actually made so far.

### Extending a Vanilla Path

If you are instead want to add additional upgrades to a vanilla path, create your `PathPlusPlus` with an override for the `ExtendsVanillaPath` property like so.

```csharp
public class DartMonkeyTopPath : PathPlusPlus
{
    public override string Tower => TowerType.DartMonkey;
    
    public override int ExtendVanillaPath => Tops;

    public override int UpgradeCount => 6; // Adding one new upgrade to bring the total upgrades up to 6
}
```

In this case, you will be setting `UpgradeCount` to be the new maximum amount of upgrades that you want the path to go to.
For example, setting it to 6 means that you are adding 1 additional upgrade sixth tier upgrade after the tower's 5 included tiers.


### Create your UpgradePlusPlus(s)

For each upgrade you want in your path, you will define an `UpgradePlusPlus<T>` where the generic parameter `<T>` will be your `PathPlusPlus` class above.

```cs
public class BetterDarts : UpgradePlusPlus<DartMonkeyFourthPath>
{
    public override int Cost => 50;
    public override int Tier => 1;
    public override string Icon => VanillaSprites.ArmorPiercingDartsUpgradeIcon;

    public override string Description => "Darts can pop Frozen bloons";

    public override void ApplyUpgrade(TowerModel towerModel)
    {
        foreach (var damageModel in towerModel.GetDescendants<DamageModel>().ToArray())
        {
            damageModel.immuneBloonProperties &= ~BloonProperties.Frozen;
        }
        
        if (IsHighestUpgrade(towerModel))
        {
            // apply a custom display, if you want
        }
    }
}
```

There are many familiar properties shared from `ModUpgrade` that all function basically the same, i.e. `Cost`, `Tier`, `Icon`, `Description`, `DisplayName` and `ApplyUpgrade`.

If you do not specify a `Portrait`, the 000 tower portrait will be used.

If your upgrade adds an ability to the tower, override the `Ability` property to be true.

If you want to change the container background for your upgrade in the upgrade screen, you can override the `Container` property with a string GUID.
Some standard options you may want are the default `VanillaSprites.UpgradeContainerBlue`, `VanillaSprites.UpgradeContainerTier5`, or `VanillaSprites.UpgradeContainerParagon`.
Additionally, the `UpgradePlusPlus` class contains some some extra sprites that would be good options for Tier 6+ Upgrades including `UpgradeContainerPlatinum`, `UpgradeContainerDiamond` and `UpgradeContainerRainbow`.

### Upgrade effects

When implementing `ApplyUpgrade` code, it's important for you to try to make as few assumptions as possible in order to increase compatability and prevent bugs with other paths.

Whenever you can, use `GetDescendants` to easily affect all instances of a given Model type within the TowerModel.

For modifying something specific, retrieve it by name if possible rather than just relying on the order. (e.g. `weapons.FirstOrDefault(model => model.name == ...)` instead of `weapons[2]`)

### Applying to Multiple Towers

You can use the `MultiPathPlusPlus` and `MultiUpdatePlusPlus` classes for making paths that apply to multiple towers.

Apart from overriding `Towers` instead of `Tower`, usage will be the same

[![Requires BTD6 Mod Helper](https://raw.githubusercontent.com/gurrenm3/BTD-Mod-Helper/master/banner.png)](https://github.com/gurrenm3/BTD-Mod-Helper#readme)
