using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Metre : MonoBehaviour, ICheckedInteractable<HandApply>, IExaminable
{
	public SpriteHandler spriteHandler;
	public MetaDataNode metaDataNode;
	public RegisterTile registerTile;

	private Pipes.MixAndVolume MixAndVolume;

	public string Examine(Vector3 worldPos = default)
	{
		return ReadMeter();
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.TargetObject != gameObject) return false;
		if (interaction.HandObject != null) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		Chat.AddExamineMsgFromServer(interaction.Performer, ReadMeter());
	}

	private string ReadMeter()
	{
		var gasInfo = metaDataNode.PipeData[0].pipeData.GetMixAndVolume;
		string pressure = gasInfo.Density().y.ToString("#.00");
		string tempK = gasInfo.Temperature.ToString("#.00");
		string tempC = (gasInfo.Temperature - 273.15f).ToString("#.00");

		if (metaDataNode.PipeData.Count > 0)
		{
			return $"The pressure gauge reads {pressure} kPa, with a temperature of {tempK} K ({tempC} °C).";
		}
		else
		{
			return "The meter is not connected to anything.";
		}
	}

	void Start()
	{
		registerTile = this.GetComponent<RegisterTile>();
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
