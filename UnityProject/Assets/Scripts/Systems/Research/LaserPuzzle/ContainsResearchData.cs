using System;
using System.Collections;
using System.Collections.Generic;
using Light2D;
using Systems.Research.Data;
using UnityEngine;

public class ResearchData
{
	[NonSerialized] public Technology Technology;
	public float ResearchPower = 0;
}

public class ContainsResearchData : MonoBehaviour, ICloneble
{



	public ResearchLaserProjector ShotFrom;


	public Color Colour = Color.white;



	public ResearchData ResearchData = new ResearchData();

	public SpriteHandler SpriteHandler;
	public LightSprite LightSprite;

	private TechnologyAndBeams TechnologyAndBeams;

	public void CloneTo(GameObject InCloneTo)
	{
		InCloneTo.GetComponent<ContainsResearchData>().Initialise(TechnologyAndBeams, ShotFrom);
	}


	public void Initialise(TechnologyAndBeams InTechnologyAndBeams, ResearchLaserProjector _ShotFrom)
	{
		ShotFrom = _ShotFrom;
		if (InTechnologyAndBeams != null)
		{
			TechnologyAndBeams = InTechnologyAndBeams;
			ResearchData.Technology = TechnologyAndBeams.Technology;
			Colour = TechnologyAndBeams.Colour;

			SpriteHandler.SetColor(Colour);
			LightSprite.Color = Colour;
		}
	}
}
