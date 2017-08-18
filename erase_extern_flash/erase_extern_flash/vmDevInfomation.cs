using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using uds_comm;

namespace erase_extern_flash
{

	public static class parseVerFromInfo
	{
		private static readonly Regex _pattern = new Regex(@"[TR][0-9]+\.[0-9]+(?:\.[0-9]+)*");
		public static string getVersionFromSystemInfoString( string info ) {
			string result = _pattern.Match( info ).Value;
			return result.Length <= 0 ? result : result.Substring(1);
		}
	}

	public class vmDevInfomation : INotifyPropertyChanged
	{
		private readonly Window _host_wnd;

		private string _dev_model;
		private string _hwid, _fwid;
		private string _hw_version, _sw_version;

		public vmDevInfomation(Window hostWnd) {
			_host_wnd = hostWnd;
		}

		#region 绑定属性实现

		public string devModel {
			get { return _dev_model; }
			set {
				if ( _dev_model == value ) return;
				_dev_model = value;
				notify( "devMode" );
			}
		}

		public string hwId {
			get { return _hwid; }
			set {
				if ( value == _hwid) return;
				_hwid = value;
				notify("hwId");
			}
		}

		public string fwId {
			get { return _fwid; }
			set
			{
				if (value == _fwid) return;
				_fwid = value;
				notify("fwId");
			}
		}

		public string hwVersion {
			get { return _hw_version; }
			set
			{
				if ( value == _hw_version) return;
				_hw_version = value;
				notify("hwVersion");
			}
		}

		public string swVersion {
			get { return _sw_version; }
			set
			{
				if ( value == _sw_version)return;
				_sw_version = value;
				notify("swVersion");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void notify( string propertyName ) {
			if ( PropertyChanged != null ) {
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
		
		#endregion

		public void onDevInfoUpdateEventHandler(object sender, EventArgs e) {
			_host_wnd.Dispatcher.Invoke((Action)(()=> {
				var args = e as updateDevInfomation.devInfoUpdateEventArgs;
				var bcu_info = args._bcu_info;

				devModel = bcu_info._dev_model;
				hwId = bcu_info._hwid;
				fwId = bcu_info._fwid;

				hwVersion = bcu_info._hw_version;
				swVersion = parseVerFromInfo.getVersionFromSystemInfoString(bcu_info._sw_version);
			}));
		}

	}

	#region 列表项数据结构定义  devInfoData

	public class devInfoData
	{
		public string _dev_model;
		public string _dev_status;

		public string _hwid;
		public string _fwid;

		public string _hw_version;
		public string _sw_version;

		public static devInfoData na_value() {
			var data = new devInfoData {
				_dev_model = udsString.InstanceNA().Value,
				_dev_status = udsString.InstanceNA().Value,
				_hwid = udsByteArray.InstanceNA().ValueString,
				_fwid = udsString.InstanceNA().ValueString,
				_hw_version = udsString.InstanceNA().ValueString,
				_sw_version = udsString.InstanceNA().ValueString,
			};

			return data;
		}

	}

	#endregion

	public sealed class updateDevInfomation : IDisposable
	{
		private bool _disposed;
		private readonly UDSComm _uds_comm;

		#region 构造及析构

		public updateDevInfomation() {
			_uds_comm = UDSComm.getInstance();
		}

		~updateDevInfomation() {
			Dispose(false);
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose( bool disposing ) {
			if ( !_disposed ) {
				if (disposing) {
					
				}

				stop_read_status_thread();
				remove_all_devinfo_update_event_handler();
				_disposed = true;
			}
		}

		#endregion

		public void startThread() {
			start_read_status_thread();
		}

		public void stopThread() {
			stop_read_status_thread();
		}

		#region 后台线程

		private Thread _read_status_thread;
		private volatile bool _read_status_thread_running;

		private void start_read_status_thread() {
			if ( null != _read_status_thread )
				return;

			_read_status_thread_running = true;
			_read_status_thread = new Thread( read_status_thread ) { IsBackground = true };
			_read_status_thread.Start();
		}

		private void stop_read_status_thread() {
			if ( null == _read_status_thread ) return;
			_read_status_thread_running = false;
			_read_status_thread.Join();
			_read_status_thread = null;
		}

		private void read_status_thread() {

			while ( _read_status_thread_running ) {
				var bcu_data = get_bcu_data();
				var args = new devInfoUpdateEventArgs { _bcu_info = bcu_data.Copy() };
				devinfo_update_notify( args );

				var sw = Stopwatch.StartNew();
				while ( sw.ElapsedMilliseconds < 600 && _read_status_thread_running )
					Thread.Sleep( 50 );

			}		// end of while

		}

		#region 读取BCU信息  get_bcu_data

		private devInfoData get_bcu_data() {
			var data = devInfoData.na_value();

			/*var cmd_devid_list = new UDSDIDSystemDevIDList();
			_uds_comm.Transmit( cmd_devid_list );
			if ( cmd_devid_list.isPositiveResponse ) {
				int bcu_model_id = cmd_devid_list.getBCUModelID();
				if ( null != _host_profile_id_name_pair_list ) {
					foreach ( var pair in _host_profile_id_name_pair_list ) {
						if ( pair.Key != bcu_model_id ) continue;
						data._dev_model = pair.Value;
						data._dev_status = "就绪";
						break;
					}
				}
			}*/

			if ( _read_status_thread_running ) {
				var cmd_bcu_fwid = new UDSDIDBCUFWIDString() { TimeOutMs = 100 };
				_uds_comm.TransmitIgnoreHeartFailFlag( cmd_bcu_fwid );
				var fwid = cmd_bcu_fwid.getFwidString();
				data._fwid = fwid.ValueString;
			}

			if ( _read_status_thread_running ) {
				var cmd_bcu_hwid = new UDSDIDBCUHWIDString() { TimeOutMs = 100 };
				_uds_comm.TransmitIgnoreHeartFailFlag( cmd_bcu_hwid );
				var hwid = cmd_bcu_hwid.getHwidString();
				data._hwid = hwid.ValueString;
			}

			if ( _read_status_thread_running ) {
				var cmd_bcu_hwver = new UDSDIDBCUHWVersionString() { TimeOutMs = 100 };
				_uds_comm.TransmitIgnoreHeartFailFlag( cmd_bcu_hwver );
				var hwver = cmd_bcu_hwver.getHWVersionString();
				data._hw_version = hwver.isDataNA ? hwver.ValueString : "1.01";
			}

			if ( _read_status_thread_running ) {
				var cmd_bcu_swver = new UDSDIDBCUReleaseInfoString() { TimeOutMs = 100 };
				_uds_comm.TransmitIgnoreHeartFailFlag( cmd_bcu_swver );
				var swver = cmd_bcu_swver.getReaseInfoString();
				data._sw_version = swver.ValueString;
			}

			return data;

		}

		#endregion

		#endregion

		#region 事件 onDevInfoUpdateEvent

		public class devInfoUpdateEventArgs : EventArgs
		{
			public devInfoData _bcu_info;
		}

		private event EventHandler onDevInfoUpdateEvent;

		public void addDevInfoUpdateEventHandler(EventHandler handler) {
			onDevInfoUpdateEvent += handler;
		}

		private void devinfo_update_notify( EventArgs args ) {
			if ( null == onDevInfoUpdateEvent ) return;
			var receivers = onDevInfoUpdateEvent.GetInvocationList();
			foreach ( var @delegate in receivers ) {
				var receiver = (EventHandler)@delegate;
				receiver.BeginInvoke( this, args, null, null );
				//receiver.Invoke( this , args );
			}
		}

		private void remove_all_devinfo_update_event_handler() {
			if ( null == onDevInfoUpdateEvent ) return;
			var dary = onDevInfoUpdateEvent.GetInvocationList();
			if ( dary.Length <= 0 ) return;
			foreach ( var del in dary ) {
				onDevInfoUpdateEvent -= del as EventHandler;
			}
		}

		#endregion

	}

}
