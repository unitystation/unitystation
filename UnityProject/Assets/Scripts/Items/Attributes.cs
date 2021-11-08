using System.Linq;
using UnityEngine;
using Mirror;
using Core.Editor.Attributes;
using Messages.Client.Interaction;
using NaughtyAttributes;

[RequireComponent(typeof(Integrity))]
[RequireComponent(typeof(CustomNetTransform))]
public class Attributes : NetworkBehaviour, IRightClickable, IExaminable
{

	[Tooltip("Display name of this item when spawned.")]
	[SerializeField]
	private string initialName = null;

	[SyncVar(hook = nameof(SyncArticleName))]
	private string articleName;
	/// <summary>
	/// Current name
	/// </summary>
	public string ArticleName => articleName;

	public string InitialName => initialName;

	public string InitialDescription => initialDescription;

	[Tooltip("Description of this item when spawned.")]
	[SerializeField]
	private string initialDescription = null;

	[Tooltip("Will this item highlight on mouseover?")]
	[SerializeField, PrefabModeOnly]
	private bool willHighlight = true;

	[Tooltip("How much does one of these sell for when shipped on the cargo shuttle?")]
	[SerializeField, BoxGroup("Cargo"), PrefabModeOnly]
	private int exportCost = 0;

	public int ExportCost
	{
		get
		{
			if (TryGetComponent<Stackable>(out var stackable))
			{
				int amount = Application.isEditor ? stackable.InitialAmount : stackable.Amount;
				return exportCost * amount;
			}

			return exportCost;
		}
	}

	[SerializeField, BoxGroup("Cargo"), PrefabModeOnly]
	[Tooltip("If default, will only be considered exportable if the value is not zero and the object is movable.")]
	private CargoExportType exportType = CargoExportType.Default;
	public CargoExportType ExportType => exportType;

	[Tooltip("Should an alternate name be used when displaying this in the cargo console report?")]
	[SerializeField, BoxGroup("Cargo"), PrefabModeOnly]
	private string exportName = "";
	public string ExportName => exportName;

	[Tooltip("Additional message to display in the cargo console report.")]
	[SerializeField, BoxGroup("Cargo"), PrefabModeOnly]
	private string exportMessage = null;
	public string ExportMessage => exportMessage;

	[Server]
	public void SetExportCost(int value)
	{
		exportCost = value;
	}

	[SyncVar(hook = nameof(SyncArticleDescription))]
	private string articleDescription;

	/// <summary>
	/// Current description
	/// </summary>
	public string ArticleDescription => articleDescription;

	public override void OnStartClient()
	{
		SyncArticleName(articleName, articleName);
		SyncArticleDescription(articleDescription, articleDescription);
		base.OnStartClient();
	}

	public override void OnStartServer()
	{
		SyncArticleName(articleName, initialName);
		SyncArticleDescription(articleDescription, initialDescription);
		base.OnStartServer();
	}

	private void SyncArticleName(string oldName, string newName)
	{
		articleName = newName;
	}

	private void SyncArticleDescription(string oldDescription, string newDescription)
	{
		articleDescription = newDescription;
	}

	/// <summary>
	/// When hovering over an object or item its name and description is shown as a tooltip on the bottom-left of the screen.
	/// The first letter of the object's name is always capitalized if the object has been given a name.
	/// If there is description it is shown in parentheses.
	/// </summary>
	public void OnHoverStart()
	{
		//failsafe - don't highlight hidden / despawned stuff
		if (gameObject.IsAtHiddenPos()) return;

		if(willHighlight)
		{
			Highlight.HighlightThis(gameObject);
		}
		string displayName = null;
		if (string.IsNullOrWhiteSpace(articleName))
		{
			displayName = gameObject.ExpensiveName();
		}
		else
		{
			displayName = articleName;
		}
		//failsafe
		if (string.IsNullOrWhiteSpace(displayName)) displayName = "error";

		UIManager.SetToolTip =
			displayName.First().ToString().ToUpper() + displayName.Substring(1);
	}

	public void OnHoverEnd()
	{
		Highlight.DeHighlight();

		UIManager.SetToolTip = string.Empty;
	}

	// Sends examine event to all monobehaviors on gameobject - keep for now - TODO: integrate w shift examine
	public void SendExamine()
	{
		SendMessage("OnExamine");
	}

	private void OnExamine()
	{
		RequestExamineMessage.Send(GetComponent<NetworkIdentity>().netId);
	}

	// Initial implementation of shift examine behaviour
	public string Examine(Vector3 worldPos)
	{
		string displayName = "<error>";
		if (string.IsNullOrWhiteSpace(articleName))
		{
			displayName = gameObject.ExpensiveName();
		}
		else
		{
			displayName = articleName;
		}

		string str = "This is a " + displayName + ".";

		if (!string.IsNullOrEmpty(ArticleDescription))
		{
			str = str + " " + ArticleDescription;
		}
		return str;
	}

	public RightClickableResult GenerateRightClickOptions()
	{
		return RightClickableResult.Create()
			.AddElement("Examine", OnExamine);
	}

	public void ServerSetArticleName(string newName)
	{
		SyncArticleName(articleName, newName);
	}

	[Server]
	public void ServerSetArticleDescription(string desc)
	{
		SyncArticleDescription(articleDescription, desc);
	}

	public enum CargoExportType
	{
		/// <summary>Export if value not zero and not secured.</summary>
		Default = 0,
		Always = 1,
		Never = 2,
	}
}
