using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Radiation;
using Weapons;
using ScriptableObjects.Systems.Research;
using Objects.Research;
using Mirror;

namespace Items.Science
{

	public class ArtifactSliver : MonoBehaviour
	{
		internal ArtifactData sliverData;

		public string ID;

		public int RPReward;

		public List<GameObject> Composition;

		private RadiationProducer radProducer;
		private MeleeEffect meleeEffect;

		[SerializeField]
		private SpriteHandler spriteHandler;

		[SerializeField]
		private ArtifactDataSO artifactDataSO;

		private void Awake()
		{
			radProducer = GetComponent<RadiationProducer>();
			meleeEffect = GetComponent<MeleeEffect>();
		}

		internal void SetUpValues(ArtifactData parentData, string Id)
		{
			ID = Id;
			sliverData = parentData;

			sliverData.radiationlevel += (int)(sliverData.radiationlevel * Random.Range(-0.20f, 0.20f)); // +- 20% accuracy
			sliverData.bluespacesig += (int)(sliverData.bluespacesig * Random.Range(-0.20f, 0.20f)); // +- 20% accuracy
			sliverData.bluespacesig += (int)(sliverData.bluespacesig * Random.Range(-0.20f, 0.20f)); // +- 20% accuracy

			RPReward = Random.Range(5, 10);

			sliverData.mass = sliverData.radiationlevel / 20 + sliverData.bluespacesig + sliverData.bananiumsig / 2;

			spriteHandler.SetCatalogueIndexSprite((int)sliverData.Type);

			GetComponent<ItemAttributesV2>().ServerSetArticleName("Artifact Sample - " + ID);

			Init();
		}

		private void Init()
		{
			if (sliverData.bluespacesig >= 100)
			{
				meleeEffect.NetSetActive(true);
				meleeEffect.maxTeleportDistance = (int)(sliverData.bluespacesig / 100);

				for (int i = 0; i < (int)((sliverData.bluespacesig + 100) / 200); i++) //every 200 bluespace sig starting at 100
				{
					Composition.Add(artifactDataSO.compositionData["bluespace"]);
				}
			}
			if (sliverData.radiationlevel >= 100)
			{
				radProducer.NetSetActive(true);
				radProducer.SetLevel(sliverData.radiationlevel);

				for (int i = 0; i < (int)((sliverData.radiationlevel) / 2500); i++) //every 2500 rads
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
				Composition.Add(artifactDataSO.compositionData[sliverData.Type.ToString()]);
			}
		}
	}
}
