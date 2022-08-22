using Mirror;
using System;

namespace Items.Bureaucracy.Internal
{
	public static class ScannerSerializer
	{
		public static void WriteScanner(this NetworkWriter writer, Scanner scanner)
		{
			writer.WriteBool(scanner.ScannerOpen);
			writer.WriteBool(scanner.ScannerEmpty);
			writer.WriteString(scanner.DocumentText);
			writer.WriteString(scanner.ScannedText);
		}

		public static Scanner ReadScanner(this NetworkReader reader)
		{
			bool scannerOpen = reader.ReadBool();
			bool scannerEmpty = reader.ReadBool();
			string documentText = reader.ReadString();
			string scannedText = reader.ReadString();
			return new Scanner(scannerOpen, scannerEmpty, documentText, scannedText);
		}
	}
}
