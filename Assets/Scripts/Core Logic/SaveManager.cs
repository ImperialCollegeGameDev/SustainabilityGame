using System.Collections.Generic;
using System.IO;
using UnityEditor.Overlays;
using UnityEngine;

public static class SaveManager
{
    private static string path =>
        Application.persistentDataPath + "/save.json";

    public static void Save()
    {
        SaveState data = new SaveState();

        data.money = GameState.Instance.money;
        data.happiness = GameState.Instance.happiness;
        data.emissions = GameState.Instance.TotalEmissions;

        data.tiles = new List<TileSaveData>();

        foreach (var tileObj in GridManager.Instance.GetTileObjects())
        {
            TileSaveData tileData = new TileSaveData();
            tileData.gridPosition = tileObj.Origin;
            tileData.def = tileObj.Definition;

            if (tileObj is ResidentialTileObject res)
            {
                tileData.occupancy = res.occupancy;
            }

            data.tiles.Add(tileData);
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
    }

    public static SaveState Load()
    {
        if (!File.Exists(path))
            return null;

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<SaveState>(json);
    }
}