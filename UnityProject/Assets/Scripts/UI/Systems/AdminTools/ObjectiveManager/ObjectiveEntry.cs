using Antagonists;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class ObjectiveEntry : MonoBehaviour
	{
		[SerializeField] private Button removeBox;
		[SerializeField] private Button checkBox;
		[SerializeField] private Text removeBoxText;
		[SerializeField] private Text checkBoxText;
		[SerializeField] private TMP_Text objectiveDescription;
		public bool isNew = false;
		public bool IsForDelete => relatedObjective.ToDelete;

		private PlayerObjectiveManagerPage antagManager;
		private ObjectiveInfo relatedObjective;
		public ObjectiveInfo RelatedObjective => relatedObjective;
		private TMP_InputField field;
		private TMP_InputField Field
		{
			get
			{
				if (field == null)
					field = objectiveDescription.gameObject.GetComponent<TMP_InputField>();
				return field;
			}
		}


		public void RemoveObjective()
		{
			if (isNew)
			{
				antagManager.RemoveEntry(this);
			}
			else
			{
				relatedObjective.ToDelete = !relatedObjective.ToDelete;
				if (relatedObjective.ToDelete)
				{
					removeBoxText.color = new Color(1, 0, 0, 1);
				}
				else
				{
					removeBoxText.color = new Color(0.8f, 0.2f, 0.2f, 1);
				}
			}
		}

		public void UpdateObjectiveText()
		{
			relatedObjective.Description = Field.text;
		}

		public void AddObjective()
		{
			antagManager.AddEntry(new ObjectiveInfo() { Description = objectiveDescription.text, IsCustom = true});
			Field.text = "Add your custom objective...";
		}

		public void ConfirmeObjective()
		{
			if (relatedObjective.IsCustom)
			{
				relatedObjective.Status = !relatedObjective.Status;
				UpdateCheckBox();
			}
		}

		private void UpdateCheckBox()
		{
			if (relatedObjective == null)
				return;

			if (relatedObjective.PrefabID >= 0 && AntagData.Instance.FromIndexObj(relatedObjective.PrefabID).IsEndRoundObjective || relatedObjective.IsEndRound)
			{
				checkBoxText.text = "E";
			}
			else if (relatedObjective.Status)
			{
				checkBoxText.text = "X";
			}
			else
			{
				checkBoxText.text = "";
			}
		}

		public void Init(PlayerObjectiveManagerPage antagManagerPage, ObjectiveInfo objective)
		{
			antagManager = antagManagerPage;
			relatedObjective = objective;

			checkBox.interactable = objective.IsCustom;
			field = objectiveDescription.gameObject.GetComponent<TMP_InputField>();
			field.interactable = objective.IsCustom;
			field.text = objective.Description;
			UpdateCheckBox();
		}

		public void Init(PlayerObjectiveManagerPage antagManagerPage)
		{
			antagManager = antagManagerPage;
			relatedObjective = new ObjectiveInfo();
			isNew = true;
			Field.text = "Add your custom objective...";
		}
	}
}