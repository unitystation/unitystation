internal enum MessageTypes : short
{
	GibMessage = 1000,
	RunMethodMessage = 1001,
	UpdateChatMessage = 1002,
	UpdateConnectedPlayersMessage = 1003,
	UpdateRoundTimeMessage = 1004,
	UpdateSlotMessage = 1005,
	UpdateUIMessage = 1006,
	ClosetHandlerMessage = 1007,
	ForceJobListUpdateMessage = 1008,
	TransformStateMessage = 1009,
	PlayerDeathMessage = 1010,

	AddEncryptionKeyMessage = 2000,
	InteractMessage = 2001,
	InventoryInteractMessage = 2002,
	PostToChatMessage = 2003,
	RemoveEncryptionKeyMessage = 2004,
	SimpleInteractMessage = 2005,
	RequestSyncMessage = 2006
}