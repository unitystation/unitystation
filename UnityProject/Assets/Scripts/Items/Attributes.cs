using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using System;
using Mirror;
using Core.Editor.Attributes;
using Detective;
using Messages.Client.Interaction;
using NaughtyAttributes;

[RequireComponent(typeof(Integrity))]
public class Attributes : NetworkBehaviour, IRightClickable, IExaminable, IServerSpawn
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
	[TextArea(3,5)]
	private string initialDescription = null;

	[Tooltip("Will this item highlight on mouseover?")]
	[SerializeField, PrefabModeOnly]
	private bool willHighlight = true;

	[Tooltip("How much does one of these sell for when shipped on the cargo shuttle?")]
	[SerializeField, BoxGroup("Cargo"), PrefabModeOnly]
	private int exportCost = 0;

	public class ExportEvent : UnityEvent<string, string> { }

	/// <summary>
	/// Server-side event invoked when this object is sold at cargo
	/// </summary>
	[NonSerialized]
	public ExportEvent onExport = new ExportEvent();

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

	[SerializeField, BoxGroup("Cargo")]
	[Tooltip("Can this be sold while oboard a cargo shuttle?")]
	public bool CanBeSoldInCargo = true;

	[Tooltip("Should an alternate name be used when displaying this in the cargo console report?")]
	[SerializeField, BoxGroup("Cargo"), PrefabModeOnly]
	private string exportName = "";
	public string ExportName => exportName;

	[Tooltip("Additional message to display in the cargo console report.")]
	[SerializeField, BoxGroup("Cargo"), PrefabModeOnly]
	[TextArea(3,5)]
	private string exportMessage = null;
	public string ExportMessage => exportMessage;

	[Server]
	public void SetExportCost(int value)
	{
		exportCost = value;
	}


	[Server]
	public void OnExport()
	{
		onExport.Invoke(exportName, exportMessage);
	}

	[SyncVar(hook = nameof(SyncArticleDescription))]
	private string articleDescription;


	/// <summary>
	/// Sizes:
	/// Tiny - pen, coin, pills. Anything you'd easily lose in a couch.
	/// Small - Pocket-sized items. You could hold a couple in one hand, but ten would be a hassle without a bag. Apple, phone, drinking glass etc.
	/// Medium - default size. Fairly bulky but stuff you could carry in one hand and stuff into a backpack. Most tools would fit this size.
	/// Large - particularly long or bulky items that would need a specialised bag to carry them. A shovel, a snowboard etc or wall mounts, kitchen appliance.
	/// Huge - Think, like, a fridge. Absolute unit. You aren't stuffing this into anything less than a shipping crate or plasma generator.
	/// Massive - Particle accelerator piece, takes up the entire tile.
	/// Humongous - Multi-block/Sprite stretches across multiple tiles structures such as the gateway
	/// </summary>
	[Tooltip("Size of this item when spawned. Is medium by default, which you should change if needed.")]
	[SerializeField]
	private Size initialSize = global::Size.Medium;

	/// <summary>
	/// Current size.
	/// </summary>
	[SyncVar(hook = nameof(SyncSize))]
	private Size size;

	/// <summary>
	/// Current size
	/// </summary>
	public Size Size => size;

	/// <summary>
	/// Current description
	/// </summary>
	public string ArticleDescription => articleDescription;


	/// <summary>
	/// For the detectives Scanner
	/// </summary>
	public AppliedDetails AppliedDetails = new AppliedDetails();

	public override void OnStartClient()
	{
		SyncArticleName(articleName, articleName);
		SyncArticleDescription(articleDescription, articleDescription);
		SyncSize(size, this.size);
		base.OnStartClient();
	}

	public override void OnStartServer()
	{
		SyncArticleName(articleName, initialName);
		SyncArticleDescription(articleDescription, initialDescription);
		base.OnStartServer();
	}

	private void SyncSize(Size oldSize, Size newSize)
	{
		size = newSize;
	}

	/// <summary>
	/// Change this item's size and sync it to clients.
	/// </summary>
	/// <param name="newSize"></param>
	[Server]
	public void ServerSetSize(Size newSize)
	{
		SyncSize(size, newSize);
	}

	private void SyncArticleName(string oldName, string newName)
	{
		articleName = newName;
	}

	private void SyncArticleDescription(string oldDescription, string newDescription)
	{
		articleDescription = newDescription;
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		size = initialSize;
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
		RequestExamineMessage.Send(netId);
	}

	private void OnPointTo()
	{
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdPoint(gameObject, gameObject.AssumedWorldPosServer());
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
			.AddElement("Examine", OnExamine)
			.AddElement("PointTo", OnPointTo);
	}

	public void ServerSetArticleName(string newName)
	{
		if (gameObject.TryGetComponent<Stackable>(out var stack))
		{
			newName = $"{newName} ({stack.Amount})";
		}
		newName = newName.Replace("[item]", $"{initialName}");
		SyncArticleName(articleName, newName);
	}

	[Server]
	public void ServerSetArticleDescription(string desc)
	{
		SyncArticleDescription(articleDescription, desc);
	}
}
