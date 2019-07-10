using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using System;


namespace MediaPlayer.Test
{
	public partial class TestWindow : Window
	{
		public TestWindow()
		{
			InitializeComponent();
			Dispatcher.Invoke(() => txt.Text += $"Constructor. Thread= {Thread.CurrentThread.ManagedThreadId}, Context= {SynchronizationContext.Current?.GetHashCode()}\n");
			A();
		}


		private async void A()
		{
			Dispatcher.Invoke(() => txt.Text += $"Start A. Thread= {Thread.CurrentThread.ManagedThreadId}, Context= {SynchronizationContext.Current?.GetHashCode()}\n");
			await B();
			Dispatcher.Invoke(() => txt.Text += $"End A. Thread= {Thread.CurrentThread.ManagedThreadId}, Context= {SynchronizationContext.Current?.GetHashCode()}\n");
		}


		private async Task B()
		{
			Dispatcher.Invoke(() => txt.Text += $"Start B. Thread= {Thread.CurrentThread.ManagedThreadId}, Context= {SynchronizationContext.Current?.GetHashCode()}\n");
			await Task.Delay(5000);
			Dispatcher.Invoke(() => txt.Text += $"End B. Thread= {Thread.CurrentThread.ManagedThreadId}, Context= {SynchronizationContext.Current?.GetHashCode()}\n");
		}
	}
}
