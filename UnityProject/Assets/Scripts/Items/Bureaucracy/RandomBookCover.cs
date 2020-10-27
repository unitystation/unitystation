using System.Collections;
using UnityEngine;

namespace Items.Bureaucracy
{
	public class RandomBookCover : MonoBehaviour
	{
		[SerializeField]
		private SpriteDataSO[] catalogue = default;

		private void Start()
		{
			SpriteHandler spriteHandler = GetComponentInChildren<SpriteHandler>();
			spriteHandler.SetSpriteSO(catalogue[Random.Range(0, catalogue.Length)]);
		}
	}
}
