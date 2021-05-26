using Mirror;
using System;

namespace Items.Bureaucracy.Internal
{
	public static class PrinterSerializer
	{
		public static void WritePrinter(this NetworkWriter writer, Printer printer)
		{
			writer.Write(printer.TrayCount);
			writer.Write(printer.TrayCapacity);
			writer.WriteBoolean(printer.TrayOpen);
		}

		public static Printer ReadPrinter(this NetworkReader reader)
		{
			int trayCount = reader.Read<int>();
			int trayCapacity = reader.Read<int>();
			bool trayOpen = reader.ReadBoolean();

			return new Printer(trayCount, trayCapacity, trayOpen);
		}
	}
}
