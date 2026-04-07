using UnityEngine;

/// <summary>
/// Tile types used in the grid layout.
/// </summary>
public enum TileType
{
    Grass     = 0,
    Road      = 1,
    FarmPlot  = 2,
    Rock      = 3,
    Wood      = 4,
    Water     = 5,
    Snow      = 6,
    Sand      = 7,   // Beach / shore transition strip
}

/// <summary>
/// Building types that can be placed on the grid.
/// </summary>
public enum BuildingType
{
    None        = 0,
    Mill        = 2,
    Cow         = 4,
    Campfire    = 5,
    Rockpile    = 6,
    Woodpile    = 7,
    Dairy       = 9,
    ChickenCoop = 10,
    House       = 11,
    WheatField  = 12,
    Toolshed    = 13,
}
