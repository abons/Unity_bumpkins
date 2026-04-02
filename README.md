# Unity_bumpkins

GAME JAM TODO (±8–10 uur)  
Doel: een minimale speelbare core loop “worker → resource → storage → resource telt op”.

---

## 0) CONTEXT (±30 min)

### Core loop (specificatie)
- Worker kan een **resource node** (boom) harvesten.
- Harvest levert `+wood` op na een korte timer (tick).
- Worker brengt wood naar **storage** (drop-off).
- UI toont wood realtime.

### Worker gedrag als state machine (DoD)
- States:
  - `Idle`
  - `MoveToNode`
  - `Harvesting`
  - `MoveToStorage`
  - `DroppingOff`
- Definitie klaar:
  - In de Console/log kun je altijd zien in welke state de worker zit (minimaal debug tekst).

### Entities (DoD)
- Worker
- Tree/ResourceNode
- Storage

### Basis regels (MVP, hardcoded maar centraal)
- Harvest tick: bijv. elke `5s` krijg je `+10 wood`
- (Optioneel) Node heeft `remaining` of `cooldown`

---

## 1) PROJECT SETUP (±30 min)

- [ ] Unity project aanmaken
- [ ] VS Code koppelen
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
