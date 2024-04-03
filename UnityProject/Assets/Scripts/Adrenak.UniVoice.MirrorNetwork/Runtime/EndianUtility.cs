using System;



namespace Adrenak.BRW {

	/// <summary>

	/// The utility used by <see cref="BytesReader"/> and

	/// <see cref="BytesWriter"/> for reading and writing bytes.

	/// </summary>

	public static class EndianUtility {

		/// <summary>

		/// Whether the big or the little Endian is used.

		/// Default is Little Endian

		/// There is little reason to change this.

		/// Don't do so unless you're sure what you're doing.

		/// </summary>

		public static bool UseLittleEndian = true;



		/// <summary>

		/// Whether the current machine requires endian correction

		/// given our <see cref="UseLittleEndian"/> preferrence

		/// </summary>

		public static bool RequiresEndianCorrection {

			get { return UseLittleEndian ^ BitConverter.IsLittleEndian; }

		}



		/// <summary>

		/// Endian correction over a byte array

		/// </summary>

		/// <param name="bytes">The byte array that needs Endian correction</param>

		public static void EndianCorrection(byte[] bytes) {

			if (RequiresEndianCorrection)

				Array.Reverse(bytes);

		}

	}

}