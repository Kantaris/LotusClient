using System;
using System.Windows.Forms;
using vpngui;
namespace WindowsFormsApplication1
{
	internal static class Program
	{
		[System.STAThread]
		private static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Login());
		}
	}
}
