using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Metre : MonoBehaviour, ICheckedInteractable<HandApply>
{
	public SpriteHandler spriteHandler;
	public MetaDataNode metaDataNode;
	public RegisterTile registerTile;

	private Pipes.MixAndVolume MixAndVolume;

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.TargetObject != gameObject) return false;
		if (interaction.HandObject != null) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (metaDataNode.PipeData.Count > 0)
		{
			Chat.AddExamineMsgFromServer(interaction.Performer,
				"The pressure gauge reads " + metaDataNode.PipeData[0].pipeData.GetMixAndVolume.Density().y + " Kpa " +
				metaDataNode.PipeData[0].pipeData.GetMixAndVolume.Temperature + "K (" + (metaDataNode.PipeData[0].pipeData.GetMixAndVolume.Temperature-273.15f) + "C)" );
			return;
		}

		Chat.AddExamineMsgFromServer(interaction.Performer, " ? Kpa ");
	}


	void Start()
	{
		registerTile = this.GetComponent<RegisterTile>();
	}

	void Update()
	{
	}

	private void OnEnable()
	{
		if (CustomNetworkManager.Instance._isServer == false) return;

		UpdateManager.Add(CycleUpdate, 1);
	}

	private void OnDisable()
	{
		if (CustomNetworkManager.Instance._isServer == false) return;

		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CycleUpdate);
	}

	public void CycleUpdate()
	{
		if (metaDataNode == null)
		{
			metaDataNode = MatrixManager.AtPoint(registerTile.WorldPositionServer, true).MetaDataLayer
				.Get(registerTile.LocalPositionServer, false);
		}

		if (metaDataNode.PipeData.Count > 0)
		{
			MixAndVolume = metaDataNode.PipeData[0].pipeData.GetMixAndVolume;
			if (MixAndVolume.Density().y == 0)
			{
				spriteHandler.ChangeSprite(0);
			}
			else
			{
				int toSet = (int) Mathf.Floor(MixAndVolume.Density().y / (500f)); //10000f/20f
				if (toSet == 0)
				{
					toSet = 1;
				}

				if (toSet > 20)
				{
					toSet = 20;
				}

				spriteHandler.ChangeSprite(toSet);
			}
		}
		else
		{
			spriteHandler.ChangeSprite(0);
		}
	}
}