using UnityEngine;

namespace Items.Cargo.Wrapping
{
	public abstract class WrappableBase: MonoBehaviour
	{
		[SerializeField]
		[Tooltip("Text you see when you perform the wrapping action." +
		         " {0} = object being wrapped, {1} = wrapping paper name.")]
		protected string actionTextOriginator = "You start wrapping {0} with {1}.";

		[SerializeField]
		[Tooltip("Text others see when you perform the wrapping action. " +
		         "{0} = your name, {1} = object being wrapped, {2} = wrapping paper name.")]
		protected string actionTextOthers = "{0} starts wrapping {1} with {2}.";

		[SerializeField][Tooltip("Prefab that will be used when spawning the package.")]
		protected GameObject normalPackagePrefab = default;

		[SerializeField][Tooltip("Prefab that will be used when spawning the package with festive paper.")]
		protected GameObject festivePackagePrefab = default;

		[SerializeField] [Tooltip("Time needed to wrap this object")]
		protected float wrapTime = 5;

		protected abstract bool CanBeWrapped(GameObject performer, WrappingPaper paper);
		protected abstract void Wrap(GameObject performer, WrappingPaper paper);

		/// <summary>
		/// Entry point for wrapping interaction of wrappable objects/items.
		/// Will evaluate if the wrap can happen, giving a message to the performer in case it can not
		/// and doing the wrap if successful.
		/// </summary>
		/// <param name="performer"></param>
		/// <param name="paper"></param>
		public void TryWrap(GameObject performer, WrappingPaper paper)
		{
			if (CanBeWrapped(performer, paper))
			{
				Wrap(performer, paper);
			}
		}
	}
}