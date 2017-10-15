using System;
using System.Threading.Tasks;
using UnityEngine;

namespace UI
{
	/// <summary>
	/// To control the critical overlays (unconscious, dying, oxygen loss etc)
	/// </summary>
	public class OverlayCrits : MonoBehaviour
	{
		public Material holeMat;
		public RectTransform shroud;

		public async Task AdjustOverlayPos(){
			if (PlayGroup.PlayerManager.LocalPlayer != null) {
				await Task.Delay(TimeSpan.FromSeconds(0.1f));
				Vector3 playerPos = Camera.main.WorldToScreenPoint(PlayGroup.PlayerManager.LocalPlayer.transform.position);
				shroud.position = playerPos;
			}
		}
	}
}
