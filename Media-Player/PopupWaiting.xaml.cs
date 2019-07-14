using System.Windows;


namespace MediaPlayer
{
	public partial class PopupWaiting : Window
	{
		public static PopupWaiting instance { get; private set; }
		private bool manualClose;



		static PopupWaiting()
		{
			Database.initializeCompleted += () =>
			 {
				 instance.manualClose = true; instance.Close();
			 };
		}


		public PopupWaiting()
		{
			instance = this;
			InitializeComponent();
		}


		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) => e.Cancel = !manualClose;
	}
}
