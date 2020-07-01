using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// THIS COMPONENT HAS BEEN REPLACED BY TOOLSWAPCOMPONENT.CS!

// some context behind this component: i'm making this as a start for my contributions
// to unitystation, so there's probably going to be a lot of weird things here.
// please let me know if you have any advice for my code.
[RequireComponent(typeof(ItemAttributesV2))]
public class JawsOfLife : MonoBehaviour, IInteractable<HandActivate>
{
	private ItemAttributesV2 itemAttributeComponent;
	private int toolSetting = 0;	// set the Jaws of Life to "tool" 0, which will be defined below
	
	private void Awake()
	{
		itemAttributeComponent = GetComponent<ItemAttributesV2>();	// get ItemAttributesV2 from the object this component is attached to
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		if (toolSetting == 0)
		{
			itemAttributeComponent.RemoveTrait(CommonTraits.Instance.Crowbar);
			itemAttributeComponent.AddTrait(CommonTraits.Instance.Wirecutter);

			Chat.AddExamineMsgFromServer(interaction.Performer,"You flick the Jaws of Life into Wirecutter mode" );	// TODO: not sure how this tool is meant to work in-game, so this description might not make sense
			
			toolSetting = 1;
		}
		else if (toolSetting == 1)
		{
			itemAttributeComponent.RemoveTrait(CommonTraits.Instance.Wirecutter);
			itemAttributeComponent.AddTrait(CommonTraits.Instance.Crowbar);
			
			Chat.AddExamineMsgFromServer(interaction.Performer,"You flick the Jaws of Life into Crowbar mode" );	// TODO: like above, this description might not make sense
			
			toolSetting = 0;
		}
	}
}