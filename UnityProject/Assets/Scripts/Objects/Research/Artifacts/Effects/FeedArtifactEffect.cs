using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeedArtifactEffect : ArtifactEffect
{
	public ItemTrait[] acceptedItems;

	public string[] acceptedItemMessages;
	public string[] emptyHandMessages;
	public string[] wrongItemMessages;

	public bool despawnItemOnFeed = true;

	public int howManyToConsume = 1;

	private int successTimer = 0;

	public override void DoEffectTouch(HandApply touchSource)
	{
		base.DoEffectTouch(touchSource);
		TryFeed(touchSource);
	}

	public virtual void TryFeed(HandApply touchSource)
	{
		//Hand touched
		if (touchSource.HandObject == null)
		{
			Chat.AddWarningMsgFromServer(touchSource.Performer, emptyHandMessages.PickRandom());
			return;
		}

		//Check for right itemtrait
		if (!Validations.HasAnyTrait(touchSource.HandObject, acceptedItems))
		{
			Chat.AddWarningMsgFromServer(touchSource.Performer, wrongItemMessages.PickRandom());
			return;
		}

		//Is correct item
		if (despawnItemOnFeed)
		{
			if (touchSource.HandObject.TryGetComponent<Stackable>(out var stackable))
			{
				if (!stackable.ServerConsume(howManyToConsume))
				{
					//Not enough items in stack
					Chat.AddExamineMsgFromServer(touchSource.Performer, "The artifact looks unimpressed");
					return;
				}
			}
			else
			{
				Despawn.ServerSingle(touchSource.HandObject);
			}
		}

		Chat.AddExamineMsgFromServer(touchSource.Performer, acceptedItemMessages.PickRandom());

		StartCoroutine(Timer(touchSource));
	}

	private IEnumerator Timer(HandApply touchSource)
	{
		yield return WaitFor.Seconds(successTimer);

		AfterFeedEffect(touchSource);
	}

	/// <summary>
	/// Called if item was successfully fed to artifact
	/// </summary>
	/// <param name="touchSource"></param>
	public virtual void AfterFeedEffect(HandApply touchSource)
	{

	}
}
