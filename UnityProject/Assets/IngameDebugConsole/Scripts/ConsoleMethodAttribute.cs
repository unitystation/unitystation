using System;
using SecureStuff;

namespace IngameDebugConsole.Scripts
{
	[AttributeUsage( AttributeTargets.Method, AllowMultiple = true )]
	public class ConsoleMethodAttribute : BaseAttribute
	{
		private string m_command;
		private string m_description;

		public string Command { get { return m_command; } }
		public string Description { get { return m_description; } }

		public ConsoleMethodAttribute( string command, string description )
		{
			m_command = command;
			m_description = description;
		}
	} }