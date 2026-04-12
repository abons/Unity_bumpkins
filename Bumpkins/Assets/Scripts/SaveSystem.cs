using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple JSON save/load via PlayerPrefs.
/// Call SaveSystem.Save() and SaveSystem.Load() from anywhere.
/// After a Load(), the Game scene reloads and ApplyPendingLoad() restores state.
/// </summary>
public static class SaveSystem
{
    private const string SaveKey = "beasts_save";

    public static bool HasSave => PlayerPrefs.HasKey(SaveKey);

    // ── Data types ──────────────────────────────────────────────────────────

    [Serializable]
    public class BumpkinSave
    {
        public string name;
        public float  x, y;
        public int    type;      // BumpkinController.BumpkinType
        public bool   isChild;
        public bool   isElder;
        public bool   freeWill;
    }

    [Serializable]
    public class BuildingSave
    {
        public int type;    // BuildingType (int)
        public int gridX;
        public int gridY;
    }

    [Serializable]
    public class SaveData
    {
        public string mapName;
        public int    gold, bread, milk, eggs, wheat;
        public float  happiness;
        public bool   millUnlocked;
        public bool   dairyUnlocked;
        public float  dayTime;
        public int    completedCycles;
        public int    season;            // Season enum as int
        public BumpkinSave[]  bumpkins;
        public BuildingSave[] buildings;
    }

    // Pending data to apply after scene reload
    private static SaveData _pending;
    public  static bool      HasPending => _pending != null;

    // ── Save ─────────────────────────────────────────────────────────────────

    public static void Save()
    {
        var gm = GameManager.Instance;
        var dnc = DayNightCycle.Instance;
        if (gm == null) { Debug.LogWarning("[SaveSystem] No GameManager — cannot save."); return; }

        var data = new SaveData
        {
            mapName       = MapSelection.SelectedLayoutName ?? "",
            gold          = gm.Gold,
            bread         = gm.Bread,
            milk          = gm.Milk,
            eggs          = gm.EggStock,
            wheat         = gm.WheatStored,
            happiness     = gm.Happiness,
            millUnlocked  = gm.MillUnlocked,
            dairyUnlocked = gm.DairyUnlocked,
            dayTime        = dnc != null ? dnc.SavedCycleTime       : 0f,
            completedCycles= dnc != null ? dnc.SavedCompletedCycles : 0,
            season         = dnc != null ? (int)dnc.CurrentSeason   : 0,
        };

        // Bumpkins
        var bumpkinList = new List<BumpkinSave>();
        foreach (var bc in GameObject.FindObjectsByType<BumpkinController>(FindObjectsSortMode.None))
        {
            if (bc.IsDead) continue;
            bumpkinList.Add(new BumpkinSave
            {
                name     = bc.name,
                x        = bc.transform.position.x,
                y        = bc.transform.position.y,
                type     = (int)bc.bumpkinType,
                isChild  = bc.isChild,
                isElder  = bc.isElder,
                freeWill = bc.freeWill,
            });
        }
        data.bumpkins = bumpkinList.ToArray();

        // Player-built completed buildings (name format: "{Type}_{col}_{row}")
        var buildingList = new List<BuildingSave>();
        foreach (var site in GameObject.FindObjectsByType<ConstructionSite>(FindObjectsSortMode.None))
        {
            if (site.CurrentStage != ConstructionSite.Stage.Done) continue;
            var parts = site.name.Split('_');
            if (parts.Length >= 3
                && int.TryParse(parts[parts.Length - 2], out int gx)
                && int.TryParse(parts[parts.Length - 1], out int gy)
                && Enum.TryParse(parts[0], out BuildingType bt))
            {
                buildingList.Add(new BuildingSave { type = (int)bt, gridX = gx, gridY = gy });
            }
        }
        // Also save ChickenCoops (no ConstructionSite)
        foreach (var go in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (!go.name.StartsWith("ChickenCoop_")) continue;
            var parts = go.name.Split('_');
            if (parts.Length >= 3
                && int.TryParse(parts[1], out int gx)
                && int.TryParse(parts[2], out int gy))
            {
                buildingList.Add(new BuildingSave { type = (int)BuildingType.ChickenCoop, gridX = gx, gridY = gy });
            }
        }
        data.buildings = buildingList.ToArray();

        string json = JsonUtility.ToJson(data, prettyPrint: false);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
        Debug.Log($"[SaveSystem] Opgeslagen. Bumpkins: {data.bumpkins.Length}, Gebouwen: {data.buildings.Length}");
    }

    // ── Load ─────────────────────────────────────────────────────────────────

    public static void Load()
    {
        if (!HasSave) { Debug.LogWarning("[SaveSystem] Geen opgeslagen spel gevonden."); return; }

        string json = PlayerPrefs.GetString(SaveKey);
        _pending = JsonUtility.FromJson<SaveData>(json);

        if (!string.IsNullOrEmpty(_pending.mapName))
            MapSelection.Select(_pending.mapName);

        SceneManager.LoadScene("Game");
    }

    /// <summary>
    /// Called by TestBumpkinSetup after normal setup. Restores bumpkins,
    /// buildings and all resource/time state from the pending save.
    /// </summary>
    public static void ApplyPendingLoad()
    {
        if (_pending == null) return;
        var data = _pending;
        _pending = null;

        var gm  = GameManager.Instance;
        var dnc = DayNightCycle.Instance;

        // Resources & unlocks
        if (gm != null)
            gm.LoadState(data.gold, data.bread, data.milk, data.eggs,
                         data.wheat, data.happiness, data.millUnlocked, data.dairyUnlocked);

        // Day/night
        if (dnc != null)
            dnc.LoadState(data.dayTime, data.completedCycles, (Season)data.season);

        // Destroy current bumpkins and re-spawn from save
        foreach (var bc in GameObject.FindObjectsByType<BumpkinController>(FindObjectsSortMode.None))
            GameObject.Destroy(bc.gameObject);

        var setup = GameObject.FindFirstObjectByType<TestBumpkinSetup>();
        if (setup != null)
        {
            foreach (var bs in data.bumpkins)
            {
                var bc = setup.SpawnBumpkinPublic(
                    bs.name,
                    new Vector3(bs.x, bs.y, 0f),
                    (BumpkinController.BumpkinType)bs.type,
                    bs.isChild);
                bc.isElder  = bs.isElder;
                bc.freeWill = bs.freeWill;
            }
        }

        // Place saved buildings (instant, no gold cost)
        var bm = BuildManager.Instance;
        if (bm != null)
        {
            foreach (var bs in data.buildings)
                bm.PlaceSaved(new Vector2Int(bs.gridX, bs.gridY), (BuildingType)bs.type);
        }

        Debug.Log($"[SaveSystem] Geladen. Bumpkins: {data.bumpkins.Length}, Gebouwen: {data.buildings.Length}");
    }
}
