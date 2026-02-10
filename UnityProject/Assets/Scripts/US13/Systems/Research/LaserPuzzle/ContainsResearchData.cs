using System;
using Mirror;
using UnityEngine;
using US13.Core.Sprite_Handler;
using US13.Systems.Research.Data;
using Util;

namespace US13.Systems.Research.LaserPuzzle
{
	public class ResearchData
	{
		[NonSerialized] public Technology Technology;
		public float ResearchPower = 0;
	}

	public class ContainsResearchData : NetworkBehaviour, ICloneble
	{
		public ResearchLaserProjector ShotFrom;

		public Color Colour = Color.white;

		public ResearchData ResearchData = new ResearchData();

		public SpriteHandler SpriteHandler;
		public LightSpriteHandler LightSprite;

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
				LightSprite.SetColor(Colour);
			}
		}
	}
}