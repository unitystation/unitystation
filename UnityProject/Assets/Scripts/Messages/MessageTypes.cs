internal enum MessageTypes : short
{
	//Server messages - 1xxx
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
	InfoWindowMessage = 1011,
	PlayerMoveMessage = 1012,
	MatrixMoveMessage = 1013,
	ShootMessage = 1014,
	TabUpdateMessage = 1015,

	//Client messages - 2xxx
	UpdateHeadsetKeyMessage = 2000,
	InteractMessage = 2001,
	InventoryInteractMessage = 2002,
	PostToChatMessage = 2003,
//	RemoveEncryptionKeyMessage = 2004, -was redundant, this id is free now
	SimpleInteractMessage = 2005,
	RequestSyncMessage = 2006,
	RequestAuthMessage = 2007,
	RequestMoveMessage = 2008,
	RequestShootMessage = 2009,
	TabInteractMessage = 2010,
	
}