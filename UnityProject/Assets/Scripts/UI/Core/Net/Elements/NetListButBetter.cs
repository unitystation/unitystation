using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace UI.Core.NetUI
{
	public class NetListButBetter<T> : NetUIStringElement
	{
		public override ElementMode InteractionMode => ElementMode.ServerWrite;

		public List<T> ListElements = new List<T>();

		private readonly List<T> PreviousListElements = new List<T>();

		public override string Value
		{
			get => JsonConvert.SerializeObject(ListElements);
			protected set
			{
				if (CustomNetworkManager.IsServer) return;
				ListElements = JsonConvert.DeserializeObject<List<T>>(value);
				RegisterListChange();
			}
		}

		private void RegisterListChange()
		{
			ElementsChanged(ListElements, PreviousListElements);
			PreviousListElements.Clear();
			PreviousListElements.AddRange(ListElements);
		}

		public virtual void ElementsChanged(List<T> NewList, List<T> OldList) { }

		public void AddElement(T addElement)
		{
			ListElements.Add(addElement);
			RegisterListChange();
			UpdatePeepers();
		}

		public void RemoveElement(T addElement)
		{
			ListElements.Remove(addElement);
			RegisterListChange();
			UpdatePeepers();
		}

		public void AddRange(List<T> elements)
		{
			ListElements.AddRange(elements);
			RegisterListChange();
			UpdatePeepers();
		}

		public void Replace(List<T> elements)
		{
			ListElements = elements;
			RegisterListChange();
			UpdatePeepers();
		}

		public void Clear()
		{
			ListElements.Clear();
			RegisterListChange();
			UpdatePeepers();
		}
	}
}
