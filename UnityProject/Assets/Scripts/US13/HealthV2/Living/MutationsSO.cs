using UnityEngine;
using UnityEngine.Serialization;
using US13.HealthV2.Living.BodyParts;
using US13.HealthV2.Living.CirculatorySystem;

namespace US13.HealthV2.Living
{
	[CreateAssetMenu(fileName = "_DoNotUse", menuName = "ScriptableObjects/Mutations/_DoNotUse")]
	public class MutationSO : ScriptableObject
	{
		[SerializeField]
		private string displayName = "";

		[SerializeField] private bool isStableMutation = false;
		[SerializeField] private bool canBeChemicallyRemoved = true;

		public bool CanBeChemicallyRemoved => canBeChemicallyRemoved;

		public string DisplayName
		{
			get
			{
				if (string.IsNullOrEmpty(displayName) == false)
				{
					return displayName;
				}
				else
				{
					return name;
				}
			}
		}

		[Tooltip(" Effects the type of dinosaur that spawned when An egg is generated, Hire equals more aggressive and dangerous Dinosaurs")]
		[Range(0, 100)] public int ResearchDifficult;

		[SerializeField, FormerlySerializedAs("Stability"), Tooltip("The stability says if this is a negative or positive mutation in terms of balancing, E.G x-ray will give - stability, while a negative mutation for example blindness will give positive stability, " +
		                                                                     "this balances out the game preventing you from having to many overpowered mutations, because you need to have a few mutations that are disadvantages")]
		private int stability = 0;


		public int Stability
		{
			get => stability;
			set
			{
				if (isStableMutation == false) stability = value;
				else stability = 0; //A stable mutation doesn't effect stability, i.e doesn't result in any positive/negative gains
			}
		}


		[Tooltip(" Description of the Mutation ")]
		public string Description;

		[Tooltip("for the Slider mini game puzzle old implementation, makes it so the slide puzzle is not necessarily solvable without using Locks")]
		public bool CanRequireLocks = false;




		public virtual Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new Mutation(BodyPart,_RelatedMutationSO);
		}
	}



	public class Mutation
	{
		public MutationSO RelatedMutationSO;

		public int Stability = 0;

		public BodyPart BodyPart;

		public Mutation(BodyPart _BodyPart,MutationSO _RelatedMutationSO)
		{
			BodyPart = _BodyPart;
			RelatedMutationSO = _RelatedMutationSO;
		}

		public virtual void SetUp()
		{


		}

		public virtual void Remove()
		{
		}
	}
}