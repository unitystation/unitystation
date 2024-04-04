using System;
using System.Collections;
using System.Collections.Generic;
using Logs;
using Messages.Client.VariableViewer;
using Newtonsoft.Json;
using SecureStuff;
using TMPro;
using UnityEngine;

public class GUI_P_Component : PageElement
{
	public static bool VVObjectComponentSelectionActive = false;

	public static GUI_P_Component ActiveComponent;

	public GameObject UIOptions;

	public TMP_Text Text;
	public bool IsSentence;

	public bool iskey;
	//buttun to set
	//ubtton to opne?

	public struct EditData
	{
		public IDType IDType;
		public ClientObjectPath.PathData ClientGameObject; //TODO Can't distinguish between multiple of the same component
		public ulong BookID;
		public ulong ShelfID;
	}

	public enum IDType
	{
		NULL,
		Book,
		Bookshelf
	}

	public override PageElementEnum PageElementType => PageElementEnum.Component;

	public override bool IsThisType(Type TType)
	//TODO support coomponents in the future
	{
		return TType.IsSubclassOf(typeof(MonoBehaviour)) || TType == typeof(GameObject);
	}

	public override void SetUpValues(
		Type ValueType, VariableViewerNetworking.NetFriendlyPage Page = null,
		VariableViewerNetworking.NetFriendlySentence Sentence = null, bool Iskey = false)
	{
		base.SetUpValues(ValueType, Page, Sentence, Iskey);

		Text.text = VVUIElementHandler.ReturnCorrectString(Page, Sentence, Iskey);
		if (Page != null)
		{
			PageID = Page.ID;
			SentenceID = 0;
			IsSentence = false;
			iskey = false;
		}
		else
		{
			PageID = Sentence.OnPageID;
			SentenceID = Sentence.SentenceID;
			IsSentence = true;
			iskey = Iskey;
		}
	}


	public void ShowUI()
	{
		VVObjectComponentSelectionActive = !VVObjectComponentSelectionActive;
		if (VVObjectComponentSelectionActive)
		{
			ActiveComponent = this;
			UIOptions.SetActive(true);
		}
		else
		{
			ActiveComponent = null;
			UIOptions.SetActive(false);
		}
	}



	public void Close()
	{
		VVObjectComponentSelectionActive = false;
		ActiveComponent = null;
		UIOptions.SetActive(false);
	}

	public void RequestOpenBookOnPage()
	{
		OpenPageValueNetMessage.Send(PageID, SentenceID, IsSentence, iskey);
	}

	public void SetBook(ulong BookID)
	{
		if (PageID != 0)
		{
			RequestChangeVariableNetMessage.Send(PageID,
				JsonConvert.SerializeObject(new EditData()
				{
					IDType =  IDType.Book,
					BookID = BookID
				}),
				UISendToClientToggle.toggle);
		}
	}

	public void SetBookShelf(ulong ShelfID)
	{
		if (PageID != 0)
		{
			RequestChangeVariableNetMessage.Send(PageID,
				JsonConvert.SerializeObject(new EditData()
				{
					IDType =  IDType.Bookshelf,
					ShelfID = ShelfID
				}),
				UISendToClientToggle.toggle);
		}
	}

	public override void Pool()
	{
		base.Pool();
		VVObjectComponentSelectionActive = false;
		ActiveComponent = null;
	}

	public void OnDisable()
	{
		VVObjectComponentSelectionActive = false;
		ActiveComponent = null;
	}

	public override string Serialise(object Data)
	{
		if (Data == "null")
		{
			return JsonConvert.SerializeObject(new EditData()
			{
				IDType = IDType.NULL
			});
		}

		var InType = Data.GetType();
		if (InType == typeof(GameObject))
		{
			return JsonConvert.SerializeObject(new EditData()
			{
				IDType = IDType.Bookshelf,
				ShelfID = Librarian.TransformToBookShelf[(Data as GameObject).transform].ID,
				ClientGameObject = ClientObjectPath.GetPathForMessage(Data as GameObject)
			});
		}
		else if (InType.IsSubclassOf(typeof(MonoBehaviour)))
		{
			return JsonConvert.SerializeObject(new EditData()
			{
				IDType = IDType.Bookshelf,
				BookID = Librarian.Book.GenerateNonMonoBook((Data as MonoBehaviour)).ID,
				ClientGameObject = ClientObjectPath.GetPathForMessage((Data as MonoBehaviour).gameObject)
			});

		}

		return (Data.ToString());
	}

	public override object DeSerialise(string StringVariable, Type InType, object InObject, bool SetUI = false)
	{
		EditData data = JsonConvert.DeserializeObject<EditData>(StringVariable);
		if (data.IDType == IDType.NULL)
		{
			return null;
		}
		if (CustomNetworkManager.IsServer)
		{
			if (InType == typeof(GameObject))
			{
				if (data.IDType == IDType.Book)
				{
					Loggy.LogError("reeeeee");
					return null;
				}

				if (Librarian.IDToBookShelf.ContainsKey(data.ShelfID) == false)
				{
					Loggy.LogError("reeeeee");
					return null;
				}

				return Librarian.IDToBookShelf[data.ShelfID].Shelf;
			}
			else if (InType.IsSubclassOf(typeof(Component)))
			{
				if (data.IDType == IDType.Bookshelf)
				{
					if (Librarian.IDToBookShelf.ContainsKey(data.ShelfID) == false)
					{
						Loggy.LogError("reeeeee");
						return null;
					}
					return Librarian.IDToBookShelf[data.ShelfID].Shelf.GetComponent(InType);
				}
				else
				{
					if (Librarian.IDToBook.ContainsKey(data.BookID) == false)
					{
						Loggy.LogError("reeeeee");
						return null;
					}
					return Librarian.IDToBook[data.BookID].BookClass;
				}
			}

			return null;

		}
		else
		{
			if (InType == typeof(GameObject))
			{
				var NetworkedObject = ClientObjectPath.GetObjectMessage(data.ClientGameObject);
				return NetworkedObject;
			}
			else if (InType.IsSubclassOf(typeof(MonoBehaviour)))
			{
				var NetworkedObject = ClientObjectPath.GetObjectMessage(data.ClientGameObject);
				return NetworkedObject.GetComponent(InType);
			}

			return null;
		}

	}
}