using SecureStuff;
using UnityEngine;
using US13.Actions.V2;
using US13.HealthV2.Living;

public class BodyPartActions : BodyPartBehavior
{
	[field:SerializeField]
	public SerializableDictionary<ActionButtonData, SerializedAction> ActivatedAbilities { get; set; }

	public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
	{
		foreach (var action in ActivatedAbilities.Keys)
		{
			livingHealth.playerScript.PlayerButtonedActions?.RegisterNewAction(action, ActivatedAbilities[action].Invoke);
		}
	}

	public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
	{
		foreach (var action in ActivatedAbilities.Keys)
		{
			livingHealth.playerScript?.PlayerButtonedActions?.UnregisterAction(action);
		}
		base.OnRemovedFromBody(livingHealth, source);
	}
}
