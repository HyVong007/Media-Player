using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace MediaPlayer
{
	/// <summary>
	/// Interaction logic for PopupWaiting.xaml
	/// </summary>
	public partial class PopupWaiting : Window
	{
		public static PopupWaiting instance { get; private set; }


		public PopupWaiting()
		{
			if (instance == null) instance = this; else throw new Exception();
			InitializeComponent();
		}


		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			instance = null;
		}
	}
}
