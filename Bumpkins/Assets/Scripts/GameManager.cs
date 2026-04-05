using UnityEngine;

/// <summary>
/// Singleton that holds all game state: gold, bread, milk, eggStock, happiness.
/// Access from anywhere via GameManager.Instance
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Config")]
    public GameConfig config;

    // --- Resources ---
    public int Gold        { get; private set; }
    public int Bread       { get; private set; }
    public int Milk        { get; private set; }
    public int EggStock    { get; private set; }
    public float Happiness { get; private set; }

    // --- Wheat in storage (waiting for bakery) ---
    public int WheatStored { get; private set; }

    // --- Building unlocks ---
    public bool MillUnlocked  { get; private set; }
    public bool DairyUnlocked { get; private set; }

    private float _happinessTick;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Maak standaard config als die niet gekoppeld is
        if (config == null)
            config = ScriptableObject.CreateInstance<GameConfig>();

        Gold      = config.startGold;
        Happiness = 50f;
    }

    void Update()
    {
        _happinessTick += Time.deltaTime;
        if (_happinessTick >= config.happinessTickSeconds)
        {
            _happinessTick = 0f;
            TickHappiness();
        }
    }

    public void SetGold(int amount) { Gold = amount; }

    // ---- Building unlocks ----
    public void UnlockMill()  { MillUnlocked  = true; Debug.Log("[GM] Molen ontgrendeld!"); }
    public void UnlockDairy() { DairyUnlocked = true; Debug.Log("[GM] Zuivelfabriek ontgrendeld!"); }

    // ---- Harvesting ----
    public void AddWheat(int amount = 1)
    {
        WheatStored += amount;
        Debug.Log($"[GM] Wheat stored: {WheatStored}");
    }

    public void AddMilk(int amount = 1)
    {
        Milk += amount;
        Debug.Log($"[GM] Milk: {Milk}");
    }

    /// <summary>Called by Bakery/Mill when bumpkin drops off wheat.</summary>
    public void ProcessWheatAtBakery(int wheatAmount)
    {
        WheatStored = Mathf.Max(0, WheatStored - wheatAmount);
        int produced = wheatAmount * config.breadPerWheat;
        Bread += produced;
        Debug.Log($"[GM] Bakery: {wheatAmount} wheat → +{produced} bread. Total bread: {Bread}");
    }

    // ---- Chicken ----
    public void AddEgg()
    {
        EggStock++;
        Debug.Log($"[GM] Egg stock: {EggStock}");
    }

    // ---- Shops ----
    public bool BuyBread()
    {
        if (Gold < config.breadPriceGold) { Debug.Log("[GM] Not enough gold for bread"); return false; }
        Gold -= config.breadPriceGold;
        Bread++;
        Debug.Log($"[GM] Bought bread. Gold: {Gold}, Bread: {Bread}");
        return true;
    }

    public bool BuyMilk()
    {
        if (Gold < config.milkPriceGold) { Debug.Log("[GM] Not enough gold for milk"); return false; }
        Gold -= config.milkPriceGold;
        Milk++;
        Debug.Log($"[GM] Bought milk. Gold: {Gold}, Milk: {Milk}");
        return true;
    }

    public bool BuyEgg()
    {
        if (Gold < config.eggPriceGold) { Debug.Log("[GM] Not enough gold for egg"); return false; }
        if (EggStock <= 0)              { Debug.Log("[GM] No eggs in stock");        return false; }
        Gold -= config.eggPriceGold;
        EggStock--;
        Debug.Log($"[GM] Bought egg. Gold: {Gold}, EggStock: {EggStock}");
        return true;
    }

    // ---- Building ----
    public bool Buy(int cost, string buildingName)
    {
        if (Gold < cost) { Debug.Log($"[GM] Not enough gold for {buildingName}"); return false; }
        Gold -= cost;
        Debug.Log($"[GM] Built {buildingName}. Gold left: {Gold}");
        return true;
    }

    // ---- Happiness ----
    private void TickHappiness()
    {
        float delta = config.happinessBaseDelta;

        // each price 100 above baseline hurts happiness
        delta += PriceImpact(config.breadPriceGold);
        delta += PriceImpact(config.milkPriceGold);
        delta += PriceImpact(config.eggPriceGold);

        Happiness = Mathf.Clamp(Happiness + delta, 0f, 100f);
        Debug.Log($"[GM] Happiness: {Happiness:F1}");
    }

    private float PriceImpact(int price)
    {
        // every 100 gold above 100 gives negative impact
        float hundreds = (price - 100) / 100f;
        return hundreds * config.priceImpactPerHundred;
    }
}
