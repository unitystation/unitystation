using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Systems.Hacking;

namespace Tests.Hacking
{
	/// <summary>
	/// Good example script if you need to use HackingProcessBase, note It's up to you to call the open UI stuff
	/// </summary>
	public class HackingTestScript : MonoBehaviour
	{
		public HackingProcessBase HackingProcessBase;

		private void Start()
		{
			HackingProcessBase.RegisterPort(Bob2, this.GetType());
			HackingProcessBase.RegisterPort(Jane2, this.GetType());
			HackingProcessBase.RegisterPort(Cat2, this.GetType());
		}

		[Button]
		public void Bob()
		{
			HackingProcessBase.ImpulsePort(Bob2);
		}

		public void Bob2()
		{
			Logger.Log("BOB");
		}

		[Button]
		public void Jane()
		{
			HackingProcessBase.ImpulsePort(Jane2);
		}

		public void Jane2()
		{
			Logger.Log("Jane");
		}


		[Button]
		public void Cat()
		{
			HackingProcessBase.ImpulsePort(Cat2);
		}

		public void Cat2()
		{
			Logger.Log("Cat");
		}
	}
}
