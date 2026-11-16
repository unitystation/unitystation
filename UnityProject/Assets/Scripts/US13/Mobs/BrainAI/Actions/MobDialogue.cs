using System.Collections.Generic;
using US13.Core.Chat;
using US13.Managers;
using Util;

namespace US13.Mobs.BrainAI.Actions
{
	public static class MobDialogue
	{
		public static void Speak(BrainMobAI ai, List<AudibleMobDialogue> dialogue)
		{
			if (dialogue == null || dialogue.Count == 0 || !ai.TryGetSettings<BotTaskSettings>(out _) || string.IsNullOrWhiteSpace(ai.Body?.LivingHealth?.playerScript?.playerName)) return;
			var line = dialogue.PickRandom();
			Chat.AddLocalMsgToChat(line.transcription, ai.Body.gameObject, ai.GetSettings<BotTaskSettings>().Language, ai.Body.LivingHealth.playerScript.playerName, true);
			if (line.audioSource != null) SoundManager.PlayNetworkedAtPos(line.audioSource, ai.Body.gameObject.AssumedWorldPosServer(), global: false);
		}
		public static void Sound(BrainMobAI ai, US13.Core.Addressables.Types.AddressableAudioSource sound)
		{
			if (sound != null && ai.Body != null) SoundManager.PlayNetworkedAtPos(sound, ai.Body.gameObject.AssumedWorldPosServer());
		}
	}
}
