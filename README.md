# Unity_bumpkins

GAME JAM TODO (±8–10 uur)  
Doel: een minimale speelbare core loop met productie + shops + bouwen, en een happiness meter die beïnvloed wordt door pricing over time.

---

## 0) CONTEXT (±30 min)

### Core loops (specificatie)

#### Loop A — Wheat → Bakery/Mill → Bread
- Je bestuurt een **bumpkin**.
- Alleen bumpkins van type **male** of **female** kunnen **wheat harvesten**.
- Harvest levert **wheat** op na een korte timer (tick).
- Bumpkin brengt wheat naar de **bakery/mill** (drop-off).
- In de bakery/mill wordt per wheat drop-off **+3 bread** geproduceerd.

#### Loop B — Female → Cow → Farm/Dairy → Milk
- Alleen bumpkins van type **female** kunnen **cow milking** doen.
- Milking levert **milk** op na een korte timer (tick).
- Female bumpkin brengt milk naar de **farm/dairy** (drop-off).
- Elke bumpkin kan **milk kopen** bij de farm/dairy.

#### Loop C — Chicken (Coop) → Eggs → Shop
- Je kunt een **chicken** (of chickencoop) kopen/plaatsen met gold.
- Een chicken produceert **elke 2 minuten +1 egg** (stock).
- Elke bumpkin kan **eggs kopen** (alleen als er egg stock is).

#### Economy (MVP)
- Elke bumpkin kan **bread**, **milk** en **egg** kopen voor **gold**.
- **Gold sources verschillen per level** (MVP: level config bepaalt hoe gold binnenkomt).
- **Gold** is de resource die je nodig hebt om buildings te kopen/plaatsen: house, bakery/mill, farm/dairy, wheat (farm/field), chicken (coop), etc.
- UI toont resources realtime.

#### Happiness meter (MVP)
- Er is een `happiness` meter (0–100).
- Happiness verandert **over time** (bijv. elke X seconden / per dag-tick).
- Pricing beïnvloedt happiness: per **source** (bread/milk/egg) kun je de prijs aanpassen; dit beïnvloedt happiness over time.

---

## 1) RULES / DATA MODEL (MVP)

### Bumpkin types (spec)
- Bumpkin kan één van deze types zijn: **male**, **female**, **boy**, **girl**.
- Alleen **male/female** kunnen wheat harvesten.
- Alleen **female** kan cow milking doen.
- Elke bumpkin kan kopen in shops (bread/milk/egg).

### Resources (MVP)
- `wheat` (grondstof)
- `bread` (product)
- `milk` (product)
- `egg` (product; via chicken production + stock)
- `gold` (currency; nodig voor bouwen/kopen)
- `happiness` (meter)

### Prices (MVP)
- Base cost (startwaarde) voor **bread**, **milk** of **egg** is **100 gold**.
- Prijzen kunnen **per source** aangepast worden:
  - `breadPriceGold` (default 100)
  - `milkPriceGold` (default 100)
  - `eggPriceGold` (default 100)
- Deze prijsaanpassingen beïnvloeden `happiness` over time.

### Production rules (MVP)
- Bakery/Mill: `1 wheat` drop-off → `+3 bread`
- Chicken production: elke `120s` → `eggStock += 1`
- Egg purchase: alleen mogelijk als `eggStock > 0` (na aankoop `eggStock -= 1`)

### Gold sources per level (MVP)
- Gold income verschilt per level en wordt vanuit een centrale `LevelConfig` bepaald.
- Voorbeelden (kies per level 1 of meer):
  - Start gold (`startingGold`)
  - Periodieke income (`goldPerTick`)
  - Rewards/quests (`questRewardGold`)
  - (Later) verkoop van producten

### Buildings / buyables (MVP)
Je kunt deze kopen/plaatsen met gold (zoals “huis kopen”):
- `House`
- `WheatField/Farm` (om wheat nodes te hebben)
- `Bakery/Mill`
- `Farm/Dairy`
- `Chicken/ChickenCoop`

---

## 2) ENTITIES (DoD)
- Bumpkin
- Wheat/GrainNode (resource node / field)
- Cow (milking node)
- Bakery/Mill (drop-off / bread productie)
- Farm/Dairy (drop-off milk + milk shop)
- Chicken/ChickenCoop (egg productie + stock)
- House (buildable voorbeeld)

---

## 3) BUMPKIN BEHAVIOR (STATE MACHINE) (DoD)

- States:
  - `Idle`
  - `MoveToTarget`
  - `HarvestingWheat` (alleen `male` / `female`)
  - `MilkingCow` (alleen `female`)
  - `MoveToBakery`
  - `MoveToFarm`
  - `DroppingOff`
  - (Optioneel) `Shopping`
  - (Optioneel) `Building`

**Definitie klaar**
- In de Console/log kun je altijd zien in welke state de bumpkin zit (minimaal debug tekst).

---

## 4) JAM PLAN (±8–10 uur)

## 4.1) PROJECT SETUP (±30 min)
- [ ] Unity project aanmaken
- [ ] VS Code koppelen
- [ ] GitHub Copilot activeren
- [ ] Sprites importeren
- [ ] Scene aanmaken

**DoD**
- Project opent zonder errors
- Er is één scene die je kan runnen met een camera en een lege “Game” root

---

## 4.2) WERELD (±1 uur)
- [ ] Tilemap/grid aanmaken (of simpele ground plane; kies 1)
- [ ] Terrain plaatsen (grass etc.)
- [ ] Camera movement (scroll/zoom)

**DoD**
- Je kunt rondkijken (pan/zoom) en ziet duidelijk de speelruimte

---

## 4.3) INPUT + BUMPKIN MOVEMENT (±1 uur)
- [ ] Click input: world position bepalen (raycast / screen→world)
- [ ] Klik op ground → bumpkin beweegt naar target
- [ ] Stopafstand zodat bumpkin niet “trilt” op target

**DoD**
- Klik ergens: bumpkin loopt daarheen en stopt netjes
- Debug/log toont: huidige target + state

---

## 4.4) PRODUCTION NODES (±1–2 uur)

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

## 4.5) DROP-OFF + PRODUCTION (±1–2 uur)

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

## 4.6) SHOPS: KOPEN VOOR GOLD (±1 uur)
- [ ] Bread shop: koop bread voor gold (`breadPriceGold`, default 100)
- [ ] Milk shop: koop milk voor gold (`milkPriceGold`, default 100)
- [ ] Egg shop: koop egg voor gold (`eggPriceGold`, default 100) en alleen als `eggStock > 0`

**DoD**
- Als `gold >= prijs`: gold daalt, item stijgt exact 1 keer per aankoop
- Egg aankoop: `eggStock` daalt exact met 1
- Als `gold < prijs` of `eggStock == 0`: aankoop faalt met duidelijke feedback

---

## 4.7) GOLD PER LEVEL (±30–60 min)
- [ ] `LevelConfig`/`GameConfig` waarin gold sources per level instelbaar zijn
- [ ] Start gold + (optioneel) income per tick

**DoD**
- Gold start/income verschilt per level door config, niet hardcoded verspreid

---

## 4.8) HAPPINESS (±1 uur)
- [ ] `happiness` meter (0–100)
- [ ] Over-time update (tick)
- [ ] Happiness wordt beïnvloed door pricing (bread/milk/egg), per source instelbaar

**Jam-safe formule (voorbeeld, simpel)**
- Elke tick:
  - `happiness += baseDelta`
  - `happiness += priceImpact(breadPriceGold, milkPriceGold, eggPriceGold)`
  - clamp naar 0–100

**DoD**
- Happiness verandert over time
- Prijs aanpassen heeft zichtbaar effect (log/UI)

---

## 4.9) BUILDING / BUYABLES (GOLD COST) (±1–2 uur)
- [ ] Minimaal: `House` kopen/plaatsen
- [ ] Ook buyable: `WheatField/Farm`, `Chicken/ChickenCoop`, `Bakery/Mill`, `Farm/Dairy`
- [ ] Building costs in gold (hardcoded maar centraal)
- [ ] Plaatsen op world (simpel: click om te plaatsen)

**DoD**
- Je kunt alleen kopen/plaatsen als je genoeg gold hebt
- Bij aankoop/plaatsing: gold daalt exact met de cost
- Object verschijnt 1 keer en blijft staan

---

## 4.10) UI (±1 uur)
- [ ] Toon minimaal `gold`, `bread`, `milk`, `eggStock`, `happiness` (optioneel `wheat`)
- [ ] Realtime updates

**DoD**
- UI klopt na harvest/drop-off/kopen/bouwen
- Geen rode errors in Console

---

## 5) EINDDOEL (MVP)
- [ ] Bumpkin beweegt
- [ ] Male/female harvesten wheat
- [ ] Female milkt cow
- [ ] Wheat → bakery/mill → bread (+3 per wheat)
- [ ] Milk → farm/dairy (drop-off) + milk kopen voor gold
- [ ] Chicken produceert egg elke 2 minuten (stock) + eggs kopen voor gold
- [ ] Bread/milk/egg base cost 100, adjustable per source
- [ ] Gold sources per level via config
- [ ] Happiness meter werkt en reageert op pricing
- [ ] Gold gebruiken om te kopen/plaatsen (house, wheat, chicken, etc.)- [ ] VS Code koppelen
- [ ] GitHub Copilot activeren
- [ ] Sprites importeren
- [ ] Scene aanmaken

**DoD**
- Project opent zonder errors
- Er is één scene die je kan runnen met een camera en een lege “Game” root

---

## 2) WERELD (±1 uur)

- [ ] Tilemap/grid aanmaken (of simpele ground plane; kies 1)
- [ ] Terrain plaatsen (grass etc.)
- [ ] Camera movement (scroll/zoom)

**Keuze (jam-safe)**
- Movement type:
  - A) Simpel: vrije world + `MoveTowards` (snelst)
  - B) Grid + pathfinding (alleen als je zeker bent)

**DoD**
- Je kunt rondkijken (pan/zoom) en ziet duidelijk de speelruimte

---

## 3) INPUT + WORKER MOVEMENT (±1 uur)

- [ ] Click input: world position bepalen (raycast / screen→world)
- [ ] Klik op ground → worker beweegt naar target
- [ ] Stopafstand zodat worker niet “trilt” op target

**DoD**
- Klik ergens: worker loopt daarheen en stopt netjes
- Debug/log toont: huidige target + state

---

## 4) RESOURCE NODES (±1 uur)

- [ ] Tree/ResourceNode object plaatsen
- [ ] Interactie: click op tree → worker gaat erheen
- [ ] Start harvest wanneer dichtbij genoeg (range check)
- [ ] Simpele timer/tick implementeren

**DoD**
- Klik boom → worker gaat erheen → na X seconden verschijnt “harvest tick” (log)
- Worker harvest niet als die te ver weg is

**Mini-balancing (kies 1)**
- A) Cooldown-based: elke X sec +Y wood zolang je aan het harvesten bent
- B) Capacity-based: node heeft `remaining` (bijv. 5 ticks) en wordt daarna “empty”

---

## 5) RESOURCE SYSTEEM (±1–2 uur)

- [ ] `wood` variabele + (optioneel) `food`
- [ ] Worker “draagt” wood (kan ook direct naar global, maar noteer keuze)
- [ ] Output zichtbaar maken (debug/log)

**DoD**
- Wood verandert deterministisch (geen dubbele ticks per frame)
- Alle tuning getallen staan op 1 plek (bijv. `GameConfig`/`Constants`)

---

## 6) BUILDING + DROP-OFF (±1–2 uur)

- [ ] Storage building plaatsen
- [ ] Worker loopt terug naar storage na harvest (of na 1 tick; kies)
- [ ] Drop-off: resource wordt toegevoegd bij aankomst

**DoD**
- Na harvest gaat worker naar storage en “dropt” precies 1 keer
- Daarna terug naar `Idle` (of terug naar node als je dat wil)

---

## 7) UI (±1 uur)

- [ ] Wood/food tonen op scherm (Text is genoeg)
- [ ] Realtime updates

**DoD**
- UI toont altijd huidige wood value
- Werkt ook na meerdere harvest + drop-offs

---

## 8) CHECK & ITERATIE (±1 uur)

### Playtest checklist (MVP)
- [ ] Klik ground → worker beweegt
- [ ] Klik tree → worker gaat erheen
- [ ] Worker harvest (tick) gebeurt op timer, niet per frame
- [ ] Worker gaat naar storage
- [ ] Drop-off verhoogt wood
- [ ] UI telt mee
- [ ] Geen rode errors in Console

### Als er bugs zijn: prioriteit
1) Core loop werkt
2) UI klopt
3) Alles eromheen

---

## REGELS (jam)
- Eerst werkend > dan mooi
- Hardcoded values oké, maar wel centraal
- 1 feature tegelijk
- Geen perfectionisme

---

## ALS TIJD OVER (polish, max 30 min per item)
- [ ] Simple feedback: highlight target / geselecteerde tree
- [ ] Worker anim: flip left/right
- [ ] 2 SFX: chop + drop-off

---

## EINDDOEL (MVP)
- [ ] Worker beweegt
- [ ] Worker harvest
- [ ] Worker brengt resource terug
- [ ] Resource telt op
