using System;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using Finisar.SQLite;

namespace SQLLiteDemo
{
	// Call this to add to log. Make sure to change foobar to whatever you want. 
	public class Form1 : Form
	{

		public Form1()
		{
			m_dbConnection =
			new SQLiteConnection("Data Source=logs.db;Version=3;");
			m_dbConnection.Open();
			string sql = "insert into logs (player_name, SteamID, IP, HD, SD, ND) values ( foobar )";
			command = new SQLiteCommand(sql, m_dbConnection);
			command.ExecuteNonQuery();
			
		}
	}
}
