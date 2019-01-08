using UnityEngine.Networking;


	public abstract class ObjectTrigger : NetworkBehaviour
	{
		public abstract void Trigger(bool iState);
	}
