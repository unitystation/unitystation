using UnityEngine;

/// <summary>
/// Made by Feiko Joosten
/// 
/// I have based this code on this blogpost. Decided to give it more functionality. http://blogs.unity3d.com/2015/12/23/1k-update-calls/
/// Use this to speed up your performance when you have a lot of update, fixed update and or late update calls in your scene
/// Let the object you want to give increased performance inherit from ManagedNetworkBehaviour
/// Replace your void Update() for public override void UpdateMe()
/// Or replace your void FixedUpdate() for public override void FixedUpdateMe()
/// Or replace your void LateUpdate() for public override void LateUpdateMe()
/// ManagedNetworkBehaviour will add the object to the update manager
/// UpdateManager will handle all of the update calls
/// 
/// Doobly notes: ManagedNetworkBehaviour just means that the monobehaviour base can be overriden
/// </summary>

public class UpdateManager : MonoBehaviour
{
	private static UpdateManager instance;

	private int regularUpdateArrayCount = 0;
	private int fixedUpdateArrayCount = 0;
	private int lateUpdateArrayCount = 0;
	private ManagedNetworkBehaviour[] regularArray = new ManagedNetworkBehaviour[0];
	private ManagedNetworkBehaviour[] fixedArray = new ManagedNetworkBehaviour[0];
	private ManagedNetworkBehaviour[] lateArray = new ManagedNetworkBehaviour[0];

	public UpdateManager()
	{
		instance = this;
	}

	public static void AddItem(ManagedNetworkBehaviour behaviour)
	{
		instance.AddItemToArray(behaviour);
	}

	public static void RemoveSpecificItem(ManagedNetworkBehaviour behaviour)
	{
		instance.RemoveSpecificItemFromArray(behaviour);
	}

	public static void RemoveSpecificItemAndDestroyIt(ManagedNetworkBehaviour behaviour)
	{
		instance.RemoveSpecificItemFromArray(behaviour);

		Destroy(behaviour.gameObject);
	}

	private void AddItemToArray(ManagedNetworkBehaviour behaviour)
	{
		if (behaviour.GetType().GetMethod("UpdateMe").DeclaringType != typeof(ManagedNetworkBehaviour))
		{
			regularArray = ExtendAndAddItemToArray(regularArray, behaviour);
			regularUpdateArrayCount++;
		}

		if (behaviour.GetType().GetMethod("FixedUpdateMe").DeclaringType != typeof(ManagedNetworkBehaviour))
		{
			fixedArray = ExtendAndAddItemToArray(fixedArray, behaviour);
			fixedUpdateArrayCount++;
		}

		if (behaviour.GetType().GetMethod("LateUpdateMe").DeclaringType == typeof(ManagedNetworkBehaviour))
			return;

		lateArray = ExtendAndAddItemToArray(lateArray, behaviour);
		lateUpdateArrayCount++;
	}

	public ManagedNetworkBehaviour[] ExtendAndAddItemToArray(ManagedNetworkBehaviour[] original, ManagedNetworkBehaviour itemToAdd)
	{
		int size = original.Length;
		ManagedNetworkBehaviour[] finalArray = new ManagedNetworkBehaviour[size + 1];
		for (int i = 0; i < size; i++)
		{
			finalArray[i] = original[i];
		}
		finalArray[finalArray.Length - 1] = itemToAdd;
		return finalArray;
	}

	private void RemoveSpecificItemFromArray(ManagedNetworkBehaviour behaviour)
	{
		if (CheckIfArrayContainsItem(regularArray, behaviour))
		{
			regularArray = ShrinkAndRemoveItemToArray(regularArray, behaviour);
			regularUpdateArrayCount--;
		}

		if (CheckIfArrayContainsItem(fixedArray, behaviour))
		{
			fixedArray = ShrinkAndRemoveItemToArray(fixedArray, behaviour);
			fixedUpdateArrayCount--;
		}

		if (!CheckIfArrayContainsItem(lateArray, behaviour)) return;

		lateArray = ShrinkAndRemoveItemToArray(lateArray, behaviour);
		lateUpdateArrayCount--;
	}

	public bool CheckIfArrayContainsItem(ManagedNetworkBehaviour[] arrayToCheck, ManagedNetworkBehaviour objectToCheckFor)
	{
		int size = arrayToCheck.Length;

		for (int i = 0; i < size; i++)
		{
			if (objectToCheckFor == arrayToCheck[i]) return true;
		}

		return false;
	}

	public ManagedNetworkBehaviour[] ShrinkAndRemoveItemToArray(ManagedNetworkBehaviour[] original, ManagedNetworkBehaviour itemToRemove)
	{
		int size = original.Length;
		ManagedNetworkBehaviour[] finalArray = new ManagedNetworkBehaviour[size - 1];
		for (int i = 0; i < size; i++)
		{
			if (original[i] == itemToRemove) continue;

			finalArray[i] = original[i];
		}
		return finalArray;
	}

	private void Update()
	{
		if (regularUpdateArrayCount == 0) return;

		for (int i = 0; i < regularUpdateArrayCount; i++)
		{
			if (regularArray[i] == null) continue;

			regularArray[i].UpdateMe();
		}
	}

	private void FixedUpdate()
	{
		if (fixedUpdateArrayCount == 0) return;

		for (int i = 0; i < fixedUpdateArrayCount; i++)
		{
			if (fixedArray[i] == null) continue;

			fixedArray[i].FixedUpdateMe();
		}
	}

	private void LateUpdate()
	{
		if (lateUpdateArrayCount == 0) return;

		for (int i = 0; i < lateUpdateArrayCount; i++)
		{
			if (lateArray[i] == null) continue;

			lateArray[i].LateUpdateMe();
		}
	}
}











