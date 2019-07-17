using System.Windows;


namespace MediaPlayer
{
	public partial class PopupWaiting : Window
	{
		public static PopupWaiting instance { get; private set; }
		private bool manualClose;



		public PopupWaiting()
		{
			if (instance == null) instance = this; else throw new System.Exception();
			InitializeComponent();
		}


		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) => e.Cancel = !manualClose;


		public new void Close()
		{
			instance = null;
			manualClose = true;
			base.Close();
		}
	}
}
