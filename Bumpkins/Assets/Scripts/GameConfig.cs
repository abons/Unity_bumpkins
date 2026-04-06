using UnityEngine;

/// <summary>
/// Central config — all tunable values in ONE place.
/// Create via: Assets > Create > Bumpkins > GameConfig
/// </summary>
[CreateAssetMenu(menuName = "Bumpkins/GameConfig", fileName = "GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Starting Resources")]
    public int startGold        = 500;

    [Header("Shop Prices (gold per unit)")]
    public int breadPriceGold   = 50;
    public int milkPriceGold    = 100;
    public int eggPriceGold     = 80;

    [Header("Production")]
    public float harvestTickSeconds  = 225f; // time for one wheat harvest (3 seasons: spring→fall, each season = 60s day + 15s night)
    public float milkTickSeconds     = 8f;   // time for one milk
    public float eggIntervalSeconds  = 142f; // chicken produces egg every 142s
    public int   breadPerWheat       = 3;    // 1 wheat → 3 bread

    [Header("Building Costs (gold)")]
    public int costHouse         = 300;
    public int costWheatField    = 100;
    public int costChickenCoop   = 200;
    public int costMill          = 500;
    public int costDairy         = 2500;
    public int costToolshed      = 1500;

    [Header("Happiness")]
    public float happinessBaseDelta = 0.1f;  // per tick
    public float happinessTickSeconds = 10f;
    // price impact: each 100 gold above/below 100 shifts happiness by this amount per tick
    public float priceImpactPerHundred = -0.5f;
}
