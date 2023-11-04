using System;
using System.Collections;
using Logs;
using UnityEngine;

public static class CoroutineExtensions {
	/// Tries to stop a coroutine based on a Coroutine Handle.
	/// will only stop the Coroutine if the handle is not null
	/// <returns>the Monobehaviour script running the coroutine, allowing chained commands</returns>
	/// <param name="handle">Handle.</param>
	public static MonoBehaviour TryStopCoroutine( this MonoBehaviour script, ref Coroutine handle ) {
		if ( !script )
			return null;
		if ( handle != null )
			script.StopCoroutine( handle );
		handle = null;
		return script;
	}

	/// Starts the coroutine and sets the routine to a Coroutine handle.
	/// <returns>the Monobehaviour script running the coroutine, allowing chained commands</returns>
	/// <param name="routine">Routine.</param>
	/// <param name="handle">Handle.</param>
	public static MonoBehaviour StartCoroutine( this MonoBehaviour script, IEnumerator routine, ref Coroutine handle ) {
		if ( !script ) {
#if UNITY_EDITOR
			Loggy.LogWarning( "A coroutine cannot run while it is null or being destroyed", Category.Threading );
#endif
			return null;
		}

		if ( !script.enabled || !script.gameObject.activeInHierarchy ) {
#if UNITY_EDITOR
			Loggy.LogWarningFormat( "The Script {0} is currently disabled and cannot start coroutines", Category.Threading,  script.name);
#endif
			return script;
		}

		handle = script.StartCoroutine( routine );

		return script;
	}

	/// Stops any possible coroutine running on the specified handle and runs a new routine in its place
	/// <returns>the Monobehaviour script running the coroutine, allowing chained commands</returns>
	/// <param name="script">Script.</param>
	/// <param name="routine">Routine.</param>
	/// <param name="handle">Handle.</param>
	public static MonoBehaviour RestartCoroutine( this MonoBehaviour script, IEnumerator routine, ref Coroutine handle ) {
		return script.TryStopCoroutine( ref handle )
			.StartCoroutine( routine, ref handle );
	}

	/// <summary>
	/// Ensures CustomNetworkManager.Instance is not null, then executes your action
	/// </summary>
	public static void WaitForNetworkManager(this MonoBehaviour script, Action action)
	{
		if (CustomNetworkManager.Instance != null)
		{ //Don't even start a coroutine if Instance exists
			action.Invoke();
		}

		IEnumerator WaitForNetworkManager(Action a)
		{
			while (CustomNetworkManager.Instance == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			a.Invoke();
		}

		script.StartCoroutine(WaitForNetworkManager(action));
	}

}