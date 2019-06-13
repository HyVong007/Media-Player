using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Speech.Recognition;
using System.Speech.Synthesis;


namespace MediaPlayer
{
	public partial class TestWindow : Window
	{

		private SpeechRecognitionEngine listener;

		public TestWindow()
		{
			InitializeComponent();
			listener = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US", true));
			listener.SetInputToDefaultAudioDevice();
			listener.SpeechRecognized += Listener_SpeechRecognized;
			var choices = new Choices("ai ra xu hue", "anh con no em", "ai dua em ve", "ao dep nang dau",
				"ba thang ta tu", "ai kho vi ai", "can nha mau tim", "cho vua long em", "chuyen ba mua mua",
				"chuyen tau hoang hon", "da co hoai lang", "dem trao ki niem", "doi thong hai mo");

			var builder = new GrammarBuilder();
			builder.Append(choices);
			listener.LoadGrammar(new Grammar(builder));

		}


		private void Listener_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
		{
			txt.Text = e.Result.Text;
		}


		private void Button_Click(object sender, RoutedEventArgs e)
		{
			listener.RecognizeAsync();
		}
	}
}
