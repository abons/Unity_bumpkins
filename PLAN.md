# Jam Plan — Beasts & Bumpkins (Unity remake)

Tijdsinschatting: ±8–10 uur  
Zie [DESIGN.md](DESIGN.md) voor core loops, data model en entities.

**Status na 6 uur:** stap 1–3 klaar + polish animaties. Resterende tijd: ±2–4 uur.  
**Update:** stap 4 + 5 grotendeels klaar. Baby/kind systeem gebouwd (bonus). Stap 10 (UI) is de resterende prioriteit.  
**Update 2:** MVP COMPLEET ✅ — touch controls, freewill toggle, baby/kind systeem, OnGUI HUD, gras gap fix, APK build instructies klaar.  
**Update 3:** Build mechanic uitgebreid — Mill + Dairy ontgrendelsysteem, constructie-pipeline voor Mill, BFS wegpathfinding rond obstakels.
**Update 4:** Wegalignment gecorrigeerd — Toolshed/Mill deur-exit tiles aangepast, `DoorExit()` helper toegevoegd, Farm/Dairy krijgen nu ook ghost road preview en auto-weg bij plaatsing. Dairy footprint 3×3 expliciet.
**Update 5:** Wolf enemy basisimplementatie klaar — `WolfController.cs` (Roaming/Hunting/Attacking/Dead), bumpkin death sequence met `d_male`/skeleton sprites, `IsDead` guard, selection clearing bij dood.

---

## Regels (jam)
- Eerst werkend > dan mooi
- Hardcoded values oké, maar wel centraal
- 1 feature tegelijk
- Geen perfectionisme

---

## Stap 1) PROJECT SETUP (±30 min)
- [x] Unity 6000.3.12f1 geïnstalleerd via winget
- [x] Unity Hub geïnstalleerd + ingelogd
- [x] Unity project aangemaakt: `Bumpkins/` (2D Built-in RP)
- [x] VS Code gekoppeld (al standaard ingesteld)
- [x] GitHub Copilot actief in VS Code
- [x] Sprites gesorteerd in `assets/Sprites/` (6 categorieën, 1250 files)
- [x] Scripts + sprites gekopieerd naar `Bumpkins/Assets/`
- [x] Scene aangemaakt: `Game`

**DoD**
- Project opent zonder errors
- Er is één scene die je kan runnen met een camera en een lege "Game" root

---

## Stap 1b) MAP ANALYSE MET VISION AI (±30 min)
- [x] Screenshot van map 1 gemaakt vanuit YouTube gameplay — opgeslagen in `screenshots/`
- [x] Screenshots aangeleverd aan ChatGPT Vision (GPT-4o)
- [x] Grid analyse ontvangen: 24×18 tiles, gebouwposities + terrein in kaart
- [x] Layout omgezet naar `Map1LayoutGenerator.cs` + `MapLayoutData.cs` + `GridMapBuilder.cs`
- [ ] ~~Assets koppelen: prefabs aanmaken voor tile types en gebouwen~~ → **uitgesteld naar polish**, placeholders werken functioneel

**DoD**
- Er is een beschreven of visuele grid-layout van map 1 die als referentie dient
- Elk aanwezig gebouw/terrain-type is gekoppeld aan een bijbehorende sprite uit `Assets/`

---

## Stap 2) WERELD (±1 uur)
- [x] Grid 24×18 gegenereerd via `GridMapBuilder` + `Map1Layout` asset
- [x] Gekleurde placeholder tiles: gras, weg, farm, rots, hout
- [x] Gebouwen als gekleurde blokken op correcte posities (Mill, Farm, CowPen etc.)
- [x] Camera gecentreerd op map — iso center (1.5, 5.4) · size 10
- [x] Camera movement (scroll/zoom)
- [x] `BoxCollider2D` op gebouwen voor click-detectie
- [x] **Isometrisch grid** geïmplementeerd — `TileToWorld()` / `BuildingToWorld()` / `SortOrder()` in `MapLayoutData`
- [x] Sprites op iso grid: gras als achtergrond, roads met flip op basis van buren
- [x] Alle gebouwsprites geladen via `Resources/Sprites/` — schaal 1.0
- [x] Bumpkins spawnen voor kampvuur in iso-coördinaten
- **Beslissing:** bewegingsblokkering (pathfinding) → uitgesteld naar polish, bumpkins lopen door gebouwen heen (MVP acceptabel)

**DoD**
- Je kunt rondkijken (pan/zoom) en ziet duidelijk de speelruimte

---

## Stap 3) INPUT + BUMPKIN MOVEMENT (±1 uur)
- [x] Click input: world position via `ClickHandler` + `Mouse.current` (nieuw Input System)
- [x] Klik op ground → geselecteerde bumpkin beweegt naar target
- [x] Stopafstand zodat bumpkin niet "trilt" op target
- [x] Male bumpkin pre-geselecteerd bij Play start
- [x] Selectie wisselen door op andere bumpkin te klikken
- [x] Rechtermuis klik → bumpkin deselected

**DoD**
- Klik bumpkin: bumpkin selected ✅
- Klik ergens: selected bumpkin loopt daarheen en stopt netjes ✅
- Debug/log toont: huidige target + state ✅

---

## Stap 4) PRODUCTION NODES — 🎯 PRIORITEIT (±1 uur)

### Wheat/GrainNode
- [x] Click op WheatField → bumpkin gaat erheen
- [x] Harvest timer (5 sec) → bumpkin draagt tarwe (m_sack / f_sack sprite)
- [x] Beide geslachten mogen harvesten
- [x] Groei-timer 3 min, veld toont WheatField_grown als klaar
- [x] Idle bumpkin auto-assigned zodra veld klaar is
- [x] Race condition fix: node gereserveerd bij toewijzing, niet bij aankomst

### Cow (Milking)
- [x] Bumpkin gaat naar CowPen
- [x] Melk timer → bumpkin draagt melk (f_milk sprite)
- [x] Alleen `female`
- [x] Koe heeft 2 min cooldown na melken

### Chicken (Egg collection)
- [x] `ChickenAnimator.cs` — kip legt ei na 10 sec
- [x] Ei zichtbaar naast kip, `CollectEgg()` API klaar
- [x] Idle bumpkin gaat ei halen als leisure-activiteit

**DoD**
- Ticks/timers gebeuren op timer, niet per frame ✅
- Verkeerde bumpkin type geeft duidelijke feedback (log) ✅

---

## Stap 5) DROP-OFF + PRODUCTION — 🎯 PRIORITEIT (±1 uur)

### Mill (Wheat → Bread)
- [x] Drop-off: bumpkin met tarwe → Mill → `GameManager.ProcessWheatAtBakery()`
- [x] Bumpkin auto-loopt naar Mill na harvest (FindNearestDropOff)

### Farm (Milk drop-off)
- [x] Drop-off: bumpkin met melk → Farm → `GameManager.AddMilk()`
- [x] Bumpkin auto-loopt naar Farm na melken

**DoD**
- Drop-off triggert precies 1 keer per delivery ✅
- Bread productie klopt: `1 wheat` → `+3 bread` ✅

---

## Stap 10) UI — ✅ KLAAR
- [x] Toon `gold`, `bread`, `milk`, `eggStock`, `wheat`
- [x] Realtime updates via `GameManager`
- [x] Geselecteerde bumpkin + state onderin
- [x] Zelfbouwende Canvas — geen inspector-wiring nodig

**DoD**
- Getallen kloppen na harvest/drop-off
- Geen rode errors in Console

---

## BONUS — Baby/Kind systeem (gebouwd)
- [x] MakeBaby actie: idle male + idle female → samen naar huis
- [x] Baby.png verschijnt voor huisdeur (4 sec), female mag niet weg
- [x] Na baby: kind spawnt (kidm/kidf sprite, schaal 0.55)
- [x] Kinderen werken niet, mogen wel leisure-activiteiten
- [x] Kind groeit op na 40 sec → volwassene, kan werken

---

---

## Stap 11) BUILD MECHANIC — 🎯 VOLGENDE PRIORITEIT (±1.5 uur)

> Zonder bouwmechaniek is het geen RTS. Dit is de volgende core feature.

### Concept
- Speler spendeert `gold` om gebouwen te plaatsen op het iso-grid
- Build mode: klik bouw-knop in UI → cursor toont ghost-preview → klik op geldig tile → bouw geplaatst

### Bouwbare gebouwen (v1 → v2)
| Gebouw      | Kosten | Levert                          | Ontgrendeld na |
|-------------|--------|---------------------------------|----------------|
| House       | 200g   | Spawn extra bumpkin             | altijd |
| ChickenCoop | 100g   | Nieuwe kip + ei-productie       | altijd |
| Toolshed    | 175g   | Productie-hub                   | altijd |
| Mill        | 400g   | Wheat drop-off + deuranimatie   | Toolshed gebouwd |
| Dairy       | 300g   | Milk drop-off                   | Mill gebouwd |

### Implementatie
- [x] `BuildManager.cs` — singleton, houdt `inBuildMode` bij + gekozen gebouwtype
- [x] `BuildMenu` in UI — 3 knoppen: House / WheatField / ChickenCoop (toon goudkosten)
- [x] `GhostPreview` — semi-transparant sprite volgt muispositie, snap naar iso-grid
  - Groen = geldig (gras-tile, niet bezet)
  - Rood = ongeldig (gebouw, weg, out-of-bounds)
- [x] Klik op geldig tile → `BuildManager.PlaceBuilding(gridPos, type)`
  - Trekt gold af (`GameManager.Buy()`)
  - Spawnt gebouw-sprite op iso-positie (zelfde flow als `GridMapBuilder`)
  - Registreert tile als bezet zodat overlap geblokkeerd wordt
  - Ongeldig tile of onvoldoende gold → ghost blijft rood / GM logt warning
- [x] Rechtermuisknop / Escape → bouw mode annuleren
- [x] House placement → `BuildManager.SpawnBumpkin()` direct na plaatsing
- [x] WheatField placement → instantieer `ProductionNode` (type Wheat, startState grown=false)
- [x] ChickenCoop placement → instantieer `ChickenAnimator`
- [x] Mill/Dairy ontgrendelsysteem — Mill zichtbaar na Toolshed gebouwd, Dairy na Mill gebouwd
- [x] Mill placement → constructie-pipeline → `MillAnimator` + `DropOffNode(Bakery)` na voltooiing
- [x] `FootprintFor(BuildingType)` helper in `BuildManager` — Mill 3×2, Farm/Dairy 3×3, ChickenCoop 1×1, rest 2×2
- [x] BFS wegpathfinding — weg loopt om bestaande gebouwen heen ipv erdoorheen
- [x] Ghost road preview weert ook ghost-gebouw-footprint als obstakel
- [x] `DoorExit(BuildingType)` helper — correcte deur-exit tile per type (Toolshed SE, Mill SE-hoek, rest SW)
- [x] Mill en Dairy krijgen ghost road preview + auto-weg bij plaatsing

### Validatieregels (simpel)
- Alleen op `TileType.Grass` tiles
- Tile mag niet al bezet zijn (gebruik `occupiedTiles: HashSet<Vector2Int>` in `BuildManager`)
- Gold moet >= bouwkosten zijn

**DoD**
- Klik "House" → goldkosten zichtbaar → klik gras tile → huis verschijnt → nieuwe bumpkin spawnt
- WheatField en ChickenCoop werken als nieuwe productie-nodes
- Ongeldige plaatsing geeft visuele feedback, geen exception
- Rechtermuisknop annuleert build mode

---

## Stap 12) WOLF ENEMY (±1.5 uur)

> Assets aanwezig: `wolf.png` (lopen), `wolfstil.png` (stilstaan), `wolfatta.png` (aanvallen), `wolfdead.png` (dood)

### Concept
- Wolf spawnt periodiek vanaf de kaartrand
- Zoekt het dichtstbijzijnde doelwit: bumpkin of dier (koe)
- Valt aan → doodt doelwit na een paar seconden
- Bumpkin "vecht" de wolf → wolf of bumpkin sterft

### `WolfController.cs` — state machine
```
Spawning → Roaming → Hunting → Attacking → [Fleeing | Dead]
```
- **Roaming**: beweegt willekeurig over de kaart (langzaam), sprite `wolfstil` / `wolf`
- **Hunting**: zodra bumpkin/dier binnen zoekradius (6 tiles) → loopt snel naar doelwit, sprite `wolf`
- **Attacking**: binnen aanvalsradius (0.8 tiles) → animeer `wolfatta`, timer 3 sec → doelwit dood
- **Fleeing**: bumpkin aangestuurd op wolf → wolf vlucht naar kaartrand, sprite `wolf` (flip), verdwijnt bij rand
- **Dead**: `wolfdead` sprite, 1 sec delay → destroy

### Implementatie
- [x] `WolfController.cs` — `MonoBehaviour`, state machine: Roaming → Hunting → Attacking → Dead
  - `FindTarget()` — zoek dichtste bumpkin of koe binnen `huntRadius = 3f`
  - `moveSpeed = 2f` (roaming), `chaseSpeed = 3.5f` (hunting)
  - Sprite-swap op state: wolfstil/wolf/wolfatta/wolfdead
  - `flipX = dir.x > 0f` (sprite faces SW by default)
  - Sort order: `-y / 0.256 + 50` (altijd boven terrain)
- [x] 1 wolf gespawnd via `GridMapBuilder.SpawnWolf()` op tile (1,8)
- [x] `BumpkinController.TakeDamage()` → death sequence: Dying → DeadLying → DeadSkeleton
- [x] `IsDead` property — blokkeert `MoveTo()`, `Update()` beweging en click-selectie
- [x] `SelectionManager.DeselectIfSelected()` — deselecteert bij dood
- [x] **Aanval op koe**: `Destroy(target.gameObject)`
- [ ] `WolfSpawner.cs` — spawnt wolf elke 60 sec vanaf kaartrand
- [ ] **Speler klik op wolf**: geselecteerde bumpkin jaagt wolf weg
- [ ] **Wolf health / Fleeing**: wolf vlucht na hit door bumpkin
- [ ] **UI feedback**: "Wolf nadert!" waarschuwingstekst

### Sprite mapping (Resources/Sprites/Animals/)
| State     | Sprite                    |
|-----------|---------------------------|
| Roaming   | `wolfstil` / `wolf`       |
| Hunting   | `wolf`                    |
| Attacking | `wolfatta`                |
| Fleeing   | `wolf` (gespiegeld)       |
| Dead      | `wolfdead`                |

**DoD**
- Wolf spawnt zichtbaar aan kaartrand elke ~60 sec
- Wolf loopt naar dichtstbijzijnde bumpkin of dier
- Wolf valt aan → doelwit verdwijnt na 3 sec
- Klik op wolf met geselecteerde bumpkin → bumpkin loopt erop af → wolf vlucht weg
- Geen NPE/exceptions als alle bumpkins dood zijn

---

## Stap 6–9) STRETCH GOALS (alleen als tijd over)

### Stap 6 — Shops
- [ ] Bread/milk/egg kopen voor gold

### Stap 7 — Gold per level
- [ ] `GameConfig` met startgold + income per tick

### Stap 8 — Happiness
- [ ] Meter 0–100, beïnvloedt door pricing

### Stap 9 — Verdediging uitbreiden
- [ ] Guard bumpkin type met hogere aanvalskracht
- [ ] Toren bouwen (via build mechanic) die automatisch wolven aanvalt

## Einddoel (MVP checklist)
- [x] Bumpkin beweegt
- [x] Male/female harvesten wheat automatisch
- [x] Female milkt cow (met cooldown)
- [x] Wheat → mill → bread (+3 per wheat)
- [x] Milk → farm (drop-off)
- [x] Kip produceert ei + idle bumpkin haalt op
- [x] UI toont resources  ✅ **MVP COMPLEET**
- [x] Build mechanic: House / WheatField / ChickenCoop / Mill / Dairy bouwen voor gold ✅
- [x] Wolf enemy: spawnt, roamt, jaagt, valt aan — bumpkin death sequence compleet ✅
- [ ] Wolf spawner (periodiek vanaf kaartrand)
- [ ] *(stretch)* Bread/milk/egg kopen voor gold
- [ ] *(stretch)* Happiness meter
- [ ] *(stretch)* Guard bumpkin / verdedigingstoren

---

## Als tijd over (polish, max 30 min per item)
- [x] Kampvuur animatie (`CampfireAnimator`) — flicker + kleurpuls + flames overlay
- [x] Kip animatie (`ChickenAnimator`) — bob, flip, ei-productie
- [x] Molen deur animatie (`MillAnimator`) — opent/sluit bij bumpkin trigger
- [ ] Wieken molen — sprite beschikbaar (`MillSails`), rotatie uitgeschakeld tot positie klopt
- [ ] Highlight target / geselecteerde node
- [ ] Bumpkin sprite flip left/right
- [ ] 2 SFX: harvest + drop-off
- [ ] Pathfinding / bewegingsblokkering (bumpkins lopen nu door gebouwen)- [x] Touch controls: 1-vinger pan, 2-vinger pinch zoom (`CameraController`)
- [x] Rechtermuisknop + middelmuisknop pan (editor/simulator fallback)
- [x] Freewill toggle per bumpkin (OnGUI knop top-right)
- [x] Gras tile gaps gefixed (`MakeSpriteFill` + 1.005 overscale)
- [x] Click-through fix (muisklik over UI-knop blokkeert wereld-klik)