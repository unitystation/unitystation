using System.Collections;
using UnityEngine;
using NaughtyAttributes;

namespace UI.Core.GUI.Components
{
	/// <summary>
	/// <para>Intended for use on diegetic adhesive labels stuck onto an object.</para>
	/// <para>Varies the position and rotation of the label at initialisation.</para>
	/// <para>See the <see cref="GUI_Acu"/> for a demonstration.</para>
	/// </summary>
	public class DiegeticLabel : MonoBehaviour
	{
		[SerializeField, MinMaxSlider(-100, 100)]
		private Vector2 positionXRange = new Vector2(-30, 30);
		[SerializeField, MinMaxSlider(-100, 100)]
		private Vector2 positionYRange = new Vector2(-5, 5);

		[SerializeField, MinMaxSlider(-20, 20)]
		private Vector2 rotationRange = new Vector2(-4, 4);

		private void Start()
		{
			// Give the gameobject a random translation within the set range
			var position = transform.localPosition;
			position.x += Random.Range(positionXRange.x, positionXRange.y);
			position.y += Random.Range(positionYRange.x, positionYRange.y);
			transform.localPosition = position;

			// Give the gameobject a random rotation within the set range
			var rotation = transform.localEulerAngles;
			rotation.z += Random.Range(rotationRange.x, rotationRange.y);
			transform.localEulerAngles = rotation;
		}
	}
}
