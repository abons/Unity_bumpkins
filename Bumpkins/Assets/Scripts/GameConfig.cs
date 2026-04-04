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
    public int breadPriceGold   = 100;
    public int milkPriceGold    = 100;
    public int eggPriceGold     = 100;

    [Header("Production")]
    public float harvestTickSeconds  = 5f;   // time for one wheat harvest
    public float milkTickSeconds     = 8f;   // time for one milk
    public float eggIntervalSeconds  = 120f; // chicken produces egg every 2 min
    public int   breadPerWheat       = 3;    // 1 wheat → 3 bread

    [Header("Building Costs (gold)")]
    public int costHouse         = 200;
    public int costWheatField    = 150;
    public int costChickenCoop   = 100;
    public int costBakery        = 300;
    public int costMill          = 400;
    public int costDairy         = 300;
    public int costToolshed      = 175;

    [Header("Happiness")]
    public float happinessBaseDelta = 0.1f;  // per tick
    public float happinessTickSeconds = 10f;
    // price impact: each 100 gold above/below 100 shifts happiness by this amount per tick
    public float priceImpactPerHundred = -0.5f;
}
