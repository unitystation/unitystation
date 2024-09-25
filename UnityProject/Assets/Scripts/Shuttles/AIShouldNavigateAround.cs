using System.Collections;
using System.Collections.Generic;
using TileMap.Behaviours;
using UnityEngine;

public class AIShouldNavigateAround : ItemMatrixSystemInit
{
	public bool ShouldNavigateAround = true;

	public override void Start()
    {
	    base.Start();
	    MetaTileMap.matrix.AIShuttleShouldAvoid = ShouldNavigateAround;
    }

}
