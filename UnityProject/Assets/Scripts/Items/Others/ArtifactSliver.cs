using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Radiation;
using Weapons;
using ScriptableObjects.Systems.Research;

public class ArtifactSliver : MonoBehaviour
{
	//Stats
	public int radiationlevel;
	public int bluespacesig;
	public int bananiumsig;
	public int mass;
	public string ID;

	public int RPReward;

	public CompositionBase compositionBase = CompositionBase.glass;

	public List<GameObject> Composition;

	private RadiationProducer radProducer;
	private MeleeEffect meleeEffect;

	[SerializeField]
	private ArtifactData artifactData;

	private void Awake()
	{
		radProducer = GetComponent<RadiationProducer>();
		meleeEffect = GetComponent<MeleeEffect>();

		Init()
;	}

	public enum CompositionBase
	{
		metal = 1,
		glass = 2, //TODO: think of more things artifacts could be made of
	}

	private void Init()
	{
		
		if(bluespacesig >= 100)
		{
			meleeEffect.enabled = true;
			meleeEffect.maxTeleportDistance = (int)(bluespacesig / 100);

			for(int i = 0; i < (int)((bluespacesig + 100)/200); i++) //every 200 bluespace sig starting at 100
			{
				Composition.Add(artifactData.compositionData["bluespace"]);
			}
		}
		if(radiationlevel >= 100)
		{
			radProducer.enabled = true;
			radProducer.SetLevel(radiationlevel);

			for (int i = 0; i < (int)((radiationlevel) / 500); i++) //every 500 rads
			{
				Composition.Add(artifactData.compositionData["uranium"]);
			}
		}

		for (int i = 0; i < (int)((bananiumsig) / 500); i++) //every 500 mClw (milli clowns)
		{
			Composition.Add(artifactData.compositionData["bananium"]);
		}

		for (int i = 0; i < (int)((mass) / 250); i++) //every 250g
		{
			Composition.Add(artifactData.compositionData[compositionBase.ToString()]);
		}
	}
}
