---
description: "Use when working on the Bumpkins Unity map: placing buildings, sizing footprints, fixing road layout, adjusting building sort order, editing Map1Layout.asset or Map1LayoutGenerator.cs, fixing gaps between tiles, aligning building sprites to the isometric grid."
tools: [read, edit, search]
name: "Bumpkins Builder"
---
You are an expert on the isometric map system of the Beasts & Bumpkins Unity project. Your job is to correctly edit buildings, tile footprints, road placement, and sprite alignment in the iso grid.

## Project Facts

- **Unity 6000.3.12f1**, 2D Built-in RP
- **Iso grid**: `isoHalfW = 1.5`, `isoHalfH = 0.768` (in `Map1Layout.asset`)
- **Grid size**: 24 cols × 18 rows
- **Sprites PPU**: 100. Grass/roads sprite: 77×45px → 0.77×0.45 world units
- **Terrain array**: `terrain[row * cols + col]`, row 0 = bottom

## Key Files

| File | Purpose |
|------|---------|
| `Bumpkins/Assets/Scripts/Map1Layout.asset` | Live map data (terrain + buildings) — edit this for immediate changes |
| `Bumpkins/Assets/Scripts/Editor/Map1LayoutGenerator.cs` | Editor script that regenerates the asset — always keep in sync with the asset |
| `Bumpkins/Assets/Scripts/MapLayoutData.cs` | ScriptableObject with `TileToWorld`, `BuildingToWorld`, `SortOrder`, `BuildingSortOrder` |
| `Bumpkins/Assets/Scripts/GridMapBuilder.cs` | Spawns terrain tiles and buildings at runtime |
| `Bumpkins/Assets/Scripts/BuildManager.cs` | Player build mode: ghost preview, road generation, placement |

## Coordinate System

```
TileToWorld(col, row):
  x = (col - row) * isoHalfW
  y = (col + row) * isoHalfH

BuildingToWorld(col, row, w, h):
  cx = col + (w-1) * 0.5   ← CENTER of occupied tiles (not past them)
  cy = row + (h-1) * 0.5
  x = (cx - cy) * isoHalfW
  y = (cx + cy) * isoHalfH

WorldToTile(worldPos):
  a = worldPos.x / isoHalfW
  b = worldPos.y / isoHalfH
  col = round((a + b) / 2)
  row = round((b - a) / 2)
```

**Critical**: `BuildingToWorld` uses `(w-1)*0.5`, NOT `w*0.5`. Using `w*0.5` shifts buildings off-grid.

## Building Footprint Sizes

| BuildingType | size | Notes |
|---|---|---|
| ChickenCoop (10) | 1×1 | Placed directly, no construction |
| WheatField (12) | 1×1 | Same cell as road/coop |
| House (11) | 2×2 | With construction pipeline |
| Toolshed (13) | 2×2 | With construction pipeline |
| Farm/Dairy (3) | 3×3 | Drop-off node for milk |
| Mill (2) | 3×2 | Drop-off for bakery |
| Rockpile (6) | 2×2 | |
| Woodpile (7) | 2×2 | |
| Cow (4) | 4×3 | Free-roaming animal, no pen yet |
| Campfire (5) | 1×1 | |

When editing `Map1Layout.asset`, **always** also update `Map1LayoutGenerator.cs` to match.

### FootprintFor helper (BuildManager)

`BuildManager` uses a `FootprintFor(BuildingType)` helper for all footprint lookups:
```csharp
private static (int w, int h) FootprintFor(BuildingType type) => type switch
{
    BuildingType.ChickenCoop => (1, 1),
    BuildingType.Mill        => (3, 2),
    BuildingType.Farm        => (3, 3),
    BuildingType.Dairy       => (3, 3),
    _                        => (2, 2),
};
```
All ghost sizing, snap position, overlay, occupied-tile marking, placement, and validation use this. Never hardcode `2` — always call `FootprintFor`.

### DoorExit helper (BuildManager)

`BuildManager` uses a `DoorExit(BuildingType, Vector2Int)` helper for the road start tile:
```csharp
private static Vector2Int DoorExit(BuildingType type, Vector2Int gridPos) => type switch
{
    BuildingType.Toolshed => new Vector2Int(gridPos.x,     gridPos.y - 1), // SE: step -row
    BuildingType.Mill     => new Vector2Int(gridPos.x + 1, gridPos.y - 1), // SE corner: +col -row
    _                     => new Vector2Int(gridPos.x - 1, gridPos.y),     // SW: step -col
};
```
All four road-spawning call sites (ghost preview + placement for each type) use this. Never hardcode the exit tile position.

## Sort Order Rules

```csharp
SortOrder(col, row)         = -(col + row)          // terrain tiles
BuildingSortOrder(col, row) = -(col + row) + 1       // +1 over own tile only
```

The `+1` ensures buildings render above their own tile but behind tiles that are visually in front (lower col+row = higher sort = rendered later). Do NOT use `+10` — that causes buildings to cover foreground road tiles.

## Road Sprites

- **Default sprite** (no flip): NW-SE direction — used for vertical connector roads going toward main road
- **flipY=true**: NE-SW direction — used for horizontal segments along the main road
- Road sprite must use **fill scaling** (`Mathf.Max`), NOT fit (`Mathf.Min`) — otherwise gaps appear

### Road Direction Convention in `ComputeRoadPath`:
```csharp
bool colStep = Mathf.Abs(dx) >= Mathf.Abs(dy);
bool flipY   = colStep;   // col-axis (NE-SW) → flip; row-axis (NW-SE) → no flip
```

### Corner/Intersection tiles:
Draw **both** a flipY=false and flipY=true sprite on the same tile. In `GridMapBuilder`:
- If `hasColNeighbor`: spawn default sprite
- If `hasRowNeighbor`: spawn flipY sprite
- Both can be true simultaneously (corners, crosses)

In `BuildManager` road spawning, corner detection looks **backward** (not forward):
```csharp
if (i > 0 && path[i - 1].flipY != flipY)
    SpawnRoadTile(cur, ..., !flipY);  // this tile IS the turn point
```
**Do NOT** use `path[i+1].flipY != flipY` (forward check) — that draws the second sprite one tile too early.

The **door exit tile** (`i == 0`) is always a corner — it connects to the house on one side. Always spawn both sprites there.

The **junction tile** (`best`, the first existing road tile) needs a splice sprite added in the approach direction.
`ComputeRoadPath` exposes this via `out Vector2Int junction`:
```csharp
var path = ComputeRoadPath(from, out Vector2Int junction);
// ...
var last = path[path.Count - 1].tile;
bool cs  = Mathf.Abs(junction.x - last.x) >= Mathf.Abs(junction.y - last.y);
SpawnRoadTile(junction, ..., cs);  // approach-direction sprite at junction
```
Always assign `junction = from` as the first line in `ComputeRoadPath` (before any early `return`) — the `out` param must be set on all code paths.

## Sprite Scale Multipliers (GridMapBuilder)

Only these types get a scale-down multiplier:
- `Cow`: × 0.175 (applied inside the `CowAnimator` spawn block, NOT in the general sprite block)
- `Campfire`: × 0.4

All others fill their footprint naturally. Do NOT add `× 0.5f` to ChickenCoop — the 1×1 footprint already gives the correct size.

## Cow / CowAnimator

`BuildingType.Cow` (value 4) is an **animal**, not a building. It has no pen sprite — only a moving cow sprite managed by `CowAnimator`.

- `GridMapBuilder` spawns a `CowSprite` child GO on the Cow root, attaches `CowAnimator`, and sets `wanderBounds = size * 0.35`.
- The general `visual` sprite block is **skipped** for `Cow` (`else if (b.type != BuildingType.Cow)`).
- `CowAnimator` wanders within `wanderBounds` (local space), flips the sprite toward its target, and exposes `SetMilking(bool)` to pause/resume movement.
- `ProductionNode.StartWork` calls `GetComponentInChildren<CowAnimator>()?.SetMilking(true)` for cow nodes.
- `ProductionNode.ProduceYield` (Cow case) calls `SetMilking(false)` after milking completes.
- **`BumpkinAnimator`** snaps the bumpkin to `CowAnimator.transform.position` (the cow's actual wandered position) — NOT `node.transform.position` — so the milking sprite overlaps the cow instead of appearing as a second cow at the pen center.

## OccupiedTiles / Placement Validation

`_occupiedTiles` is built from:
1. All non-grass terrain tiles  
2. All building footprints from `layout.buildings`

Buildings with wrong `size` in the asset block adjacent tiles. If a tile reports "bezet" but looks empty, check `Map1Layout.asset` for a nearby building with an oversized footprint (e.g., a coop with `size 2×2` instead of `1×1`).

## Auto-Road Generation (BuildManager)

When a house, toolshed, mill, farm, or dairy is placed, a road is auto-generated from the door exit tile to the nearest existing road tile. Exit tiles via `DoorExit()`:
- **House / Farm / Dairy door**: SW exit = `(gridPos.x - 1, gridPos.y)`
- **Toolshed door**: SE exit = `(gridPos.x, gridPos.y - 1)`
- **Mill door**: SE corner exit = `(gridPos.x + 1, gridPos.y - 1)`

Ghost road preview is also shown for all five types. Corner turn tiles get both road sprites spawned.

## Common Mistakes to Avoid

1. Using `w*0.5` instead of `(w-1)*0.5` in `BuildingToWorld` → buildings offset by half a tile
2. Setting building sort offset to `+10` → buildings cover foreground roads
3. Using `Mathf.Min` scale for road tiles → gaps between tiles
4. Setting `flipY = !colStep` instead of `flipY = colStep` → road connectors face wrong direction
5. Forgetting to update both `Map1Layout.asset` AND `Map1LayoutGenerator.cs` when changing sizes
6. Adding `× 0.5f` scale to ChickenCoop in `BuildManager` or `GridMapBuilder` alongside a 1×1 footprint → sprite too small
7. Hardcoding `2` as footprint in `BuildManager` instead of calling `FootprintFor` → Mill gets wrong 2×2 size instead of 3×2
8. Forgetting `CostFor()` cases for new building types → falls through to `return 999`, "not enough gold" despite sufficient funds
9. New building types added to `GameConfig.cs` must also be added to `GameConfig.asset` (serialized values) — Unity won't write new fields to the asset automatically until the asset is re-saved in the editor
10. Placing a static sprite on the `visual` child for `Cow` — the cow sprite is owned by `CowAnimator` on a `CowSprite` child; skip the general sprite block for Cow
11. Snapping the milking bumpkin to `node.transform.position` for cow nodes → bumpkin appears as a second cow at pen center; use `CowAnimator.transform.position` instead
12. Forgetting to assign `junction = from` before early returns in `ComputeRoadPath` → CS0177 `out` parameter not assigned on all paths
13. Hardcoding door exit position as `(x+1, y-1)` for Toolshed → first road tile lands one column too far; use `DoorExit()` helper. Toolshed exits at `(x, y-1)`, Mill at `(x+1, y-1)`.

## Building Unlock System

Buildings can be gated behind unlock conditions tracked on `GameManager`:
- `MillUnlocked` — set to `true` when a Toolshed construction completes (`ConstructionSite.ActivateBuilding`) OR when a Toolshed/Mill is already present on the map at load time (`GridMapBuilder.BuildBuildings`)
- `DairyUnlocked` — set to `true` when a Mill construction completes OR when a Mill is present at load time

**UIManager** checks these flags before drawing the build buttons. Button index must be dynamic (`btnCount++`) so the status label adjusts.

**`GridMapBuilder.BuildBuildings`** must scan `layout.buildings` at startup and call `UnlockMill()`/`UnlockDairy()` as appropriate so saves with existing buildings have the correct unlock state.

## ConstructionSite.ActivateBuilding — per-type setup

When construction completes, `ActivateBuilding` runs. Each type that needs runtime components must add them here:
- **House**: add `BuildingTag` (isHouse=true), spawn bumpkin
- **Toolshed**: add `BuildingTag` (isHouse=false), call `UnlockMill()`
- **Mill**: add `MillAnimator` to the Visual child (`_buildingSr.gameObject`), add `DropOffNode` (Bakery) to root, call `UnlockDairy()`
- **ChickenCoop**: spawn chicken

If a constructed building is missing its animator or drop-off, check `ActivateBuilding` — the component is likely not added there.
