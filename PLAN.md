# Jam Plan — Beasts & Bumpkins (Unity remake)

Tijdsinschatting: ±8–10 uur  
Zie [DESIGN.md](DESIGN.md) voor core loops, data model en entities.

---

## Regels (jam)
- Eerst werkend > dan mooi
- Hardcoded values oké, maar wel centraal
- 1 feature tegelijk
- Geen perfectionisme

---

## Stap 1) PROJECT SETUP (±30 min)
- [ ] Unity project aanmaken
- [ ] VS Code koppelen
- [ ] GitHub Copilot activeren
- [ ] Sprites importeren
- [ ] Scene aanmaken

**DoD**
- Project opent zonder errors
- Er is één scene die je kan runnen met een camera en een lege "Game" root

---

## Stap 1b) MAP ANALYSE MET VISION AI (±30 min)
- [ ] Screenshot van map 1 maken vanuit gameplay-opname of `MAP1.VID` (bijv. via VLC of DOSBox screenshot) — `m1back.png` is de briefing-achtergrond, niet de speelkaart
- [ ] Screenshot aanleveren aan Vision AI (bijv. ChatGPT / Copilot Vision)
- [ ] Prompt: *"Analyseer deze game map. Beschrijf de tile-layout, gebouw-posities, paden en terrein. Geef een raster-indeling (bijv. 20×15) met labels per cel zodat ik dit kan nabouwen in Unity met de meegeleverde sprites."*
- [ ] Resulterende grid/layout gebruiken als bouwtekening voor de Unity scene
- [ ] Assets koppelen: gebruik geïmporteerde sprites uit `Assets/` om gebouwen, tiles en decoraties te plaatsen conform de analyse

**DoD**
- Er is een beschreven of visuele grid-layout van map 1 die als referentie dient
- Elk aanwezig gebouw/terrain-type is gekoppeld aan een bijbehorende sprite uit `Assets/`

---

## Stap 2) WERELD (±1 uur)
- [ ] Tilemap/grid aanmaken (of simpele ground plane; kies 1)
- [ ] Terrain plaatsen (grass etc.) op basis van Vision AI map-analyse (zie stap 1b)
- [ ] Camera movement (scroll/zoom)

**DoD**
- Je kunt rondkijken (pan/zoom) en ziet duidelijk de speelruimte

---

## Stap 3) INPUT + BUMPKIN MOVEMENT (±1 uur)
- [ ] Click input: world position bepalen (raycast / screen→world)
- [ ] Klik op ground → bumpkin beweegt naar target
- [ ] Stopafstand zodat bumpkin niet "trilt" op target

**DoD**
- Klik ergens: bumpkin loopt daarheen en stopt netjes
- Debug/log toont: huidige target + state

---

## Stap 4) PRODUCTION NODES (±1–2 uur)

### Wheat/GrainNode
- [ ] Wheat/GrainNode object plaatsen
- [ ] Interactie: click op wheat node → bumpkin gaat erheen
- [ ] Alleen `male`/`female`: start `HarvestingWheat`
- [ ] Simpele timer/tick implementeren (deterministisch)

### Cow (Milking)
- [ ] Cow object plaatsen
- [ ] Interactie: click op cow → bumpkin gaat erheen
- [ ] Alleen `female`: start `MilkingCow`
- [ ] Simpele timer/tick implementeren (deterministisch)

### Chicken (Egg production)
- [ ] Chicken/ChickenCoop plaatsen
- [ ] Timer: elke 120 seconden `eggStock += 1`

**DoD**
- Ticks/timers gebeuren op timer, niet per frame
- Verkeerde bumpkin type geeft duidelijke feedback (log/UI)

---

## Stap 5) DROP-OFF + PRODUCTION (±1–2 uur)

### Bakery/Mill (Wheat → Bread)
- [ ] Bakery/Mill plaatsen
- [ ] Drop-off: wheat wordt ingeleverd bij aankomst
- [ ] Productie: per wheat drop-off produceer je **+3 bread**

### Farm/Dairy (Milk drop-off)
- [ ] Farm/Dairy plaatsen
- [ ] Drop-off: milk wordt ingeleverd bij aankomst (MVP: opslaan/tellen)

**DoD**
- Drop-off triggert precies 1 keer per delivery
- Bread productie klopt: `1 wheat` → `+3 bread`

---

## Stap 6) SHOPS: KOPEN VOOR GOLD (±1 uur)
- [ ] Bread shop: koop bread voor gold (`breadPriceGold`, default 100)
- [ ] Milk shop: koop milk voor gold (`milkPriceGold`, default 100)
- [ ] Egg shop: koop egg voor gold (`eggPriceGold`, default 100) en alleen als `eggStock > 0`

**DoD**
- Als `gold >= prijs`: gold daalt, item stijgt exact 1 keer per aankoop
- Egg aankoop: `eggStock` daalt exact met 1
- Als `gold < prijs` of `eggStock == 0`: aankoop faalt met duidelijke feedback

---

## Stap 7) GOLD PER LEVEL (±30–60 min)
- [ ] `LevelConfig`/`GameConfig` waarin gold sources per level instelbaar zijn
- [ ] Start gold + (optioneel) income per tick

**DoD**
- Gold start/income verschilt per level door config, niet hardcoded verspreid

---

## Stap 8) HAPPINESS (±1 uur)
- [ ] `happiness` meter (0–100)
- [ ] Over-time update (tick)
- [ ] Happiness wordt beïnvloed door pricing (bread/milk/egg), per source instelbaar

**Jam-safe formule (voorbeeld)**
- Elke tick:
  - `happiness += baseDelta`
  - `happiness += priceImpact(breadPriceGold, milkPriceGold, eggPriceGold)`
  - clamp naar 0–100

**DoD**
- Happiness verandert over time
- Prijs aanpassen heeft zichtbaar effect (log/UI)

---

## Stap 9) BUILDING / BUYABLES (±1–2 uur)
- [ ] Minimaal: `House` kopen/plaatsen
- [ ] Ook buyable: `WheatField/Farm`, `Chicken/ChickenCoop`, `Bakery/Mill`, `Farm/Dairy`
- [ ] Building costs in gold (hardcoded maar centraal)
- [ ] Plaatsen op world (simpel: click om te plaatsen)

**DoD**
- Je kunt alleen kopen/plaatsen als je genoeg gold hebt
- Bij aankoop/plaatsing: gold daalt exact met de cost
- Object verschijnt 1 keer en blijft staan

---

## Stap 10) UI (±1 uur)
- [ ] Toon minimaal `gold`, `bread`, `milk`, `eggStock`, `happiness` (optioneel `wheat`)
- [ ] Realtime updates

**DoD**
- UI klopt na harvest/drop-off/kopen/bouwen
- Geen rode errors in Console

---

## Einddoel (MVP checklist)
- [ ] Bumpkin beweegt
- [ ] Male/female harvesten wheat
- [ ] Female milkt cow
- [ ] Wheat → bakery/mill → bread (+3 per wheat)
- [ ] Milk → farm/dairy (drop-off) + milk kopen voor gold
- [ ] Chicken produceert egg elke 2 minuten (stock) + eggs kopen voor gold
- [ ] Bread/milk/egg base cost 100, adjustable per source
- [ ] Gold sources per level via config
- [ ] Happiness meter werkt en reageert op pricing
- [ ] Gold gebruiken om te kopen/plaatsen (house, wheat, chicken, etc.)

---

## Als tijd over (polish, max 30 min per item)
- [ ] Highlight target / geselecteerde node
- [ ] Bumpkin sprite flip left/right
- [ ] 2 SFX: harvest + drop-off
