# Dev Log — Beasts & Bumpkins Unity remake

---

## Sessie 4 — 2 april 2026 (auto-gedrag, baby-systeem, UI, touch, APK)

### Auto-harvest + auto-assign
- `ProductionNode.TryAutoAssign()`: zodra tarweveld klaar / koe klaar → nearest idle bumpkin wordt automatisch gestuurd
- `TryFindWork()` + `TryIdleActivity()` draaien na elke Idle-state
- Race condition opgelost: `TryReserve(bumpkin)` combineert `CanBeWorkedBy` + set `_occupied=true` atomisch
- Timers: tarwe groeien 180s, koe cooldown na melken 120s

### BumpkinAnimator overhaul
- Flip-animatie verwijderd, overlay-systeem verwijderd
- Working-state: bumpkin teleporteert naar `node.transform.position` (harvest/melk sprite op het veld)
- Sprites: `m_still/f_still` (idle), `m_harvest/f_harvest` (oogsten), `milking` (koe), `m_sack/f_sack` (tarwe sjouwen), `f_milk` (melk sjouwen), `kidm/kidf` (kind idle)
- Kind-detectie: monitort `isChild` change → herlaadt sprite

### Idle activiteiten (leisure)
- Roll 0–4 voor volwassen males, 0–3 voor vrouwen + kinderen:
  - 0: kampvuur · 1: gebouw binnengaan · 2: ei halen · 3: niets · 4: makeBaby (male only)
- MakeBaby: idle male zoekt idle female + vrij `isHouse`-gebouw → beiden lopen erheen

### Baby/kind systeem (`BabySystem.cs`)
- Female verstopt sprite bij aankomst huis, baby.png spawnt als child van huis op `localPosition(-0.5, -0.1, 0)`
- Na 4s: baby verdwijnt, female vrijgelaten, kind gespawnd op huispositie (schaal 0.55, `isChild=true`)
- Kind groeit op na 40s → `isChild=false`, scale=1.0f, kan werken
- `BuildingTag.isHouse=true` alleen op House; Toolshed heeft `isHouse=false`
- Tweede huis toegevoegd op `(6,13)` in `Map1LayoutGenerator`

### MVP UI (`UIManager.cs`)
- Pure `OnGUI`, geen Canvas / TMP → geen witte border artifacts
- Top-left: Gold / Bread / Milk / Eggs / Wheat labels
- Bottom-center: geselecteerde bumpkin + huidige state
- Freewill-knop top-right: groen=AAN / rood=UIT, alleen als bumpkin geselecteerd
- `IsPointerOverGUI` static bool → `ClickHandler` blokkeert wereld-klik als muis over knop

### Freewill toggle
- `BumpkinController.freeWill = true` veld
- Als `false`: `TryFindWork()` en `TryIdleActivity()` overgeslagen
- Na handmatig `MoveTo()`: `_playerMoved` flag + 4s `WaitThenIdle` voor auto-gedrag hervat

### Touch controls + camera (`CameraController.cs`)
- 1-vinger drag: world-space pan via `Input.GetTouch`
- 2-vinger pinch: zoom (`zoomMin=4`, `zoomMax=16`)
- `UnityEngine.TouchPhase` volledig gekwalificeerd (vermijdt InputSystem-ambiguïteit)
- Rechtermuisknop toegevoegd als pan-fallback naast middelmuisknop (voor Device Simulator)

### Gras tile gaps gefixed (`GridMapBuilder.cs`)
- `MakeSpriteFill()`: gebruikt `Mathf.Max(scaleX, scaleY) * 1.005f` → geen sub-pixel gaps meer

### APK build
- Stappen gedocumenteerd: Switch Platform → Android, Add Modules (SDK/NDK/JDK via Hub), Player Settings, Build → .apk
- Build & Run optie voor direct installeren via USB met Developer Mode + USB-debugging

---

## Sessie 3 — 2 april 2026 (polish + animaties)

### Isometrisch grid (afgerond)
- `MapLayoutData.cs` volledig omgebouwd naar iso-coördinaten (`isoHalfW=0.5`, `isoHalfH=0.256`)
- Nieuwe methoden: `TileToWorld()`, `BuildingToWorld()`, `SortOrder()`, `BuildingSortOrder()`
- `GridMapBuilder.cs` bijgewerkt: tiles + gebouwen via iso-methoden, gras altijd als achtergrond
- Roads spawnen met `flipY` op basis van buurdetectie (rij vs kolom)
- Compile error opgelost: `data.tileSize` verwijderd uit `Map1LayoutGenerator.cs`
- Camera startpositie aangepast naar iso-centrum `(1.5, 5.4, -10)`

### Map layout — map 1 bijgewerkt
- TownHall verwijderd (niet aanwezig in map 1)
- Farm en Dairy samengevoegd tot 1 Farm drop-off `(7,7)`
- WheatField × 2 (was × 4)
- ChickenCoop × 3 toegevoegd rechtsboven
- House + Toolshed toegevoegd
- FarmPlot en Wood terraintiles verwijderd (hoort niet in origineel spel)
- Bumpkins spawnen voor kampvuur in iso-coords `(0.7, 5.7)` + `(1.3, 5.6)`

### Sprites + schaal
- Alle gebouwen + bumpkins op scale 1.0 (was 0.8)
- House sprite import gerepareerd (spriteMode Multiple → Single, rect 134×123 → 217×147)
- Bumpkin collider radius 0.4 → 0.1

### Animaties
| Script | Beschrijving |
|---|---|
| `CampfireAnimator.cs` | Flames overlay + scale-flicker (3 sinussen) + kleurpuls + flipX |
| `ChickenAnimator.cs` | Bob + flip, ei na 10 sec als apart sprite naast kip, `CollectEgg()` API |
| `MillAnimator.cs` | Wiek-overlay op pos (0.6, 0.4), deur op (0.69, -0.36), trigger radius 0.01 opent/sluit bij bumpkin |

### Bekend / uitgesteld
- Wiekrotatie uitgeschakeld — sprite bevat bovenste helft molen + wieken, positie nog niet perfect
- Deur-trigger werkt via `OnTriggerEnter/Exit2D` met kinematic `Rigidbody2D` op mill root

---

## Sessie 1 — 2 april 2026 (±1 uur)

### Voortgang

#### Stap 1 — Project setup (grotendeels klaar)
- `winget install Unity.UnityHub` → Unity Hub 3.15.2 geïnstalleerd
- `winget install Unity.Unity.6000` → Unity 6000.3.12f1 gedownload (3.85 GB) + geïnstalleerd
- Editor handmatig gelocate in Unity Hub via **Installs → Locate**
- Unity project aangemaakt: `Unity_bumpkins/Bumpkins/` — template: 2D Built-in Render Pipeline
- Unity Version Control (Plastic SCM) overgeslagen — bestaande Git repo gebruikt
- Sprites uit `beastnbump/` (eerder geëxtraheerd) gesorteerd in 6 categorieën:
  - `Terrain/` 116 · `Buildings/` 116 · `Units/` 212 · `Animals/` 100 · `Effects/` 86 · `UI/` 249 · `Misc/` 371
- Scripts + sprites gekopieerd naar `Bumpkins/Assets/` (14 scripts, 1250 sprites)
- Unity importeert assets bij eerste opstart

#### Stap 1b — Map analyse (klaar)
- 6 YouTube gameplay screenshots opgeslagen in `screenshots/`
- ChatGPT Vision analyse uitgevoerd → grid 24×18, gebouwposities + terreintypen bepaald
- Fog of war deels aanwezig in screenshots → basis-layout voldoende voor MVP

### Scripts geschreven (klaar voor Unity)

| Script | Stap |
|---|---|
| `GameConfig.cs` | ScriptableObject — alle tunable waarden |
| `GameManager.cs` | Singleton — gold/bread/milk/eggs/happiness state |
| `BumpkinController.cs` | Click-to-move + resource carrying |
| `BumpkinClick.cs` | Klik op bumpkin → selecteer |
| `SelectionManager.cs` | Welke bumpkin is geselecteerd |
| `ProductionNode.cs` | Wheat/Cow harvest timer + click-to-assign |
| `DropOffNode.cs` | Bakery/Dairy drop-off bij aankomst |
| `ChickenCoop.cs` | Autonome ei-timer (elke 2 min) |
| `GroundClickHandler.cs` | Klik op grond → stuur bumpkin erheen |
| `UIManager.cs` | HUD — realtime resource display |
| `TileType.cs` | Enums TileType + BuildingType |
| `MapLayoutData.cs` | ScriptableObject — 24×18 terrain array + buildings |
| `GridMapBuilder.cs` | Spawnt tiles + gebouwen in scene vanuit layout |
| `Editor/Map1LayoutGenerator.cs` | Tools → Bumpkins → Generate Map1 Layout |

### Sessie 2 — voortgang (stap 2 + 3)
- [x] VS Code koppelen — al standaard ingesteld
- [x] Scene `Game` aangemaakt (Empty Scene)
- [x] `Tools → Bumpkins → Generate Map1 Layout` uitgevoerd → `Map1Layout.asset` aangemaakt
- [x] `GridMapBuilder` op `Map` object + `Map1Layout` gelinkt → placeholder kaart zichtbaar (groen/bruin/rood etc.)
- [x] Camera gecentreerd (12, 9, -10), size 10
- [x] Camera movement: WASD + scrollwiel (`CameraController.cs`, nieuw Input System)
- [x] Input System issue opgelost: `Input.GetKey` → `Keyboard.current` / `Mouse.current`
- [x] `BoxCollider2D` op gebouwen toegevoegd via `GridMapBuilder`
- [x] Bumpkins gespawnd via `TestBumpkinSetup` (male blauw @ 10,9 · female roze @ 12,9)
- [x] `ClickHandler` centrale click-handler (linksmuisklik = select/move, rechtsmuis = deselect)
- [x] Male bumpkin pre-geselecteerd bij Play start
- [x] Click-to-move werkend — bumpkin loopt naar klikpositie en stopt netjes
- **Beslissing:** pathfinding/bewegingsblokkering → uitgesteld naar polish

### Nog te doen
- [ ] Stap 4: WheatNode + CowNode plaatsen, bumpkin harvesten
- [ ] Stap 5: Bakery/Dairy drop-off
- [ ] Stap 6: Shops (gold → bread/milk/egg)
- [ ] Stap 7: Gold per level via config
- [ ] Stap 8: Happiness meter
- [ ] Stap 9: Buyable buildings
- [ ] Stap 10: HUD UI
