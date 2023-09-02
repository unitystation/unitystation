using Logs;
using NPC.Mood;
using UnityEngine;

namespace NPC.AI
{
	/// <summary>
	/// Makes it possible to get an idea what the current mood is for this mob by simply
	/// examining it.
	/// </summary>
	[RequireComponent(typeof(MobMood))]
	public class ExaminableMood: MonoBehaviour, IExaminable
	{
		[SerializeField] [Tooltip("Strings that represent a perfect mood. From 90% to 100%")]
		private string[] veryGoodMood = {"It looks incredibly happy and healthy!"};

		[SerializeField] [Tooltip("Strings that represent a good mood. From 70% to 89%")]
		private string[] goodMood = {"It looks happy.", "It looks healthy."};

		[SerializeField] [Tooltip("Strings that represent a normal mood. From 41% to 69%")]
		private string[] normalMood = {""};

		[SerializeField] [Tooltip("Strings that represent a bad mood. That is from 1% to 40%")]
		private string[] badMood =
		{
			"It looks kind of sad.",
			"It looks stressed out.",
			"It looks badly treated."
		};


		private MobMood mood;
		private void Awake()
		{
			mood = GetComponent<MobMood>();
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			switch (mood.LevelPercent)
			{
				case int n when n > 89:
					return veryGoodMood.PickRandom();
				case int n when n.IsBetween(70, 89):
					return goodMood.PickRandom();
				case int n when n.IsBetween(41, 69):
					return normalMood.PickRandom();
				case int n when n < 41:
					return badMood.PickRandom();
				default:
					Loggy.LogError(
						$"Examinable Mood got unexpected range of mood level: {mood.LevelPercent}",
						Category.Interaction);
					break;
			}

			return normalMood.PickRandom();
		}
	}
}