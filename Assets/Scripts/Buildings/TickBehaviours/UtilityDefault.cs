using UnityEngine;

[CreateAssetMenu(menuName = "CityBuilder/TickBehaviours/UtilityDefault")]
class UtilityDefault : TickBehaviour
{
    public override void Tick(TileObject tileObject, float delta)
    {
        TileObjectDefinition def = tileObject.Definition;
        if (tileObject is not UtilityTileObject util)
        {
            Debug.LogError("TickBehaviour UtilityDefault applied to non-utility tile object.");
            return;
        }

        int output = def.Utility.Output;
        float emission = def.Utility.Emission * delta;

        TileTypeState state = TileStateCatalog.Instance.Get(def.Id);

        util.efficiency -= (delta / def.Utility.DegradeTime) * (1 - GameState.Instance.Settings.MinimumEfficiency);
        util.efficiency = Mathf.Max(util.efficiency, GameState.Instance.Settings.MinimumEfficiency);

        GameState.Instance.Power += Mathf.FloorToInt(output * util.efficiency);
        GameState.Instance.EmissionsDelta += Mathf.FloorToInt(emission);

        tileObject.AddTime(delta);
    }
}