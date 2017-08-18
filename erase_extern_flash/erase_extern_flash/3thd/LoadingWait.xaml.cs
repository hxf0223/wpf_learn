using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace erase_extern_flash
{
	/// <summary>
	/// Interaction logic for LoadingWait.xaml
	/// http://blog.csdn.net/lhx527099095/article/details/8005095
	/// </summary>
	public partial class loadingWait : UserControl
	{

		private readonly DispatcherTimer _animation_timer;

		
		public loadingWait()
		{
			InitializeComponent();
			_animation_timer = new DispatcherTimer(DispatcherPriority.ContextIdle, Dispatcher) {Interval = new TimeSpan(0, 0, 0, 0, 90)};

			var bind_info = new Binding( "waitingMessage" ) { Source = this };
			this.tbWaiting.SetBinding(TextBlock.TextProperty, bind_info);
		}

		// 声明依赖属性
		// 元素绑定自身 http://blog.csdn.net/lanpst/article/details/19406331
		public static readonly DependencyProperty waitingMessageProperty =
			DependencyProperty.Register( "waitingMessage", typeof( string ), typeof( loadingWait ) );

		//声明中转的变量  
		public string waitingMessage {
			get { return (string)GetValue( waitingMessageProperty ); }
			set { SetValue( waitingMessageProperty, value ); }
		}  



		#region Private Methods

		private void start()
		{
			_animation_timer.Tick += handleAnimationTick;
			_animation_timer.Start();
		}

		private void stop()
		{
			_animation_timer.Stop();
			_animation_timer.Tick -= handleAnimationTick;
		}

		private void handleAnimationTick(object sender, EventArgs e)
		{
			SpinnerRotate.Angle = (SpinnerRotate.Angle + 36) % 360;
		}

		private void handleLoaded(object sender, RoutedEventArgs e)
		{
			const double offset = Math.PI;
			const double step = Math.PI * 2 / 10.0;

			setPosition(C0, offset, 0.0, step);
			setPosition(C1, offset, 1.0, step);
			setPosition(C2, offset, 2.0, step);
			setPosition(C3, offset, 3.0, step);
			setPosition(C4, offset, 4.0, step);
			setPosition(C5, offset, 5.0, step);
			setPosition(C6, offset, 6.0, step);
			setPosition(C7, offset, 7.0, step);
			setPosition(C8, offset, 8.0, step);
		}

		private static void setPosition(Ellipse ellipse, double offset,double posOffSet, double step)
		{
			ellipse.SetValue(Canvas.LeftProperty, 50.0
				+ Math.Sin(offset + posOffSet * step) * 50.0);

			ellipse.SetValue(Canvas.TopProperty, 50
				+ Math.Cos(offset + posOffSet * step) * 50.0);
		}

		private void handleUnloaded(object sender, RoutedEventArgs e)
		{
			stop();
		}

		private void handleVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var is_visible = (bool)e.NewValue;

			if (is_visible)
				start();
			else
				stop();
		}

		#endregion  
	
	}
}
