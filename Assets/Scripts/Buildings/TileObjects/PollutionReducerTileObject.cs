using System;
using UnityEngine;

public class PollutionReducerTileObject : TileObject
{
    [NonSerialized] public float emissionReduction = 5;
    [NonSerialized] public float emissionMultiplier = 1;
}