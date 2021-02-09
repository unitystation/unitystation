using Mirror;
using System;

namespace Assets.Scripts.Items.Bureaucracy.Internal
{
	public static class PrinterSerializer
	{
		public static void WritePrinter(this NetworkWriter writer, Printer printer)
		{
			writer.WritePackedInt32(printer.TrayCount);
			writer.WritePackedInt32(printer.TrayCapacity);
			writer.WriteBoolean(printer.TrayOpen);
		}

		public static Printer ReadPrinter(this NetworkReader reader)
		{
			int trayCount = reader.ReadPackedInt32();
			int trayCapacity = reader.ReadPackedInt32();
			bool trayOpen = reader.ReadBoolean();
			return new Printer(trayCount, trayCapacity, trayOpen);
		}
	}
}