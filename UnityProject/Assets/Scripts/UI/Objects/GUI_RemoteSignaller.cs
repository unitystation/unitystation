using System.Collections;
using UnityEngine;

namespace trainofpainandsuffering
{
	public class GUI_RemoteSignaller : NetTab
	{
		private RemoteSignaller obj;
		[SerializeField] private NetLabel netlabel = null;

		public override void OnEnable()
		{
			base.OnEnable();
			StartCoroutine(WaitForProvider());
		}

		IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			obj = Provider.GetComponentInChildren<RemoteSignaller>();
		}

		public void UpdateFreq()
		{
			if (obj.IsOn)
			{
				int freq = obj.frequencyReceive;
				string hundred = ((freq - (freq % 100))/100).ToString();
				string tens = (freq % 100).ToString();
				tens = tens.Length >= 2 ? tens : "0" + tens;
				netlabel.SetValueServer($"{hundred}.{tens}");
			}
			else
			{
				netlabel.SetValueServer("");
			}
		}

		public void AdjFreq(int freq)
		{
			if (obj.IsOn)
			{
				obj.UpdateFreq(freq);
				UpdateFreq();
			}
		}

		public void SendSignal()
		{
			if (obj.IsOn)
			{
				obj.SendSignal();
			}
		}

		public void TogglePower()
		{
			obj.TogglePower();
			UpdateFreq();
		}
	}
}