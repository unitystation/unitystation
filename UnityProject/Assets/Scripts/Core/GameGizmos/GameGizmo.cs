using InGameGizmos;
using UnityEngine;

namespace Core.GameGizmos
{
	public class GameGizmo : MonoBehaviour
	{

		public float SecondsToLive = -1;

		public void OnDestroy()
		{
			GameGizmomanager.Instance.OrNull()?.ActiveGizmos?.Remove(this);
		}
		public void Remove()
		{
			Destroy(this.gameObject);
		}

		public virtual string Serialisable()
		{
			return "";
		}

		public virtual void DeSerialisable(string Data)
		{
		}

		public void Update()
		{
			CheckTimeToLive();
		}

		protected virtual void CheckTimeToLive()
		{
			if (SecondsToLive <= 0) return;

			SecondsToLive -= Time.deltaTime;
			if (SecondsToLive <= 0 || SecondsToLive.Approx(0))
			{
				GameGizmomanager.Instance.OrNull()?.ActiveGizmos?.Remove(this);
			}
		}

		public void SetTimeAlive(float time)
		{
			SecondsToLive = time;
		}
	}
}
