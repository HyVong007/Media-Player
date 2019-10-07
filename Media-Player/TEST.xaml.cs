using System.Windows;


namespace MediaPlayer
{
	public partial class TEST : Window
	{
		public TEST()
		{
			InitializeComponent();
		}


		private void Button_Read(object sender, RoutedEventArgs e)
		{
			bool b = Application.Current.Read<bool>("a");

		}


		private void Button_Write(object sender, RoutedEventArgs e)
		{
			Application.Current.Write("a", (bool?)true);
		}
	}
}
