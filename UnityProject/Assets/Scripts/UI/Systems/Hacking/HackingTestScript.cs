using System.Collections.Generic;
using Logs;
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
			Loggy.Log("BOB");
		}

		[Button]
		public void Jane()
		{
			HackingProcessBase.ImpulsePort(Jane2);
		}

		public void Jane2()
		{
			Loggy.Log("Jane");
		}


		[Button]
		public void Cat()
		{
			HackingProcessBase.ImpulsePort(Cat2);
		}

		public void Cat2()
		{
			Loggy.Log("Cat");
		}
	}
}
