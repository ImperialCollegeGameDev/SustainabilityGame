using UnityEngine;

[CreateAssetMenu(menuName = "CityBuilder/TickBehaviours/UtilityDefault")]
class UtilityDefault : TickBehaviour
{
    public override void Tick(TileObject tileObject, float delta)
    {
        TileObjectDefinition def = tileObject.Definition;

        int output = def.Utility.Output;
        float emission = def.Utility.Emission * delta;
        GameState.Instance.Power += output;
        GameState.Instance.TotalEmissions += Mathf.FloorToInt(emission);
    }
}