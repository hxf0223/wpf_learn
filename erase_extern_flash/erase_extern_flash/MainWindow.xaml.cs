using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using erase_extern_flash.erase_business;
using models.flash_partition_model;
using overlapped_namepipe.duplex;
using support.enumextension;
using SupportLibs;
using uds_comm;
using uds_comm.interop;

namespace erase_extern_flash
{
	/// <summary>
	/// MainWindow.xaml 的交互逻辑
	/// </summary>
	public partial class MainWindow : Window
	{
		private UDSComm _uds_comm;
		private updateDevInfomation _update_dev_info;
		private vmDevInfomation _vm_dev_info;
		private eraseBusiness _erase_process;

		private overlappedNamedPipeDuplexAppMutex _ov_named_pipe_duplex_app_mutex;


		public MainWindow() {
			Debug.WriteLine("on main window");
			InitializeComponent();
			TraceRedirect.AddTraceListener();
			
			this.MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
			this.Loaded +=MainWindow_Loaded;
			this.Closed += onClosed;
		}

		private void onClosed(object sender, EventArgs eventArgs) {
			_update_dev_info.stopThread();
			_uds_comm.Stop();
		}

		private void on_app_mutex_server_handler(object sender, EventArgs e) {
			var args = e as overlappedNamedPipeDuplexAppMutex.serverResponseCbEventArgs;
			Debug.WriteLine("on_app_mutex_server_handler: " + args);
		}

		private void MainWindow_Loaded( object sender, RoutedEventArgs e ) {
			string appname = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
			_ov_named_pipe_duplex_app_mutex = new overlappedNamedPipeDuplexAppMutex(appname, on_app_mutex_server_handler, null);
			_ov_named_pipe_duplex_app_mutex.startServer();

			_uds_comm = UDSComm.getInstance();
			_update_dev_info = new updateDevInfomation();
			_vm_dev_info = new vmDevInfomation(this);
			this.DataContext = _vm_dev_info;

			_erase_process = new eraseBusiness();

			var flash_model = flashPartitionModel.getInstance();
			string path = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, @"net_libs\uds_config.json");
			var uds_config = UDSWrapper.Can_piParam_Type_Json.readFromJsonFile(path);

			if (false == _uds_comm.Start(uds_config, null)) {
				MessageBox.Show(this, "打开CAN失败");
				this.Close();
			}
			else {
				// 需要更新界面的功能代码，最好等待所有的元素完成再调用（因为此时并不能确定是否所有的元素都render完成）
				// http://geekswithblogs.net/ilich/archive/2012/10/16/running-code-when-windows-rendering-is-completed.aspx
				// https://blog.magnusmontin.net/2013/04/30/implement-a-mvvm-loading-dialog-in-wpf/
				Dispatcher.BeginInvoke(new Action<RoutedEventArgs>(on_load_finished), DispatcherPriority.ContextIdle, e);
			}

		}

		private void on_load_finished( RoutedEventArgs e ) {
			_update_dev_info.addDevInfoUpdateEventHandler(_vm_dev_info.onDevInfoUpdateEventHandler);
			_update_dev_info.startThread();

			_erase_process.startReadExternFlashInfo(read_info_event_handler);
		}


		private void MainWindow_MouseLeftButtonDown( object sender, MouseButtonEventArgs e ) {
			if (!(e.OriginalSource is Grid) && !(e.OriginalSource is Border) && !(e.OriginalSource is Window)) return;
			var win = new WindowInteropHelper( this );
			win32Interop.SendMessage(win.Handle, win32Interop.WM_NCLBUTTONDOWN, (int) win32Interop.HitTest.HTCAPTION, 0);
		}

		private void btnX_Click( object sender, RoutedEventArgs e ) { this.Close(); }
		private void btnMin_Click( object sender, RoutedEventArgs e ) { this.WindowState = WindowState.Minimized; }
		private void btnCancel_Click( object sender, RoutedEventArgs e ) { this.Close(); }

		private void btnErase_Click(object sender, RoutedEventArgs e) {
			btnCancel.IsEnabled = btnErase.IsEnabled = false;
			btnClose.IsEnabled = btnMin.IsEnabled = false;
			_erase_process.startEraseExternFlash(erase_finished_event_handler);
		}

		private void erase_finished_event_handler(object sender, RunWorkerCompletedEventArgs e) {
			if (false == e.Cancelled && null == e.Error) {
				var result = e.Result as object[];
				var ierror = (eraseBusiness.readErrorEnum) result[0];
				var mode = (UDSDIDQueryBCUBasicState.HostRunModeEnum) result[1];

				MessageBox.Show(ierror != eraseBusiness.readErrorEnum.ok ? ierror.Description() : "擦除完成");
			}
			else {
				// TODO: cancel process
			}

			btnClose.IsEnabled = btnMin.IsEnabled = true;
			btnErase.IsEnabled = btnCancel.IsEnabled = true;
		}

		private void read_info_event_handler(object sender, RunWorkerCompletedEventArgs e) {
			if (false == e.Cancelled && null == e.Error) {

			}
			else {
				// TODO: cancel proces
			}
		}

	}

}
