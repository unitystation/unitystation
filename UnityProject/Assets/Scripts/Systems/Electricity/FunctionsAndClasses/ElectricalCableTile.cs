using System.Collections;
using System.Collections.Generic;
using Tiles;
using UnityEngine;


[CreateAssetMenu(fileName = "Electrical_Cable_Tile", menuName = "Tiles/Electrical Cable Tile", order = 1)]
public class ElectricalCableTile : BasicTile
{
	public List<PowerTypeCategory> CanConnectTo = new List<PowerTypeCategory>();

	public PowerTypeCategory PowerType;
	public Connection WireEndB;
	public Connection WireEndA;
	public Sprite sprite;
	public override Sprite PreviewSprite => sprite;
}
