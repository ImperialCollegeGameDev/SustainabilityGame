using UnityEngine;

[CreateAssetMenu(menuName = "CityBuilder/TickBehaviours/CoalPowerPlant")]
class CoalPowerPlant : TickBehaviour
{
    public override void Tick(TileObject tileObject, float delta)
    {
        TileObjectDefinition def = tileObject.Definition;

        int output = def.Utility.Output;
        float emission = def.Utility.Emission * delta;

        TileTypeState state = TileStateCatalog.Instance.Get(def.Id);
        if (state.HasUpgrade("DoubleTrouble"))
        {
            emission *= 2;
            output *= 2;
        }

        GameState.Instance.Power += output;
        GameState.Instance.TotalEmissions += Mathf.FloorToInt(emission);
    }
}