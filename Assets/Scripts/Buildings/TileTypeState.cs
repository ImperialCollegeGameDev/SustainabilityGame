using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TileTypeState // Can store runtime info about a specific type of building, e.g. total energy produced by all coal power plants
{
    public TileObjectDefinition Definition;
}