using UnityEngine;

[CreateAssetMenu(menuName = "CityBuilder/TickBehaviours/Park")]
class Park : TickBehaviour
{
    public override void Tick(TileObject tileObject, float delta)
    {
        TileObjectDefinition def = tileObject.Definition;
        if (tileObject is not UtilityTileObject util)
        {
            Debug.LogError("TickBehaviour UtilityDefault applied to non-utility tile object.");
            return;
        }

        float emission = def.Utility.Emission * delta;
        float mult = 1 - GameState.Instance.PreviousEmissionsDelta / 300f;
        emission *= mult;
        emission = Mathf.Min(0, emission);
        TileTypeState state = TileStateCatalog.Instance.Get(def.Id);

        GameState.Instance.EmissionsReductionDelta += Mathf.FloorToInt(emission);

        state.AddTime(delta);
    }
}