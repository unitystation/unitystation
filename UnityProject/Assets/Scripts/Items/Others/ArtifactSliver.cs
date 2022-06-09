using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Radiation;
using Weapons;
using ScriptableObjects.Systems.Research;

namespace Items.Science
{
	public enum CompositionBase
	{
		metal = 1,
		glass = 2, //TODO: think of more things artifacts could be made of
	}

	public struct ArtifactData
	{
		public ArtifactData(int radlvl, int bluelvl, int bnalvl, int mss,CompositionBase comp)
		{
			radiationlevel = radlvl;
			bluespacesig = bluelvl;
			bananiumsig = bnalvl;
			mass = mss;
			compositionBase = comp;
		}

		public CompositionBase compositionBase;
		public int radiationlevel;
		public int bluespacesig;
		public int bananiumsig;
		public int mass;
	}

	public class ArtifactSliver : MonoBehaviour
	{
		public ArtifactData sliverData;

		public string ID;

		public int RPReward;

		public List<GameObject> Composition;

		private RadiationProducer radProducer;
		private MeleeEffect meleeEffect;

		[SerializeField]
		private ArtifactDataSO artifactDataSO;

		private void Awake()
		{
			radProducer = GetComponent<RadiationProducer>();
			meleeEffect = GetComponent<MeleeEffect>();

			Init()
	;
		}

		private void Init()
		{

			if (sliverData.bluespacesig >= 100)
			{
				meleeEffect.enabled = true;
				meleeEffect.maxTeleportDistance = (int)(sliverData.bluespacesig / 100);

				for (int i = 0; i < (int)((sliverData.bluespacesig + 100) / 200); i++) //every 200 bluespace sig starting at 100
				{
					Composition.Add(artifactDataSO.compositionData["bluespace"]);
				}
			}
			if (sliverData.radiationlevel >= 100)
			{
				radProducer.enabled = true;
				radProducer.SetLevel(sliverData.radiationlevel);

				for (int i = 0; i < (int)((sliverData.radiationlevel) / 500); i++) //every 500 rads
				{
					Composition.Add(artifactDataSO.compositionData["uranium"]);
				}
			}

			for (int i = 0; i < (int)((sliverData.bananiumsig) / 500); i++) //every 500 mClw (milli clowns)
			{
				Composition.Add(artifactDataSO.compositionData["bananium"]);
			}

			for (int i = 0; i < (int)((sliverData.mass) / 250); i++) //every 250g
			{
				Composition.Add(artifactDataSO.compositionData[sliverData.compositionBase.ToString()]);
			}
		}
	}
}
