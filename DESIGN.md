# Design — Beasts & Bumpkins (Unity remake)

Doel: een minimale speelbare core loop met productie + shops + bouwen, en een happiness meter die beïnvloed wordt door pricing over time.

---

## 0) CORE LOOPS

### Loop A — Wheat → Bakery/Mill → Bread
- Je bestuurt een **bumpkin**.
- Alleen bumpkins van type **male** of **female** kunnen **wheat harvesten**.
- Harvest levert **wheat** op na een korte timer (tick).
- Bumpkin brengt wheat naar de **bakery/mill** (drop-off).
- In de bakery/mill wordt per wheat drop-off **+3 bread** geproduceerd.

### Loop B — Female → Cow → Farm/Dairy → Milk
- Alleen bumpkins van type **female** kunnen **cow milking** doen.
- Milking levert **milk** op na een korte timer (tick).
- Female bumpkin brengt milk naar de **farm/dairy** (drop-off).
- Elke bumpkin kan **milk kopen** bij de farm/dairy.

### Loop C — Chicken (Coop) → Eggs → Shop
- Je kunt een **chicken** (of chickencoop) kopen/plaatsen met gold.
- Een chicken produceert **elke 2 minuten +1 egg** (stock).
- Elke bumpkin kan **eggs kopen** (alleen als er egg stock is).

### Economy (MVP)
- Elke bumpkin kan **bread**, **milk** en **egg** kopen voor **gold**.
- **Gold sources verschillen per level** (MVP: level config bepaalt hoe gold binnenkomt).
- **Gold** is de resource die je nodig hebt om buildings te kopen/plaatsen.
- UI toont resources realtime.

### Happiness meter (MVP)
- Er is een `happiness` meter (0–100).
- Happiness verandert **over time** (bijv. elke X seconden / per dag-tick).
- Pricing beïnvloedt happiness: per **source** (bread/milk/egg) kun je de prijs aanpassen; dit beïnvloedt happiness over time.

---

## 1) RULES / DATA MODEL (MVP)

### Bumpkin types
- Bumpkin kan één van deze types zijn: **male**, **female**, **boy**, **girl**.
- Alleen **male/female** kunnen wheat harvesten.
- Alleen **female** kan cow milking doen.
- Elke bumpkin kan kopen in shops (bread/milk/egg).

### Resources
- `wheat` (grondstof)
- `bread` (product)
- `milk` (product)
- `egg` (product; via chicken production + stock)
- `gold` (currency; nodig voor bouwen/kopen)
- `happiness` (meter)

### Prices
- Base cost voor **bread**, **milk** of **egg** is **100 gold**.
- Prijzen kunnen **per source** aangepast worden:
  - `breadPriceGold` (default 100)
  - `milkPriceGold` (default 100)
  - `eggPriceGold` (default 100)
- Deze prijsaanpassingen beïnvloeden `happiness` over time.

### Production rules
- Bakery/Mill: `1 wheat` drop-off → `+3 bread`
- Chicken production: elke `120s` → `eggStock += 1`
- Egg purchase: alleen mogelijk als `eggStock > 0` (na aankoop `eggStock -= 1`)

### Gold sources per level
- Gold income verschilt per level via een centrale `LevelConfig`.
- Voorbeelden:
  - Start gold (`startingGold`)
  - Periodieke income (`goldPerTick`)
  - Rewards/quests (`questRewardGold`)

### Buildings / buyables
- `House`
- `WheatField/Farm`
- `Bakery/Mill`
- `Farm/Dairy`
- `Chicken/ChickenCoop`

---

## 2) ENTITIES

- Bumpkin
- Wheat/GrainNode (resource node / field)
- Cow (milking node)
- Bakery/Mill (drop-off / bread productie)
- Farm/Dairy (drop-off milk + milk shop)
- Chicken/ChickenCoop (egg productie + stock)
- House (buildable voorbeeld)

---

## 3) BUMPKIN BEHAVIOR (STATE MACHINE)

States:
- `Idle`
- `MoveToTarget`
- `HarvestingWheat` (alleen `male` / `female`)
- `MilkingCow` (alleen `female`)
- `MoveToBakery`
- `MoveToFarm`
- `DroppingOff`
- (Optioneel) `Shopping`
- (Optioneel) `Building`

**DoD**
- In de Console/log kun je altijd zien in welke state de bumpkin zit (minimaal debug tekst).
