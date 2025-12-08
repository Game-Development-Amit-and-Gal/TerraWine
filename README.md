# TerraWine 

TerraWine is a single-player strategy and management game where you play as the owner of a small winery competing in regional wine competitions.

Your goal is to build the most successful winery you can within three in-game years:
- Produce as many wine bottles as possible.
- Improve the quality of your wines.
- Increase the reputation and publicity of your winery.
- Beat the other vineyards around you in the recurring wine competitions.

Every in-game year is divided into three seasons. At the end of each three-season cycle, a wine competition takes place. After three full years, the final competition decides the overall winner.

## World & Core Loop

You are the owner of a vineyard and winery:

- **Vineyard (Field):**  
  Grow different grape varieties and manage your plots.

- **Cellar & Barrels:**  
  Age your wine in barrels, control quality, and prepare bottles.

- **Selling:**  
  Sell bottles to a supplier to earn money and unlock more options and resources.

- **World Map & Risky Actions:**  
  To get more resources you can:
  - Take risky actions like trying to steal from other wineries.
  - Explore and look for resources on your own.
  - Take temporary jobs to earn extra income.

But you are not alone:
- **Other wineries can also try to steal from you**, damage your progress, or get an advantage before the next wine competition.

Use your resources, timing, and decisions wisely over three in-game years and three recurring competitions to try and become the best winery in the region.


## Screenshots


![Main menu](Docs/Images/main_menu.png)
![Vineyard scene](Docs/Images/vineyard_scene.png)
![Barrel room](Docs/Images/barrel_room.png)

## Gameplay Overview

In TerraWine you manage a small vineyard and winery across several seasons:

- Plant different grape varieties (Cabernet Sauvignon, Grenache, Petit Verdot, etc.).
- Harvest grapes at the right time to balance quantity and quality.
- Age your wine in barrels and track how many bottles you can produce.
- Sell bottles from the wine truck to earn coins and improve your economy.
- Upgrade your vineyard, cellar and designs to increase efficiency and prestige.
- Compete in recurring wine competitions against nearby wineries.
- Take risks on the world map: explore, take side jobs, or even try to steal resources –  
  but remember that **other wineries can also steal from you**.


## Controls



| Action                     | Keyboard / Mouse                 |
|----------------------------|----------------------------------|
| Move                       | Arrow keys                       |
| Open truckInventory        | E                                |
| Open Bag / Shop / World Map| Click UI buttons with the mouse  |

## Features

- Plant and harvest multiple grape varieties (Cabernet Sauvignon, Grenache, Petit Verdot, etc.).
- Barrel and cellar system that turns harvested grapes into wine bottles over time.
- Wine truck for selling bottles and earning coins.
- Inventory system with clear categories (Resources, Wine Bottles, Design, etc.).
- Economy system that tracks player coins, income and upgrades.
- Recurring wine competitions every three seasons, with a final competition after three years.
- World map with risky actions: explore, take side jobs, or try to steal from other wineries.
- Other wineries can also steal from you, forcing you to plan your strategy carefully.
- 2.5D vineyard world with fields, cellar and surrounding wineries.

## Tech Stack

- **Engine:** Unity 6 (2025.x)
- **Language:** C#
- **Target Platform:** PC (Windows)
- **Version Control:** Git + GitHub
- **Distribution:** itch.io https://amit-and-gal.itch.io/terrawine


### Option 1 – Play the build

1. Download the latest release from the **Releases** page.
2. Extract the zip file.
3. Run `TerraWine.exe`.

### Option 2 – Open the project in Unity

1. Clone this repository:
   ```bash
   git clone https://github.com/Game-Development-Amit-and-Gal/TerraWine
   ```
2. Open the folder in **Unity 6 (2025.x)**.
3. Open `Assets/Scenes/MainMenu.unity`.
4. Press **Play** in the Unity editor.

## Project Structure

```text
Assets/
  _Recovery/                  # Unity auto-recovery data (not game logic)
  Editor/                     # Editor-related scripts/settings (if used)     
  Materials/                  # Shared materials for the project

  Photo/                      # Reference / in-game photos
    Garden/
      Graps/                  # Grapes / vineyard images
    Materials/              # Extra materials/textures
    Wine_Bottle/            # Wine bottle images
    item/                   # Sead image

  PlayerPrefsEditor/          # Third-party tool for editing PlayerPrefs
    Documentation/
    Editor/
    Editor Resources/
    Samples/
  PreFabs/                    # Reusable prefabs (trees, props, UI, etc.)

  Resources/
    Items/                    # ScriptableObject item definitions (seeds, bottles, etc.)

  Scenes/                     # Main scenes of the game
    (MainMenu, Garden, Wine_Scene, WorldMap, etc.)

  Scripts/                    # All gameplay C# scripts
    Garden/                   # Planting, plots, trees and garden logic
    Inventory/                # Inventory system & UI
    MainMenu/                 # Main menu, intro and start-game logic
    SceneManagement/          # GameManager, scene loading and global flow
    Shop/                     # Shop / truck / selling systems
    Wine_Scenes/              # Wine making, barrels and cellar logic
    WorldMap/                 # World map movement and events

  Settings/
    Scenes/                   # Unity 6 scene settings/assets
    Sprites/                  # Sprite collections / atlases
    StreamingAssets/          # Assets loaded at runtime

  TextMesh Pro/               # TMP fonts and resources
    Examples & Extras/
    Fonts/
    Resources/

  Tile/                       # Tilemap assets for the 2.5D world (if used)
```


### Key Scripts

- **GameManager** – controls overall game flow and holds `GameData`.  
  [View GameManager.cs][(https://github.com/Game-Development-Amit-and-Gal/TerraWine/blob/main/Assets/Scripts/MainMenu/GameManager.cs)

- **GameData** – stores persistent data like years, seasons, coins, and competition state.  
  [View GameData.cs](https://github.com/Game-Development-Amit-and-Gal/TerraWine/blob/main/Assets/Scripts/MainMenu/GameData.cs)

- **SeasonManager** – manages seasons and triggers wine competitions.  
  [View SeasonManager.cs](https://github.com/Game-Development-Amit-and-Gal/TerraWine/blob/main/Assets/Scripts/MainMenu/SeasonManager.cs)

- **EconomyManager** – handles player coins, prices and income.  
  [View EconomyManager.cs](https://github.com/Game-Development-Amit-and-Gal/TerraWine/blob/main/Assets/Scripts/MainMenu/EconomyManager.cs)

- **InventoryManager** – manages all items the player owns and notifies the UI when something changes.  
  [View InventoryManager.cs](https://github.com/Game-Development-Amit-and-Gal/TerraWine/blob/main/Assets/Scripts/Inventory/InventoryManager.cs)

- **InventoryUI** – draws the inventory grid and category filters for the player.  
  [View InventoryUI.cs](https://github.com/Game-Development-Amit-and-Gal/TerraWine/blob/main/Assets/Scripts/Inventory/InventoryUI.cs)

- **ItemSO** – ScriptableObject definition for seeds, wine bottles, designs and more.  
  [View ItemSO.cs](https://github.com/Game-Development-Amit-and-Gal/TerraWine/blob/main/Assets/Scripts/Inventory/ItemSO.cs)

- **TruckSeller** – allows selling wine bottles from the truck to earn coins.  
  [View TruckSeller.cs](https://github.com/Game-Development-Amit-and-Gal/TerraWine/blob/main/Assets/Scripts/Garden/TruckSeller.cs)




## Contributing

TerraWine is currently developed as a student project by a small team.

If you want to suggest improvements or report bugs, you can:

1. Open an **Issue** in this repository.
2. (Optional) Fork the repo and submit a **Pull Request** with a clear description of your changes.

Please keep commits small and focused, and add comments to any complex game logic you change.

## Credits

>

- **Gal Maymon** – Game design, programming, art direction.
- **Amit Nachum** – Programming, systems design.

