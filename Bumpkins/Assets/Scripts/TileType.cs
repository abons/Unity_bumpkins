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
    Tree1     = 8,   // tree01 sprite; winter variant: tree05
    Tree2     = 9,   // tree02 sprite; winter variant: tree04
    Tree10    = 10,  // tree10 sprite; winter variant: tree11
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
