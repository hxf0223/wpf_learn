using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using models.flash_partition_model;
using modules.data_upload.data_process;
using modules.mode_and_secure_tool;
using uds_comm;


using HostRunModeEnum = uds_comm.UDSDIDQueryBCUBasicState.HostRunModeEnum;

namespace erase_extern_flash.erase_business
{
	public class eraseBusiness : IDisposable
	{

		#region readErrorEnum

		public enum readErrorEnum
		{
			[Description( "完成" )]
			ok = 0,
			[Description( "等待进入数据模式超时" )]
			wait_data_mode_timeout = -1,
			[Description( "复位到正常模式超时" )]
			wait_nomal_mode_timeout = -2,
			[Description( "进入会话模式失败" )]
			enter_session_fail = -3,
			[Description( "搜索有效历史数据范围失败" )]
			get_history_fail_1 = -4,
			[Description( "搜索有效历史数据范围失败(2)" )]
			get_history_fail_2 = -5,
			[Description( "搜索范围内没有数据" )]
			user_range_invalid = -6,
			[Description( "读取数据失败" )]
			read_data_fail = -7,
			[Description( "擦除Flash失败" )]
			erase_flash_fail = -8,
			[Description( "存储故障" )]
			flash_exception = -9
		}

		#endregion

		private bool _disposed;
		private readonly UDSComm _uds_comm;

		public eraseBusiness() {
			_uds_comm = UDSComm.getInstance();

		}

		~eraseBusiness() {
			Dispose(false);
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose( bool disposing ) {
			if ( _disposed ) return;
			if ( disposing ) {
				_uds_comm.Stop();
			}

			_disposed = true;
		}

		#region 后台任务：读取分区信息

		private BackgroundWorker _read_extern_flash_info_worker;

		private bool is_read_extern_flash_bw_running() {
			try {
				return (null != _read_extern_flash_info_worker && _read_extern_flash_info_worker.IsBusy);
			} catch (Exception e) {
				Debug.WriteLine(e);
			}
			return false;
		}

		public void startReadExternFlashInfo( RunWorkerCompletedEventHandler workerCallback ) {
			if (is_read_extern_flash_bw_running() ) return;
			_read_extern_flash_info_worker = new BackgroundWorker { WorkerSupportsCancellation = true };
			_read_extern_flash_info_worker.DoWork += bgworker_read_extern_flash_info;
			_read_extern_flash_info_worker.RunWorkerCompleted += workerCallback;
			var bw_params = new object[] {_uds_comm};
			_read_extern_flash_info_worker.RunWorkerAsync( bw_params );
		}

		public void stopReadExternFlashInfo() {
			if (!is_read_extern_flash_bw_running() ) return;
			_read_extern_flash_info_worker.CancelAsync();
			while (is_read_extern_flash_bw_running() ) {
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
					new ThreadStart(delegate { }));
			}

			_read_extern_flash_info_worker = null;
		}

		private void bgworker_read_extern_flash_info(object sender, DoWorkEventArgs e) {
			var bw = sender as BackgroundWorker;
			var bw_params = e.Argument as object[];
			var udscomm = bw_params[0] as UDSComm;

			var ierror = readErrorEnum.ok;
			var mode = enter_and_wait_data_mode(bw, udscomm, 2500, true);
			if (mode != HostRunModeEnum.data_mode) goto end_of_bw;

			Thread.Sleep(3000);

			var fsid = new FlashSortIndex(0, 0, 0);
			var data_process = new dataUploadDataProcess(udscomm, bw);
			var data = data_process.readSectionData(fsid);
			Debug.WriteLine("flash infomation data {0}", data);

			data_process.Dispose();

			end_of_bw:

			if ( bw.CancellationPending )
				e.Cancel = true;
		}

		#endregion


		#region 后台任务：擦除片外flash

		private BackgroundWorker _erase_extern_flash_worker;

		private bool is_erase_extern_flash_bw_running() {
			try {
				return (null != _erase_extern_flash_worker && _erase_extern_flash_worker.IsBusy);
			} catch ( Exception e ) { Debug.WriteLine( e ); }
			return false;
		}

		public void startEraseExternFlash( RunWorkerCompletedEventHandler workerCallback ) {
			if ( is_erase_extern_flash_bw_running() ) return;
			_erase_extern_flash_worker = new BackgroundWorker { WorkerSupportsCancellation = true };
			_erase_extern_flash_worker.DoWork += bgworker_erase_extern_flash;
			_erase_extern_flash_worker.RunWorkerCompleted += workerCallback;
			var bw_params = new object[] { _uds_comm };
			_erase_extern_flash_worker.RunWorkerAsync( bw_params );
		}

		public void stopEraseExternFlash() {
			if ( !is_erase_extern_flash_bw_running() ) return;
			_erase_extern_flash_worker.CancelAsync();
			while ( is_erase_extern_flash_bw_running() ) {
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
					new ThreadStart(delegate { }));
			}

			_erase_extern_flash_worker = null;
		}

		private void bgworker_erase_extern_flash( object sender, DoWorkEventArgs e ) {
			var bw = sender as BackgroundWorker;
			var bw_params = e.Argument as object[];
			var udscomm = bw_params[0] as UDSComm;

			var ierror = readErrorEnum.ok;

			var mode = enter_and_wait_data_mode(bw, udscomm, 2500, true);
			if ( mode != HostRunModeEnum.data_mode ) {
				ierror = readErrorEnum.wait_data_mode_timeout;
				Debug.WriteLine( "bgworker_erase_extern_flash fail: enter_and_wait_data_mode" );
				goto end_of_bw;
			}

			Thread.Sleep( 5000 );		// 开机3秒之后才能擦除
			var erase_cmd = new UDSRoutingServiceExternFlashErase { TimeOutMs = 1200 };
			udscomm.Transmit(erase_cmd);
			Debug.WriteLine("erase flash, rx data: " + erase_cmd.RxDataString);

			if ( false == erase_cmd.isPositiveResponse ) {
				ierror = readErrorEnum.erase_flash_fail;
				Debug.WriteLine( "bgworker_erase_extern_flash fail: UDSRoutingServiceExternFlashErase" );
				goto end_of_bw;
			}

			Thread.Sleep(800);
			wait_erase_finish(bw, udscomm, 2000);

			Thread.Sleep(800);
			mode = enter_and_wait_data_mode(bw, udscomm, 2500, false);
			Debug.WriteLine("bgworker_erase_extern_flash end: mode {0}", mode);

			if ( mode != HostRunModeEnum.data_mode ) {
				//ierror = readErrorEnum.wait_data_mode_timeout;		// do not set error here
				//Debug.WriteLine( "bgworker_erase_extern_flash fail: enter_and_wait_data_mode 2" );
			}

			end_of_bw:

			e.Result = new object[] {ierror, mode};
			if ( bw.CancellationPending )
				e.Cancel = true;

			//_erase_extern_flash_worker = null;
		}

		private HostRunModeEnum enter_and_wait_data_mode( BackgroundWorker bw, UDSComm udscomm, int waitms, bool enterDataMode ) {
			if ( enterDataMode ) udscomm.setDongleModeData();
			else udscomm.setDongleModeNormal();

			if ( enterDataMode ) {
				var dreset = new UDSDIDDataModeResetBCU();
				udscomm.Transmit( dreset );
				if ( dreset.isNegativeResponse ) {
					if ( dreset.getNrcCode() == UDSDIDResetBCUBase._nrc_not_support ) {
						var resetcmd = new UDSDIDResetBCU();
						udscomm.Transmit( resetcmd );
					}
				}
			}
			else {
				var resetcmd = new UDSDIDResetBCU();
				udscomm.Transmit( resetcmd );
			}


			Thread.Sleep( 1500 );
			var destmode = enterDataMode ? HostRunModeEnum.data_mode : HostRunModeEnum.nomal;
			return wait_host_dest_mode( udscomm, bw, waitms, destmode );
		}

		private static bool wait_erase_finish( BackgroundWorker bw, UDSComm udscomm, int waitms ) {
			var sw = Stopwatch.StartNew();
			while ( !bw.CancellationPending && sw.ElapsedMilliseconds < waitms ) {
				var cmd = new UDSRoutingServiceExternFlashErase_Query();
				udscomm.Transmit( cmd );
				Debug.WriteLine(string.Format("UDSRoutingServiceExternFlashErase_Query: {0}", cmd.RxDataString));
				Thread.Sleep(200);
			}

			return true;
		}

		#endregion

		#region 等待BCU进入数据模式  wait_host_dest_mode

		protected HostRunModeEnum wait_host_dest_mode( UDSComm udsComm, BackgroundWorker bw, int waitTimeMs, HostRunModeEnum destMode = HostRunModeEnum.data_mode ) {
			var mode = HostRunModeEnum.not_availble;
			var sw = Stopwatch.StartNew();

			while ( sw.ElapsedMilliseconds < waitTimeMs && !bw.CancellationPending ) {
				var bsuccess = false;
				mode = modeSecureTool.uds_query_bcu_run_mode( udsComm, out bsuccess );
				if ( mode == destMode && bsuccess ) break;
				Thread.Sleep( 200 );
			}

			sw.Stop();
			return mode;
		}

		#endregion

	}
}
