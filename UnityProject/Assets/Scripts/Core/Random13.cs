using System;

namespace Core
{
	/// <summary>
	/// An extremely fast, efficient and deterministic class for randomness.
	/// Best reserved for server logic, while client logic can use DMMath.
	/// </summary>
	public class Random13
	{
		public Random13()
		{
			Instance = this;
		}

		public static Random13 Instance;

		//https://github.com/id-Software/DOOM/blob/77735c3ff0772609e9c8d29e3ce2ab42ff54d20b/linuxdoom-1.10/m_random.c#LL31C1-L31C1
		private readonly byte[] randomTable = new byte[]
		{
			0, 8, 109, 220, 222, 241, 149, 107, 75, 248, 254, 140, 16, 66,
			74, 21, 211, 47, 80, 242, 154, 27, 205, 128, 161, 89, 77, 36,
			95, 110, 85, 48, 212, 140, 211, 249, 22, 79, 200, 50, 28, 188,
			52, 140, 202, 120, 68, 145, 62, 70, 184, 190, 91, 197, 152, 224,
			149, 104, 25, 178, 252, 182, 202, 182, 141, 197, 4, 81, 181, 242,
			145, 42, 39, 227, 156, 198, 225, 193, 219, 93, 122, 175, 249, 0,
			175, 143, 70, 239, 46, 246, 163, 53, 163, 109, 168, 135, 2, 235,
			25, 92, 20, 145, 138, 77, 69, 166, 78, 176, 173, 212, 166, 113,
			94, 161, 41, 50, 239, 49, 111, 164, 70, 60, 2, 37, 171, 75,
			136, 156, 11, 56, 42, 146, 138, 229, 73, 146, 77, 61, 98, 196,
			135, 106, 63, 197, 195, 86, 96, 203, 113, 101, 170, 247, 181, 113,
			80, 250, 108, 7, 255, 237, 129, 226, 79, 107, 112, 166, 103, 241,
			24, 223, 239, 120, 198, 58, 60, 82, 128, 3, 184, 66, 143, 224,
			145, 224, 81, 206, 163, 45, 63, 90, 168, 114, 59, 33, 159, 95,
			28, 139, 123, 98, 125, 196, 15, 70, 194, 253, 54, 14, 109, 226,
			71, 17, 161, 93, 186, 87, 244, 138, 20, 52, 123, 251, 26, 36,
			17, 46, 52, 231, 232, 76, 31, 221, 84, 37, 216, 165, 212, 106,
			197, 242, 98, 43, 39, 175, 254, 145, 190, 84, 118, 222, 187, 136,
			120, 163, 236, 249
		};

		public int CurrentRandomTableIndex { get; private set; } = 0;
		public int RandomNumber => randomTable[CurrentRandomTableIndex];
		public Action<int> OnRandomTableIndexChanged;

		private void ProgressIndex()
		{
			CurrentRandomTableIndex++;
			if (CurrentRandomTableIndex >= randomTable.Length - 1) CurrentRandomTableIndex = 0;
			OnRandomTableIndexChanged?.Invoke(CurrentRandomTableIndex);
		}

		/// <summary>
		/// Quick way to randomly decide if something is true or false. Perfect for deterministic behavior as it relies on the
		/// random table and its current used index.
		/// </summary>
		public static bool Prob()
		{
			if (Instance == null)
			{
				throw new NotImplementedException("Random13 was never created and stored anywhere.");
			}
			Instance.ProgressIndex();
			if (Instance.RandomNumber % 2 == 0) return true;
			return false;
		}

		/// <summary>
		/// Uses a given time to determine the probability of having a 50/50 chance for something to happen.
		/// Extremely fast and much more predictable.
		/// </summary>
		/// <param name="time">Usually DateTime.Now but a specific time can be given to replay expected events.</param>
		/// <returns>50/50 chance for true or false based on current time given.</returns>
		public static bool ProbFromTime(DateTime time)
		{
			if (time.Ticks % 2 == 0) return true;
			return false;
		}
	}
}