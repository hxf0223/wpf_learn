using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using erase_extern_flash.can_dev_types;
using overlapped_namepipe.duplex;
using uds_comm;
using uds_comm.interop;

namespace erase_extern_flash
{
	/// <summary>
	/// App.xaml 的交互逻辑
	/// </summary>
	public partial class App : Application
	{
		private ManualResetEvent _mre;

		protected override void OnStartup( StartupEventArgs e ) {
			Application.Current.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;

			_mre = new ManualResetEvent( false );
			string appname = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

			var ov_named_pipe_duplex_app_mutex =
				new overlappedNamedPipeDuplexAppMutex( appname, null, on_app_mutex_client_handler );
			ov_named_pipe_duplex_app_mutex.startClient();
			bool bresult = _mre.WaitOne( 200 );

			if ( bresult ) {
				System.Threading.Thread.Sleep( 60 );
				ov_named_pipe_duplex_app_mutex.stopClient();
				ov_named_pipe_duplex_app_mutex.Dispose();

				Shutdown( 0 );
				return;
			}
			else {
				ov_named_pipe_duplex_app_mutex.stopClient();
				ov_named_pipe_duplex_app_mutex.Dispose();

				if ( !try_can_open() ) {
					Shutdown( 0 );
					return;
				}

				base.OnStartup( e );
				Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
				this.StartupUri = new Uri( "MainWindow.xaml", UriKind.Relative );
			}

		}

		private static bool try_can_open() {
			var uds_comm = UDSComm.getInstance();
			string path = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, @"net_libs\uds_config.json");
			var uds_config = UDSWrapper.Can_piParam_Type_Json.readFromJsonFile(path);

			while ( true ) {
				bool bok = uds_comm.Start( uds_config, null );
				if ( bok ) {
					System.Threading.Thread.Sleep(100);
					uds_comm.Stop();
					return true;
				}

				var dlg = new openCan();
				uds_config.can_devtype_list.ForEach(x => dlg.addCanDevType(x.dev, x.type));
				dlg.setSelDevType(string.Empty, uds_config.can_device_type);

				var bdlg = dlg.ShowDialog();
				if ( null == bdlg || false == bdlg )
					break;

				var sel_can_dev = dlg.getSelDevType();
				if ( null != sel_can_dev) {
					uds_config.can_device_type = sel_can_dev.type;
					uds_config.writeToFile(path);
				}
			}

			return false;
		}

		private void on_app_mutex_client_handler( object sender, EventArgs e ) {
			var args = e as overlappedNamedPipeDuplexAppMutex.clientResponseCbEventArgs;
			_mre.Set();
		}
	}

}
