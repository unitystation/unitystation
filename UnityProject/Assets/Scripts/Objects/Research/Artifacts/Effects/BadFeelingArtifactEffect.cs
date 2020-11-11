using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Research/ArtifactEffects/BadFeeling")]
public class BadFeelingArtifactEffect : ArtifactEffect
{
	public string[] Messages;
	public string[] DrasticMessages;

	public override void DoEffectTouch(Artifact artifact, GameObject touchSource)
	{
		base.DoEffectTouch(artifact, touchSource);
		Chat.AddWarningMsgFromServer(touchSource, Messages.PickRandom());
	}
}
