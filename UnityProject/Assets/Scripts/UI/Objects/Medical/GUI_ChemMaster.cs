using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chemistry;
using Chemistry.Components;
using UnityEngine.UI;
using System.Text;
using Items;
using TMPro;

namespace UI.Objects.Chemistry
{
	public class GUI_ChemMaster : NetTab
	{
		public ChemMaster ChemMaster;

		#region Serialized Fields
		[SerializeField]
		private NetPageSwitcher mainSwitcher;
		[SerializeField]
		private NetPage containerReagentList;
		[SerializeField]
		private NetPage bufferControlList;
		[SerializeField]
		private NetPage productOutList;
		[SerializeField]
		private NetButtonAuth ejectandClear;
		[SerializeField]
		private EmptyItemList containerList;
		[SerializeField]
		private EmptyItemList bufferList;
		[SerializeField]
		private NetPageSwitcher customAmountInputPromptSwitcher;
		[SerializeField]
		private NetPage customAmountInputPromptPage;
		[SerializeField]
		private NetPage noCustomPromptPage;
		[SerializeField]
		private NetLabel customAmountLabel;
		[SerializeField]
		private NetLabel customAmountReagentLabel;
		[SerializeField]
		private NetLabel productReagentList;
		[SerializeField]
		private NetLabel productAmountToDispense;
		[SerializeField]
		private NetLabel productAmountsList;
		[SerializeField]
		private Scrollbar containerScrollbar;
		[SerializeField]
		private Scrollbar bufferScrollbar;
		[SerializeField]
		private NetLabel containerNoReagent;
		[SerializeField]
		private NetLabel bufferNoReagent;
		[SerializeField]
		private NetLabel transferModeButtonLabel;
		[SerializeField]
		private EmptyItemList productList;
		[SerializeField]
		private NetLabel productTypeChoice;
		[SerializeField]
		private NetLabel productMaxAmount;
		[SerializeField]
		private TMP_InputField productNameInputField;
		[SerializeField]
		private GameObject inputFieldBackgroundText;
		[SerializeField]
		private int customProductNameCharacterLimit;
		#endregion

		#region Initialization

		protected override void InitServer()
		{
			StartCoroutine(WaitForProvider());
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}
			// Makes sure it connects with the machine
			ChemMaster = Provider.GetComponentInChildren<ChemMaster>();
			// Subscribe to change event from ChemMaster.cs
			ChemMaster.changeEvent += UpdateAll;
			UpdateAll();
			containerNoReagent.SetValueServer("No container");
			bufferNoReagent.SetValueServer("No reagents in buffer");
			productAmountToDispense.SetValueServer($"{productDispenseAmount}");
			productTypeChoice.SetValueServer($"Please select product Type");
			productMaxAmount.SetValueServer("");
			productNameInputField.characterLimit = customProductNameCharacterLimit;
			UpdateProductOptions();
		}
		#endregion

		#region Custom Transfer Amount UI
		private float customTransferAmount;
		private Reagent customTransferReagentCandidate;
		private bool sendCustomAmountToBuffer;
		public void OpenCustomPrompt(Reagent newTransferReagentCandidate, bool toBuffer)
		{
			sendCustomAmountToBuffer = toBuffer;
			customAmountInputPromptSwitcher.SetActivePage(customAmountInputPromptPage);
			customTransferAmount = 0;
			customAmountLabel.SetValueServer($"---");
			customTransferReagentCandidate = newTransferReagentCandidate;
			customAmountReagentLabel.SetValueServer($"{customTransferReagentCandidate}");
		}

		public void CloseCustomPrompt()
		{
			customAmountInputPromptSwitcher.SetActivePage(noCustomPromptPage);
			customTransferReagentCandidate = null;
		}

		public void AddTransferAmountDigit(int digit)
		{
			if (customTransferAmount > 0) customTransferAmount = (customTransferAmount * 10) + digit;
			else customTransferAmount = digit;
			if (sendCustomAmountToBuffer)
			{
				if (customTransferAmount > ChemMaster.GetBufferSpace())
					customTransferAmount = ChemMaster.GetBufferSpace();
				if (customTransferAmount > ChemMaster.GetBufferCapacity())
					customTransferAmount = ChemMaster.GetBufferCapacity();
				if (customTransferAmount > ChemMaster.Container.CurrentReagentMix[customTransferReagentCandidate])
					customTransferAmount = ChemMaster.Container.CurrentReagentMix[customTransferReagentCandidate];
			}
			else
			{
				if (customTransferAmount > ChemMaster.Container.MaxCapacity - ChemMaster.Container.ReagentMixTotal)
					customTransferAmount = ChemMaster.Container.MaxCapacity - ChemMaster.Container.ReagentMixTotal;
				if (customTransferAmount > ChemMaster.Container.MaxCapacity)
					customTransferAmount = ChemMaster.Container.MaxCapacity;
				if (customTransferAmount > ChemMaster.GetBufferMix()[customTransferReagentCandidate])
					customTransferAmount = ChemMaster.GetBufferMix()[customTransferReagentCandidate];
			}
			customAmountLabel.SetValueServer($"{customTransferAmount:F2}u");
		}

		public void RemoveTransferAmountDigit()
		{
			customTransferAmount = (int)(customTransferAmount / 10);
			customAmountLabel.SetValueServer($"{customTransferAmount:F2}u");
		}

		public void TransferCustomAmount()
		{
			if (sendCustomAmountToBuffer)
			{
				ChemMaster.TransferContainerToBuffer(customTransferReagentCandidate, customTransferAmount);
			}
			else
			{
				ChemMaster.TransferBufferToContainer(customTransferReagentCandidate, customTransferAmount);
			}
			CloseCustomPrompt();
		}

		public void Analyze(Reagent reagent, ConnectedPlayer player)
		{
			Chat.AddExamineMsg(player.GameObject, $"This is {reagent.Name}. {reagent.description}");
		}
		#endregion

		#region Product Logistics
		private int productChoice = -1;

		public void SelectProduct(int choice)
		{
			productChoice = choice;
			GameObject product = ChemMaster.ChemMasterProducts[choice];
			productTypeChoice.SetValueServer($"{product.GetComponent<ItemAttributesV2>().InitialName}s");
			productMaxAmount.SetValueServer($"Max {product.GetComponent<ReagentContainer>().MaxCapacity}u");
			foreach(var entry in productList.Entries)
			{
				if (entry.transform.GetSiblingIndex() == choice)
				{
					entry.GetComponentInChildren<NetButton>().SetValueServer($"false");
				}
				else
				{
					entry.GetComponentInChildren<NetButton>().SetValueServer($"true");
				}
			}
		}

		private int productDispenseAmount=1;

		public void IncrementProductAmount()
		{
			productDispenseAmount = Mathf.Clamp(productDispenseAmount + 1, 1, 10);
			productAmountToDispense.SetValueServer($"{productDispenseAmount}");
		}

		public void DecrementProductAmount()
		{
			productDispenseAmount = Mathf.Clamp(productDispenseAmount - 1, 1, 10);
			productAmountToDispense.SetValueServer($"{productDispenseAmount}");
		}

		private string customNameInProgress = "";
		public void InputFieldBackgroundEvaluate(string input)
		{
			if(input == "")
			{
				inputFieldBackgroundText.SetActive(true);
				if (customNameInProgress != "")
				{
					productNameInputField.text = customNameInProgress;
				}
			}
			else
			{
				inputFieldBackgroundText.SetActive(false);
			}
			customNameInProgress = input;
		}

		public void DispenseProduct(string newName)
		{
			if (productChoice > -1)
			{
				ChemMaster.DispenseProduct(productChoice, productDispenseAmount,newName);
			}
			productDispenseAmount=1;
			productAmountToDispense.SetValueServer($"{productDispenseAmount}");
			productTypeChoice.SetValueServer($"Please select");
			productMaxAmount.SetValueServer("");
			inputFieldBackgroundText.SetActive(true);
			foreach (var entry in productList.Entries)
			{
				entry.GetComponentInChildren<NetButton>().SetValueServer($"true");
			}
		}
		#endregion

		#region Reagent Management
		public void TransferContainerToBuffer(Reagent reagent, float amount)
		{
			ChemMaster.TransferContainerToBuffer(reagent, amount);
		}
		public void BufferTransfer(Reagent reagent, float amount)
		{
			if (transferBack)
			{
				TransferBufferToContainer(reagent, amount);
			}
			else
			{
				RemoveFromBuffer(reagent, amount);
			}
		}
		public void TransferBufferToContainer(Reagent reagent, float amount)
		{
			ChemMaster.TransferBufferToContainer(reagent, amount);
		}
		public void RemoveFromBuffer(Reagent reagent, float amount)
		{
			ChemMaster.RemoveFromBuffer(reagent, amount);
		}
		public void ClearBuffer()
		{
			ChemMaster.ClearBuffer();
		}

		private bool transferBack = true;
		public void ToggleTransferMode()
		{
			transferBack = !transferBack;
			string temp = transferBack ? "Container" : "Disposal";
			transferModeButtonLabel.SetValueServer($"Transfering to {temp}");
		}
		#endregion

		#region NetUI Logistics

		/// <summary>
		/// Updates contents of Container Page with current list of reagents and amounts, and provides
		///		feedback if there is a lack of reagents or container
		/// </summary>
		public void DisplayContainerReagents()
		{
			if (ChemMaster.Container)
			{
				if (ChemMaster.Container.CurrentReagentMix.Total != 0)
				{
					ReagentMix tempMix = ChemMaster.Container.CurrentReagentMix;
					containerList.Clear();
					containerList.SetItems(tempMix.reagents.Count);
					int i = 0;
					foreach (Reagent reagent in tempMix.reagents.Keys)
					{
						GUI_ChemContainerEntry thing = containerList.Entries[i] as GUI_ChemContainerEntry;
						thing.ReInit(reagent, tempMix.reagents[reagent], GetComponent<GUI_ChemMaster>());
						i++;
					}
					containerNoReagent.SetValueServer("");
				}
				else
				{
					containerList.Clear();
					containerNoReagent.SetValueServer("No Reagents in Container");
				}
				//we have a container that is capable of being ejected
				ejectandClear.SetValueServer("true");
			}
			else
			{
				containerList.Clear();
				containerNoReagent.SetValueServer("No container");
				ejectandClear.SetValueServer("false");
			}
		}

		/// <summary>
		/// Updates contents of Buffer Page with current list of reagents and amounts, and provides
		///		feedback if there is a lack of reagents or containers
		/// </summary>
		public void DisplayBufferReagents()
		{
			if (ChemMaster.GetBufferMix() != null)
			{
				ReagentMix tempMix = ChemMaster.GetBufferMix();
				bufferList.Clear();
				bufferList.SetItems(tempMix.reagents.Count);
				int i = 0;
				foreach (Reagent reagent in tempMix.reagents.Keys)
				{
					GUI_ChemBufferEntry thing = bufferList.Entries[i] as GUI_ChemBufferEntry;
					thing.ReInit(reagent, tempMix.reagents[reagent], GetComponent<GUI_ChemMaster>());
					i++;
				}
				bufferNoReagent.SetValueServer("");
			}
			else
			{
				bufferList.Clear();
				if (!ChemMaster.BufferslotOne && !ChemMaster.BufferslotTwo)
					bufferNoReagent.SetValueServer("No containers in buffer");
				else bufferNoReagent.SetValueServer("No reagents in buffer");
			}
		}

		/// <summary>
		/// Updates contents of Product Page with current list of reagents and amounts, and provides
		///		feedback if there is a lack of reagents
		/// </summary>
		public void DisplayProductReagents()
		{
			if (ChemMaster.GetBufferMix() != null)
			{
				ReagentMix tempMix = ChemMaster.GetBufferMix();
				StringBuilder reagentListStr = new StringBuilder();
				StringBuilder amountsListStr = new StringBuilder();
				foreach (Reagent reagent in tempMix.reagents.Keys)
				{
					reagentListStr.Append($"{reagent.Name}\n");
					amountsListStr.Append($"{tempMix.reagents[reagent]}u\n");
				}
				productReagentList.SetValueServer(reagentListStr.ToString());
				productAmountsList.SetValueServer(amountsListStr.ToString());
			}
			else
			{
				productReagentList.SetValueServer("No reagents in the buffer");
				productAmountsList.SetValueServer("");
			}
			productNameInputField.text = customNameInProgress;
		}

		/// <summary>
		/// Updates Product Page buttons with available
		/// </summary>
		private void UpdateProductOptions()
		{
			productList.Clear();
			foreach (GameObject listItemin in ChemMaster.ChemMasterProducts)
			{
				if (listItemin == null)
				{
					continue;
				}
				var thing = productList.AddItem().GetComponent<GUI_ChemProductEntry>();
				thing.ReInit(this);
			}
		}

		public void SwitchPageToContainer()
		{
			mainSwitcher.SetActivePage(containerReagentList);
		}
		public void SwitchPageToBuffer()
		{
			mainSwitcher.SetActivePage(bufferControlList);
		}
		public void SwitchPageToProducts()
		{
			mainSwitcher.SetActivePage(productOutList);
		}

		public void DisablePlayerForTextInput()
		{
			UIManager.IsInputFocus = true;
			UIManager.PreventChatInput = true;
		}

		public void EnablePlayerAfterTextInput()
		{
			UIManager.IsInputFocus = false;
			UIManager.PreventChatInput = false;
		}

		public void UpdateAll()
		{
			DisplayContainerReagents();
			DisplayBufferReagents();
			DisplayProductReagents();
			UpdateProductOptions();
		}
		#endregion

		/// <summary>
		/// Ejects input container from ChemMaster into best slot available
		/// </summary>
		/// <param name="player"></param>
		public void EjectContainer(ConnectedPlayer player)
		{
			if (ChemMaster.Container != null)
			{
				ChemMaster.EjectContainer(player);
			}
			else
			{
				Logger.LogWarning("Attempted to eject from a ChemMaster without container", Category.Interaction);
			}
		}

		public void OnDestroy()
		{
			ChemMaster.changeEvent -= UpdateAll;
		}
	}
}