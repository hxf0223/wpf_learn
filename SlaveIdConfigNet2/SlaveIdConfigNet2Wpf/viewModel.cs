using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using slave_uds;
using SlaveIdConfigNet2;
using uds_comm;
using uds_comm.interop;

namespace SlaveIdConfigNet2Wpf.viewModel 
{

	#region viewModelBase

	public class viewModelBase : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		protected void raisePropertyChanged( string propertyName ) {
			if ( propertyName != null && PropertyChanged != null ) {
				PropertyChanged.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
			}
		}
	}

	#endregion

	#region DelegateCommand

	public class delegateCommand : ICommand {
		public Action<object> _execute_command = null;	//A method prototype without return value.
		public Func<object, bool> _can_execute_command = null;	//A method prototype return a bool type.
		public event EventHandler CanExecuteChanged;

		public bool CanExecute( object parameter ) {
			return _can_execute_command == null || this._can_execute_command(parameter);
		}

		public void Execute( object parameter ) {
			if (this._execute_command != null)
				this._execute_command(parameter);
		}
		
		public void raiseCanExecuteChanged() {
			if (CanExecuteChanged != null) {
				CanExecuteChanged(this, EventArgs.Empty);
			}
		}
	}

	public class delegateCommandNoParameter : ICommand {
		public Action _execute_command = null;		// A method prototype without return infoValue, without param.
		public Func<bool> _can_execute_command = null; // A method prototype return a bool type.
		public event EventHandler CanExecuteChanged;

		private bool can_execute() {
			return _can_execute_command == null || this._can_execute_command();
		}

		private void execute() {
			if ( this._execute_command != null )
				this._execute_command();
		}

		bool ICommand.CanExecute( object parameter ) {
			return can_execute();
		}

		void ICommand.Execute( object parameter ) {
			execute();
		}

		public void raiseCanExecuteChanged() {
			if (CanExecuteChanged != null) {
				CanExecuteChanged( this, EventArgs.Empty );
			}
		}
	}

	#endregion

	public class bmuInfomation : viewModelBase {

		#region infoKv

		public class infoKv : viewModelBase
		{
			private string _key, _info_value;

			public string infoKey {
				get {
					return _key;
				}
				set {
					_key = value;
					raisePropertyChanged( "infoKey" );
				}
			}

			public string infoValue {
				get {
					return _info_value;
				}
				set {
					_info_value = value;
					raisePropertyChanged( "infoValue" );
				}
			}
		}

		#endregion

		#region select changed event

		public class selectChangedEventArgs : EventArgs
		{
			private readonly canRxTpIfMap _tp_if_map;
			public canRxTpIfMap tpIfMap {
				get {
					return _tp_if_map;
				}
			}

			public selectChangedEventArgs(canRxTpIfMap map) {
				_tp_if_map = map;
			}
		}

		public event EventHandler<selectChangedEventArgs> onSelectedEvent;
		public void selected_changed_notify(canRxTpIfMap map) {
			if (null == onSelectedEvent)
				return;

			var receivers = onSelectedEvent.GetInvocationList();
			var args = new selectChangedEventArgs(map);
			foreach (var x in receivers) {
				var receiver = x as EventHandler<selectChangedEventArgs>;
				if (null == receiver)
					continue;

				//receiver.BeginInvoke(this, args, null, null);
				receiver.Invoke( this, args );
			}
		}

		#endregion

		// bmu's realtime status
		//  DataGrid: http://www.cnblogs.com/sbgh/p/6841285.html
		private ObservableCollection<infoKv> _bcu_stt_list;
		public ObservableCollection<infoKv> bcuSttList {
			get { return _bcu_stt_list; }
			set {
				_bcu_stt_list = value;
				raisePropertyChanged("bcuSttList");
			}
		}

		#region 用户接口属性

		private string get_stt(string key) {
			foreach (var x in _bcu_stt_list) {
				if (x.infoKey == key) return x.infoValue;
			}

			return string.Empty;
		}

		private void set_stt( string key, string v ) {
			foreach (var x in _bcu_stt_list) {
				if (x.infoKey != key) continue;	
				x.infoValue = v;
			}
		}

		private bool kv_info_key_exist(string key) {
			var temp = _bcu_stt_list.FirstOrDefault(x => x.infoKey == key);
			return (null != temp) ? true : false;
		}

		private infoKv create_kv_info(string key, string defValue) {
			var temp = new infoKv {infoKey = key, infoValue = defValue};
			return temp;
		}

		private const string keyHwid = "HWID";
		public string hwid {
			get { return get_stt(keyHwid); }
			set {
				if (!kv_info_key_exist(keyHwid)) {
					var temp = create_kv_info(keyHwid, "N/A");
					_bcu_stt_list.Add(temp);
				}
				set_stt(keyHwid, value );
			}
		}

		private const string keySwVer = "软件版本号";
		public string swver {
			get { return get_stt(keySwVer); }
			set {
				if ( !kv_info_key_exist(keySwVer) ) {
					var temp = create_kv_info(keySwVer, "N/A" );
					_bcu_stt_list.Add( temp );
				}
				set_stt(keySwVer, value );
			}
		}

		private string keyAin = "模拟输入";
		public string ain {
			get { return get_stt(keyAin); }
			set {
				if ( !kv_info_key_exist(keyAin) ) {
					var temp = create_kv_info(keyAin, "N/A");
					_bcu_stt_list.Add(temp);
				}
				set_stt(keyAin, value);
			}
		}

		private string keyDin = "数字输入";
		public string din {
			get { return get_stt(keyDin); }
			set {
				if ( !kv_info_key_exist(keyDin) ) {
					var temp = create_kv_info(keyDin, "N/A" );
					_bcu_stt_list.Add( temp );
				}
				set_stt( keyDin, value );
			}
		}

		#endregion

		private ObservableCollection<canRxTpIfMap> _rx_tp_if_map_list;
		public ObservableCollection<canRxTpIfMap> rxTpIfMapList {
			get { return _rx_tp_if_map_list; }
			set {
				if (null == value)
					return;
				_rx_tp_if_map_list = value;
				raisePropertyChanged( "rxTpIfMapList" );
			}
		}

		private canRxTpIfMap _selected_tp_if_map;
		public canRxTpIfMap selectedTpIfMap {
			get {
				return _selected_tp_if_map;
			}
			set {
				if (_selected_tp_if_map == value)
					return;

				_selected_tp_if_map = value;
				raisePropertyChanged( "selectedTpIfMap" );
				selected_changed_notify(_selected_tp_if_map);

				if ( null != _selected_tp_if_map ) {
					var str = string.Format( "bmuInfomation {0}, selected canId changed: 0x{1:x}",
						_priority, _selected_tp_if_map.canId );
					Debug.WriteLine(str);
				}
			}
		}

		public canRxTpIfMap usedTpIfMap { get; set; }

		private readonly int _priority;
		public bmuInfomation(ObservableCollection<canRxTpIfMap> mapList, int priority ) {
			_bcu_stt_list = new ObservableCollection<infoKv>();
			hwid = swver = ain = din = "N/A";
			rxTpIfMapList = mapList;
			_priority = priority;
		}
	}
	
	public class bmuCollectionViewModel : viewModelBase
	{
		private ObservableCollection<canRxTpIfMap> _rx_tp_if_map_list;
		private ObservableCollection<bmuInfomation> _bmu_info_list;
		private readonly UDSComm _uds_comm;

		public delegateCommandNoParameter broadcastCommand { get; set; }
		public delegateCommandNoParameter setAllBmuIdCommand { get; set; }

		public ObservableCollection<bmuInfomation> bmuList {
			get {
				return _bmu_info_list;
			}
			set {
				if (null != value) {
					_bmu_info_list = value;
					/*foreach (var x in _bmu_info_list) {
						x.onSelectedEvent += on_bmu_selected_id_changed;
					}*/
				}
			}
		}

		public ObservableCollection<canRxTpIfMap> rxTpIfMapList {
			get {
				return _rx_tp_if_map_list;
			}
			set {
				if (null == value)
					return;
				_rx_tp_if_map_list = value;
				foreach ( var x in _bmu_info_list ) {
					x.rxTpIfMapList = _rx_tp_if_map_list;
				}
			}
		}

		private string _run_error_info;
		public string runErrorInfomation {
			get {
				return _run_error_info;
			}
			set {
				_run_error_info = value;
				raisePropertyChanged( "runErrorInfomation" );
			}
		}

		public enum runStateEnum { idle, bc_before_canid_first_alloc, canid_first_alloc, get_bmu_info, set_all_bmu_id }
		private runStateEnum _run_state = runStateEnum.idle;

		public bmuCollectionViewModel( ObservableCollection<canRxTpIfMap> tpIfMapList) {
			_rx_tp_if_map_list = new ObservableCollection<canRxTpIfMap>();
			_bmu_info_list = new ObservableCollection<bmuInfomation>();
			_uds_comm = UDSComm.getInstance();

			broadcastCommand = new delegateCommandNoParameter {
				_execute_command = new Action(broadcast_get_canid_async),
				_can_execute_command = new Func<bool>(is_can_run_broadcast_task)
			};

			setAllBmuIdCommand = new delegateCommandNoParameter {
				_execute_command = new Action(set_all_bmu_id_async),
				_can_execute_command = new Func<bool>(is_can_run_bmu_user_sel_id_task)
			};
		}

		private void on_bmu_selected_id_changed(object sender, bmuInfomation.selectChangedEventArgs args) {
			setAllBmuIdCommand.raiseCanExecuteChanged();
		}

		private bool is_can_run_broadcast_task() {
			if (_run_state == runStateEnum.idle)
				return true;
			return false;
		}

		private bool is_can_run_bmu_user_sel_id_task() {
			if (null == bmuList || bmuList.Count == 0)
				return false;
			if (_run_state != runStateEnum.idle)
				return false;

			var bmu_tp_if_map_list = (from x in bmuList where null != x.selectedTpIfMap select x.selectedTpIfMap).ToList();
			return bmu_tp_if_map_list.Select(x => bmu_tp_if_map_list.FindAll(y => y.canId == x.canId))
				.All(duplicated_list => duplicated_list.Count == 1);
		}

        #region test bw

		private BackgroundWorker _test_bw_worker;

		private void test_bw_finished_event_handler( object sender, RunWorkerCompletedEventArgs e ) {
			broadcastCommand.raiseCanExecuteChanged();
			this.bmuList.Add( new bmuInfomation( rxTpIfMapList, bmuList.Count ) );
		}

		private void start_test_bw() {
			_test_bw_worker = new BackgroundWorker { WorkerSupportsCancellation = true };
			_test_bw_worker.DoWork += bgworker_test;
			_test_bw_worker.RunWorkerCompleted += test_bw_finished_event_handler;
			_test_bw_worker.RunWorkerAsync();
		}

		private void bgworker_test( object sender, DoWorkEventArgs e ) {
			var bw = sender as BackgroundWorker;
			Thread.Sleep( 300 );

			if ( bw.CancellationPending )
				e.Cancel = true;

			_test_bw_worker = null;
		}

        #endregion

        #region 广播任务

        private BackgroundWorker _bw_broadcast_get_canid;

		private bool is_broadcast_get_canid_bw_running() {
			try {
				return (null != _bw_broadcast_get_canid && _bw_broadcast_get_canid.IsBusy);
			} catch ( Exception e ) {
				Debug.WriteLine(e);
			}
			return false;
		}

		private void broadcast_get_canid_finished_event_handler(object sender, RunWorkerCompletedEventArgs e) {
			runErrorInfomation = string.Empty;

			if ( e.Cancelled || null != e.Error ) {
				_run_state = runStateEnum.idle;
				broadcastCommand.raiseCanExecuteChanged();
				setAllBmuIdCommand.raiseCanExecuteChanged();
				return;
			}

			var objs = e.Result as object[];
			var bc_result = (slaveIdConfigBroadcastStatePattern.stateEnum) objs[0];
			var uds_config = objs[1] as UDSWrapper.Can_piParam_Type_Json;
			var slave_uds_process = objs[2] as slaveUdsDataProcess;

			var if_canid_list = uds_config.canif_piParam.pCanIf_Rxpdu_piParam_Array;
			var tp_rxpdu_list = uds_config.cantp_piParam.pCanTp_RxNsdu_piParam_Array;

			if (bc_result != slaveIdConfigBroadcastStatePattern.stateEnum.ok) {	// broadcast fail
				bmuList.Clear();
				_run_state = runStateEnum.idle;
				broadcastCommand.raiseCanExecuteChanged();
				setAllBmuIdCommand.raiseCanExecuteChanged();
				runErrorInfomation = Properties.Resources.ResourceManager.GetString( "str_bc_fail" );
				return;
			}

			var resp_rxpduids = slave_uds_process.getSlaveResponseStrs("CanIdV2");
			var resp_rxpduid_crc_list = new List<Tuple<uint, ushort>>();
			foreach (var x in resp_rxpduids) {
				var temp_tp_rxpdu_list = (from y in tp_rxpdu_list select y.CanTpChannelId).ToArray();
				int pos = Array.IndexOf(temp_tp_rxpdu_list, x.Key );
				if (pos >= 0 && pos < if_canid_list.Length) {
					ushort crc = ushort.Parse(x.Value);
					resp_rxpduid_crc_list.Add(new Tuple<uint, ushort>(if_canid_list[pos].Canid, crc));
				}
			}

			slave_uds_process.Dispose();
			start_first_alloc_async(_uds_comm, uds_config, resp_rxpduid_crc_list, null);
		}

		private void broadcast_get_canid_async() {
			_uds_comm.Stop();
			bmuList.Clear();
			_run_state = runStateEnum.bc_before_canid_first_alloc;
			broadcastCommand.raiseCanExecuteChanged();
			setAllBmuIdCommand.raiseCanExecuteChanged();

			var slave_uds_process = new slaveUdsDataProcess();

			var config_path = udsConfigPath;
			var uds_config = create_broadcast_canid_config_all_master();

			bool berr = _uds_comm.Start(uds_config, slave_uds_process.rx_indication_cbk);
			if (false == berr) {
				_run_state = runStateEnum.idle;
				broadcastCommand.raiseCanExecuteChanged();
				setAllBmuIdCommand.raiseCanExecuteChanged();
				runErrorInfomation = Properties.Resources.ResourceManager.GetString( "str_open_can_fail" );
				return;
			}

			start_broadcast_get_canid_bw(broadcast_get_canid_finished_event_handler, _uds_comm, uds_config, slave_uds_process);
		}

		private void start_broadcast_get_canid_bw( RunWorkerCompletedEventHandler workerCallback, UDSComm udsComm,
			UDSWrapper.Can_piParam_Type_Json udsConfig, slaveUdsDataProcess slaveUdsProcess ) {
			if ( is_broadcast_get_canid_bw_running() ) return;
			_bw_broadcast_get_canid = new BackgroundWorker {WorkerSupportsCancellation = true};
			_bw_broadcast_get_canid.DoWork += bw_broadcast_get_canid;
			_bw_broadcast_get_canid.RunWorkerCompleted += workerCallback;
			var bw_params = new object[] {udsComm, udsConfig, slaveUdsProcess};
			_bw_broadcast_get_canid.RunWorkerAsync(bw_params);
		}

		private void stop_broadcast_get_canid_async() {
			if ( !is_broadcast_get_canid_bw_running() ) return;
			_bw_broadcast_get_canid.CancelAsync();
			while ( is_broadcast_get_canid_bw_running() ) { }
			_bw_broadcast_get_canid = null;
		}

		private void bw_broadcast_get_canid(object sender, DoWorkEventArgs e) {
			var bw = sender as BackgroundWorker;
			var bw_params = e.Argument as object[];
			var uds_comm = bw_params[0] as UDSComm;
			var uds_config = bw_params[1] as UDSWrapper.Can_piParam_Type_Json;
			var slave_uds_process = bw_params[2] as slaveUdsDataProcess;
			var pattern = new slaveIdConfigBroadcastStatePattern(bw, uds_comm);

			while (pattern.process() >= 0) {
				// nothing to do
			}

			// wait slave uds process module catched
			// the response
			Thread.Sleep(300);

			e.Result = new object[] {pattern.stateResult, uds_config, slave_uds_process};
			if (bw.CancellationPending) e.Cancel = true;
			_bw_broadcast_get_canid = null;
		}

		#endregion

		#region 临时分配地址

		private enum firstAllocResultEnum { alloc_success, no_need_to_alloc, fail, check_after_alloc_fail }
		private BackgroundWorker _bw_first_alloc;

		private bool is_first_alloc_bw_running() {
			try {
				return (null != _bw_first_alloc && _bw_first_alloc.IsBusy);
			} catch ( Exception e ) {
				Debug.WriteLine( e );
			}
			return false;
		}

		private void first_alloc_finished_event_handler( object sender, RunWorkerCompletedEventArgs e ) {
			runErrorInfomation = string.Empty;

			if ( e.Cancelled || null != e.Error ) {
				_run_state = runStateEnum.idle;
				broadcastCommand.raiseCanExecuteChanged();
				setAllBmuIdCommand.raiseCanExecuteChanged();
				return;
			}

			var objs = e.Result as object[];
			var bw_result = (firstAllocResultEnum) objs[0];
			var uds_config = objs[1] as UDSWrapper.Can_piParam_Type_Json;
			var old_canid_crc_list = objs[2] as List<Tuple<uint, ushort>>;
			var new_rxpdu_if_map_list = objs[3] as List<Tuple<byte, uint, int>>;

			rxTpIfMapList.Clear();
			var if_rx_canid_list = uds_config.canif_piParam.pCanIf_Rxpdu_piParam_Array;
			var tp_rxpduid_list = uds_config.cantp_piParam.pCanTp_RxNsdu_piParam_Array;
			for (int i = 0; i < if_rx_canid_list.Length && i < tp_rxpduid_list.Length; i++) {
				var tp_if_map = new canRxTpIfMap((uint) tp_rxpduid_list[i].CanTpChannelId, if_rx_canid_list[i].Canid);
				rxTpIfMapList.Add( tp_if_map );
			}

			if (firstAllocResultEnum.fail == bw_result) {
				_run_state = runStateEnum.idle;
				broadcastCommand.raiseCanExecuteChanged();
				setAllBmuIdCommand.raiseCanExecuteChanged();
				runErrorInfomation = Properties.Resources.ResourceManager.GetString( "str_first_alloc_fail" );
				return;
			}

			var priority = 0;
			var bmu_txpduid_canid_list = new List<Tuple<byte, uint>>();
			foreach ( var x in new_rxpdu_if_map_list ) {
				var bmu_info =
					new bmuInfomation(rxTpIfMapList, priority++) {
						selectedTpIfMap = rxTpIfMapList[x.Item3],
						usedTpIfMap = rxTpIfMapList[x.Item3]
					};
				bmu_info.onSelectedEvent += on_bmu_selected_id_changed;
				this.bmuList.Add(bmu_info);

				var tx_map = new Tuple<byte, uint>(
					(byte) uds_config.cantp_piParam.pCanTp_TxNsdu_piParam_Array[x.Item3].CanTpChannelId,
					uds_config.canif_piParam.pCanIf_Txpdu_piParam_Array[x.Item3].Canid);
				bmu_txpduid_canid_list.Add(tx_map);
			}

			get_bmuinfo_async( uds_config, bmu_txpduid_canid_list );
		}

		private void start_first_alloc_async(UDSComm udsComm, UDSWrapper.Can_piParam_Type_Json oldUdsConfig, 
			List<Tuple<uint, ushort>> canIdCrcList, slaveUdsDataProcess slaveUdsProcess ) {
			udsComm.Stop();
			_run_state = runStateEnum.canid_first_alloc;
			broadcastCommand.raiseCanExecuteChanged();
			setAllBmuIdCommand.raiseCanExecuteChanged();
			//var slave_uds_process = new slaveUdsDataProcess();

			var uds_config = create_valid_canid_config_all_master();
			bool berr = udsComm.Start( oldUdsConfig, null );
			if (!berr) {
				_run_state = runStateEnum.idle;
				broadcastCommand.raiseCanExecuteChanged();
				setAllBmuIdCommand.raiseCanExecuteChanged();
				runErrorInfomation = Properties.Resources.ResourceManager.GetString( "str_open_can_fail" );
				return;
			}

			start_first_alloc_bw( first_alloc_finished_event_handler, udsComm, oldUdsConfig, canIdCrcList, uds_config );
		}

		private void start_first_alloc_bw( RunWorkerCompletedEventHandler workerCallback, UDSComm udsComm,
			UDSWrapper.Can_piParam_Type_Json oldUdsConfig, List<Tuple<uint, ushort>> canIdCrcList,  
			UDSWrapper.Can_piParam_Type_Json udsConfig) {
			if (is_first_alloc_bw_running() )
				return;

			_bw_first_alloc = new BackgroundWorker { WorkerSupportsCancellation = true };
			_bw_first_alloc.DoWork += bw_first_alloc;
			_bw_first_alloc.RunWorkerCompleted += workerCallback;
			var bw_params = new object[] {udsComm, oldUdsConfig, canIdCrcList, udsConfig};
			_bw_first_alloc.RunWorkerAsync(bw_params);
		}

		private void stop_first_alloc_bw() {
			if ( !is_first_alloc_bw_running() )
				return;
			_bw_first_alloc.CancelAsync();
			while (is_first_alloc_bw_running() ) {
			}
			_bw_first_alloc = null;
		}

		private void bw_first_alloc( object sender, DoWorkEventArgs e ) {
			var bw = sender as BackgroundWorker;
			var bw_params = e.Argument as object[];
			var uds_comm = bw_params[ 0 ] as UDSComm;
			var old_uds_config = bw_params[ 1 ] as UDSWrapper.Can_piParam_Type_Json;
			var old_canid_crc_list = bw_params[2] as List<Tuple<uint, ushort>>;
			var uds_config = bw_params[3] as UDSWrapper.Can_piParam_Type_Json;

			var old_if_rx_canid_list = old_uds_config.canif_piParam.pCanIf_Rxpdu_piParam_Array;
			var old_tp_tx_pduid_list = old_uds_config.cantp_piParam.pCanTp_TxNsdu_piParam_Array;

			var if_rx_canid_list = uds_config.canif_piParam.pCanIf_Rxpdu_piParam_Array;
			var tp_rx_pduid_list = uds_config.cantp_piParam.pCanTp_RxNsdu_piParam_Array;
			var tp_tx_pduid_list = uds_config.cantp_piParam.pCanTp_TxNsdu_piParam_Array;
			var temp_tp_rx_pduid_list = (from y in tp_rx_pduid_list select y.CanTpChannelId).ToArray();
			var temp_if_rx_canid_list = (from y in if_rx_canid_list select y.Canid).ToArray();

			var new_rxpdu_if_list = new List<Tuple<byte, uint, int>>();

			firstAllocResultEnum bw_result = firstAllocResultEnum.alloc_success;
			if ( old_canid_crc_list.Count > if_rx_canid_list.Length ) {
				bw_result = firstAllocResultEnum.fail;
				goto end_of_process;
			}

			bool canid_valid_flag = true;
			foreach ( var x in old_canid_crc_list ) {
				var valid_pos = Array.IndexOf( temp_if_rx_canid_list, x.Item1 );
				var dup_list = (from y in old_canid_crc_list where y.Item1 == x.Item1 select y.Item1).ToList();
				if ( valid_pos < 0 || dup_list.Count > 1 ) {
					canid_valid_flag = false;
					break;
				}
			}

			if ( false == canid_valid_flag) {
				byte k = 0;
				var new_check_canid_crc_list = new List<Tuple<uint, ushort>>();

				foreach (var x in old_canid_crc_list) {
					var temp_old_if_rx_list = (from y in old_if_rx_canid_list select y.Canid).ToArray();
					int pos = Array.IndexOf(temp_old_if_rx_list, x.Item1 );
					if (pos < 0 || pos >= old_tp_tx_pduid_list.Length)
						continue;

					byte new_rxpduid = tp_rx_pduid_list[k].CanTpChannelId;
					var cmd_set_canidv2 = new UDSDIDSetCanIdV2(new_rxpduid, x.Item2) {TimeOutMs = 100};
					uds_comm.TransmitIgnoreHeartFailFlag( cmd_set_canidv2, new_rxpduid );

					uint new_canid = if_rx_canid_list[k].Canid;		// k should be equal to rxpduid
					new_rxpdu_if_list.Add(new Tuple<byte, uint, int>(new_rxpduid, new_canid, k++));
					new_check_canid_crc_list.Add( new Tuple<uint, ushort>( new_canid, x.Item2 ) );
				}				
			}
			else {
				bw_result = firstAllocResultEnum.no_need_to_alloc;
				new_rxpdu_if_list.AddRange(from x in old_canid_crc_list
					let pos = Array.IndexOf(temp_if_rx_canid_list, x.Item1)
					select new Tuple<byte, uint, int>(temp_tp_rx_pduid_list[pos], x.Item1, pos));
			}

			Thread.Sleep(300);

			if ( check_after_first_alloc( uds_comm, old_canid_crc_list ) == firstAllocResultEnum.check_after_alloc_fail ) {
				bw_result = firstAllocResultEnum.check_after_alloc_fail;
			}
			uds_comm.Start( uds_config, null );		// restore valid canid config

			end_of_process:
			e.Result = new object[] { bw_result, uds_config, old_canid_crc_list, new_rxpdu_if_list};
			if ( bw.CancellationPending )
				e.Cancel = true;

			_bw_first_alloc = null;
		}

		private firstAllocResultEnum check_after_first_alloc( UDSComm udsComm, List<Tuple<uint, ushort>> checkCrcCanIdList ) {
			udsComm.Stop();
			var slave_uds_data_process = new slaveUdsDataProcess();
			var temp_uds_config = create_broadcast_canid_config_all_master();
			var temp_uds_rxpduids = (from x in temp_uds_config.cantp_piParam.pCanTp_RxNsdu_piParam_Array select x.CanTpChannelId).ToArray();
			var temp_uds_rxcanids = (from x in temp_uds_config.canif_piParam.pCanIf_Rxpdu_piParam_Array select x.Canid).ToArray();
			udsComm.Start( temp_uds_config, slave_uds_data_process.rx_indication_cbk );

			firstAllocResultEnum check_result = firstAllocResultEnum.alloc_success;
			var cmd_bc = new UDSDIDQueryCanIdV2 { TimeOutMs = 200 };
			udsComm.TransmitIgnoreHeartFailFlag( cmd_bc );
			Thread.Sleep( 300 );		// wait for broadcast receive done

			var resp_rxpduids = slave_uds_data_process.getSlaveResponseStrs( "CanIdV2" );
			var resp_rxpduid_crc_list = new List<Tuple<uint, ushort>>();
			foreach ( var x in resp_rxpduids ) {
				int pos = Array.IndexOf( temp_uds_rxpduids, x.Key );
				if ( pos < 0 ) {
					check_result = firstAllocResultEnum.check_after_alloc_fail;
					break;
				}

				var temp_canid = temp_uds_rxcanids[ pos ];
				ushort crc = ushort.Parse( x.Value );
				resp_rxpduid_crc_list.Add( new Tuple<uint, ushort>( temp_canid, crc ) );
			}

			var check_canid_arr = (from x in checkCrcCanIdList select x.Item1).ToArray();
			var check_crc_arr = (from x in checkCrcCanIdList select x.Item2).ToArray();
			if ( resp_rxpduid_crc_list.Count == check_canid_arr.Length ) {
				foreach ( var x in resp_rxpduid_crc_list ) {
					int pos = Array.IndexOf( check_canid_arr, x.Item1 );
					if ( pos < 0 || check_crc_arr[ pos ] != x.Item2 ) {
						check_result = firstAllocResultEnum.check_after_alloc_fail;
						break;
					}
				}
			} else {
				check_result = firstAllocResultEnum.check_after_alloc_fail;
			}

			udsComm.Stop();
			slave_uds_data_process.Dispose();
			return check_result;
		}

		#endregion

		#region 获取从机信息

		private class bmuInfoData
		{
			internal byte _tpid;
			internal uint _canid;
			internal string _sw_ver;
			internal string _hwid;
			internal string _di;
			internal string _ai;

			internal bmuInfoData() {
				_tpid = byte.MaxValue;
				_canid = uint.MaxValue;
				_sw_ver = _hwid = "N/A";
				_di = _ai = "N/A";
			}
		}

		private BackgroundWorker _bw_bmuinfo;

		private void bmuinfo_bw_finished_event_handler( object sender, RunWorkerCompletedEventArgs e ) {
			runErrorInfomation = string.Empty;

			if ( e.Cancelled || null != e.Error ) {
				_run_state = runStateEnum.idle;
				broadcastCommand.raiseCanExecuteChanged();
				setAllBmuIdCommand.raiseCanExecuteChanged();
				return;
			}

			var objs = e.Result as object[];
			//var bmus = objs[0] as ObservableCollection<bmuInfomation>;
			var info_list = objs[0] as List<bmuInfoData>;

			foreach (var x in this.bmuList) {
				var info = info_list.FirstOrDefault(y => null != x.selectedTpIfMap && x.selectedTpIfMap.tpId == y._tpid) ??
							new bmuInfoData();
				x.hwid = info._hwid;
				x.swver = info._sw_ver;
				x.din = info._di;
				x.ain = info._ai;
			}

			_run_state = runStateEnum.idle;
			broadcastCommand.raiseCanExecuteChanged();
			setAllBmuIdCommand.raiseCanExecuteChanged();
		}

		private void get_bmuinfo_async( UDSWrapper.Can_piParam_Type_Json udsConfig,
			List<Tuple<byte, uint>> bmuTxPduIdList) {
			_run_state = runStateEnum.get_bmu_info;
			broadcastCommand.raiseCanExecuteChanged();
			setAllBmuIdCommand.raiseCanExecuteChanged();

			_uds_comm.Stop();
			_uds_comm.Start(udsConfig, null);
			start_bmuinfo_bw(_uds_comm, bmuTxPduIdList);
		}

		private void start_bmuinfo_bw( UDSComm udsComm, List<Tuple<byte, uint>> bmuTxPduIdList) {
			_bw_bmuinfo = new BackgroundWorker { WorkerSupportsCancellation = true };
			_bw_bmuinfo.DoWork += bgworker_bmuinfo;
			_bw_bmuinfo.RunWorkerCompleted += bmuinfo_bw_finished_event_handler;
			var param = new object[] { udsComm, bmuTxPduIdList };
			_bw_bmuinfo.RunWorkerAsync(param);
		}

		private void bgworker_bmuinfo( object sender, DoWorkEventArgs e ) {
			var bw = sender as BackgroundWorker;
			var objs = e.Argument as object[];
			var uds_comm = objs[0] as UDSComm;
			var bmu_tx_pduid_list = objs[1] as List<Tuple<byte, uint>>;
			//var bmus = objs[2] as ObservableCollection<bmuInfomation>;


			Thread.Sleep( 300 );
			var info_list = new List<bmuInfoData>();
			foreach (var x in bmu_tx_pduid_list) {
				var info = new bmuInfoData {_tpid = x.Item1, _canid = x.Item2};

				var cmd_hwid = new UDSDIDBCUHWIDString() {TimeOutMs = 100};
				uds_comm.TransmitIgnoreHeartFailFlag(cmd_hwid, x.Item1);
				info._hwid = cmd_hwid.getHwidString().ValueString;

				var cmd_swver = new UDSDIDBCUSWVersionString() {TimeOutMs = 100};
				uds_comm.TransmitIgnoreHeartFailFlag(cmd_swver, x.Item1);
				info._sw_ver = cmd_swver.getSWVersionString().ValueString;

				var cmd_di = new UDSDIDDiStatusList() { TimeOutMs = 100 };
				uds_comm.TransmitIgnoreHeartFailFlag( cmd_di, x.Item1 );
				var di_arr = cmd_di.getList();
				if ( di_arr.Length > 0 && di_arr[ 0 ].isDataOk ) {
					info._di = (bool)di_arr[ 0 ].value ? "高" : "低";
				} else {
					info._di = new udsBool().ToString();
				}

				var cmd_ai = new UDSDIDAiStatusList() { TimeOutMs = 100 };
				uds_comm.TransmitIgnoreHeartFailFlag( cmd_ai, x.Item1 );
				if ( cmd_ai.getList().Length > 0 ) {
					info._ai = cmd_ai.getList()[ 0 ].ValueString;
				} else {
					info._ai = udsIndVoltage.InstanceNA().ValueString;
				}

				info_list.Add(info);
			}

			e.Result = new object[] { info_list };
			if ( bw.CancellationPending )
				e.Cancel = true;

			_bw_bmuinfo = null;
		}

		#endregion

		#region 设置所有BMU ID

		private enum setAllBmuIdResultEnum { ok, set_fail, validate_fail }
		private BackgroundWorker _set_all_bmu_id_bw_worker;

		private void set_all_bmu_id_bw_finished_event_handler( object sender, RunWorkerCompletedEventArgs e ) {
			runErrorInfomation = string.Empty;

			if ( e.Cancelled || null != e.Error ) {
				this.bmuList.Clear();
				_run_state = runStateEnum.idle;
				broadcastCommand.raiseCanExecuteChanged();
				setAllBmuIdCommand.raiseCanExecuteChanged();
				return;
			}

			var objs = e.Result as object[];
			var result = (setAllBmuIdResultEnum) objs[0];
			var uds_config = objs[1] as UDSWrapper.Can_piParam_Type_Json;
			var bmu_rxpdu_if_map_list = objs[2] as Tuple<byte, uint>[];

			if (result != setAllBmuIdResultEnum.ok) {
				this.bmuList.Clear();
				_run_state = runStateEnum.idle;
				broadcastCommand.raiseCanExecuteChanged();
				setAllBmuIdCommand.raiseCanExecuteChanged();
				runErrorInfomation = Properties.Resources.ResourceManager.GetString( "str_set_all_id_fail" );
				return;
			}

			this.bmuList.Clear();
			this.rxTpIfMapList.Clear();
			var rxpdu_list = (from x in uds_config.cantp_piParam.pCanTp_RxNsdu_piParam_Array select x.CanTpChannelId).ToArray();
			var rxcanid_list = (from x in uds_config.canif_piParam.pCanIf_Rxpdu_piParam_Array select x.Canid).ToArray();
			for (int i = 0; i < rxpdu_list.Length; i++) {
				this.rxTpIfMapList.Add(new canRxTpIfMap(rxpdu_list[i], rxcanid_list[i]));
			}

			int priority = 0;
			var bmu_txpdu_if_map_list = new List<Tuple<byte, uint>>();
			foreach (var x in bmu_rxpdu_if_map_list) {
				int pos = Array.IndexOf(rxpdu_list, x.Item1);
				var bmu = new bmuInfomation(this.rxTpIfMapList, priority++) {
					selectedTpIfMap = this.rxTpIfMapList[pos],
					usedTpIfMap = this.rxTpIfMapList[pos]
				};
				this.bmuList.Add(bmu);

				var tx_map = new Tuple<byte, uint>(uds_config.cantp_piParam.pCanTp_TxNsdu_piParam_Array[pos].CanTpChannelId,
					uds_config.canif_piParam.pCanIf_Txpdu_piParam_Array[pos].Canid);
				bmu_txpdu_if_map_list.Add(tx_map);
			}

			get_bmuinfo_async( uds_config, bmu_txpdu_if_map_list );
		}

		private void set_all_bmu_id_async() {
			_run_state = runStateEnum.set_all_bmu_id;
			broadcastCommand.raiseCanExecuteChanged();
			setAllBmuIdCommand.raiseCanExecuteChanged();

			Debug.Assert(bmuList.All(x => x.selectedTpIfMap != null));
			Debug.Assert(bmuList.All(x => x.usedTpIfMap != null));

			var uds_config = create_valid_canid_config_all_master();
			var valid_tp_canid_map_list = (from x in bmuList
				select new Tuple<byte, uint>((byte) x.usedTpIfMap.tpId, x.usedTpIfMap.canId)).ToArray();
			var will_set_tp_canid_map_list = (from x in bmuList
				select new Tuple<byte, uint>((byte) x.selectedTpIfMap.tpId, x.selectedTpIfMap.canId)).ToArray();
		
			_uds_comm.Stop();
			bool berr = _uds_comm.Start(uds_config, null);
			if (!berr) {
				_run_state = runStateEnum.idle;
				broadcastCommand.raiseCanExecuteChanged();
				setAllBmuIdCommand.raiseCanExecuteChanged();
				runErrorInfomation = Properties.Resources.ResourceManager.GetString( "str_open_can_fail" );
				return;
			}

			start_set_all_bmu_id_bw(_uds_comm, uds_config, valid_tp_canid_map_list, will_set_tp_canid_map_list);
		}

		private void start_set_all_bmu_id_bw(UDSComm udsComm, UDSWrapper.Can_piParam_Type_Json udsConfig, 
			Tuple<byte, uint>[] validTpCanIdMapList, Tuple<byte, uint>[] willSetTpCanIdMapList) {
			_set_all_bmu_id_bw_worker = new BackgroundWorker { WorkerSupportsCancellation = true };
			_set_all_bmu_id_bw_worker.DoWork += bw_set_all_bmu_id;
			_set_all_bmu_id_bw_worker.RunWorkerCompleted += set_all_bmu_id_bw_finished_event_handler;
			var param = new object[] {udsComm, udsConfig, validTpCanIdMapList, willSetTpCanIdMapList};
			_set_all_bmu_id_bw_worker.RunWorkerAsync(param);
		}

		private void bw_set_all_bmu_id( object sender, DoWorkEventArgs e ) {
			var bw = sender as BackgroundWorker;
			var objs = e.Argument as object[];
			var uds_comm = objs[0] as UDSComm;
			var uds_config = objs[1] as UDSWrapper.Can_piParam_Type_Json;
			var valid_tp_canid_map_list = objs[2] as Tuple<byte, uint>[];
			var will_set_tp_canid_map_list = objs[3] as Tuple<byte, uint>[];
			Thread.Sleep(100);

			var result = setAllBmuIdResultEnum.ok;
			Debug.Assert(valid_tp_canid_map_list.Length == will_set_tp_canid_map_list.Length);

			for (int i = 0; i < valid_tp_canid_map_list.Length; i++) {
				byte newid = will_set_tp_canid_map_list[i].Item1;
				byte usedid = valid_tp_canid_map_list[i].Item1;		// should be equal to index of array
				var cmd_set = new UDSDIDSetCanId(newid);
				bool bok = uds_comm.TransmitIgnoreHeartFailFlag(cmd_set, usedid);
				if (!bok) {
					result = setAllBmuIdResultEnum.set_fail;
					goto end_of_process;
				}
			}

			foreach (var x in valid_tp_canid_map_list) {
				var cmd_reset = new UDSDIDResetBCU() {TimeOutMs = 200};
				uds_comm.TransmitIgnoreHeartFailFlag(cmd_reset, x.Item1);
			}

			Thread.Sleep(4000);

			foreach ( var x in will_set_tp_canid_map_list) {
				var cmd_test = new UDSDIDBCUSWVersionString() {TimeOutMs = 200};
				uds_comm.TransmitIgnoreHeartFailFlag(cmd_test, x.Item1);
				if (cmd_test.isTimeOut) {
					result = setAllBmuIdResultEnum.validate_fail;
					break;
				}
			}

			e.Result = new object[] {result, uds_config, will_set_tp_canid_map_list};

			end_of_process:
			if ( bw.CancellationPending )
				e.Cancel = true;

			_set_all_bmu_id_bw_worker = null;
		}

		#endregion

		#region udsConfigPath

		public static string udsConfigPath {
			get { return Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, @"net_libs\uds_config.json"); }
		}

		#endregion

		#region 创建UDS节点配置

		private static UDSWrapper.Can_piParam_Type_Json create_valid_canid_config_all_master() {
			string config_path = udsConfigPath;
			var uds_config = UDSWrapper.Can_piParam_Type_Json.readFromJsonFile( config_path );
			var canif_rx_pdu_list = new List<UDSWrapper.CanIf_Rxpdu_piParam_Type>();
			var canif_tx_pdu_list = new List<UDSWrapper.CanIf_Txpdu_piParam_Type>();
			var cantp_rx_pdu_list = new List<UDSWrapper.CanTp_RxNsdu_piParam_Type>();
			var cantp_tx_pdu_list = new List<UDSWrapper.CanTp_TxNsdu_piParam_Type>();

			for ( uint i = 0; i < 15; i++ ) {
				canif_tx_pdu_list.Add( new UDSWrapper.CanIf_Txpdu_piParam_Type() { Canid = 0x770 + i, group = 0 } );
				canif_rx_pdu_list.Add( new UDSWrapper.CanIf_Rxpdu_piParam_Type() { Canid = 0x780 + i, group = 0 } );
				cantp_tx_pdu_list.Add( new UDSWrapper.CanTp_TxNsdu_piParam_Type( (byte)i, (byte)i, (byte)i ) );
				cantp_rx_pdu_list.Add( new UDSWrapper.CanTp_RxNsdu_piParam_Type( (byte)i, (byte)i, (byte)i ) );
			}

			uds_config.enable_rx_master_cb_notify = 1;
			uds_config.canif_piParam.txPdu_Host = 0x7df;
			uds_config.canif_piParam.pCanIf_Rxpdu_piParam_Array = canif_rx_pdu_list.ToArray();
			uds_config.canif_piParam.pCanIf_Txpdu_piParam_Array = canif_tx_pdu_list.ToArray();
			uds_config.cantp_piParam.pCanTp_RxNsdu_piParam_Array = cantp_rx_pdu_list.ToArray();
			uds_config.cantp_piParam.pCanTp_TxNsdu_piParam_Array = cantp_tx_pdu_list.ToArray();

			return uds_config;
		}

		private static UDSWrapper.Can_piParam_Type_Json create_broadcast_canid_config_all_master() {
			var uds_config = create_valid_canid_config_all_master();
			var canif_rx_pdu_list = uds_config.canif_piParam.pCanIf_Rxpdu_piParam_Array.ToList();
			var canif_tx_pdu_list = uds_config.canif_piParam.pCanIf_Txpdu_piParam_Array.ToList();
			var cantp_rx_pdu_list = uds_config.cantp_piParam.pCanTp_RxNsdu_piParam_Array.ToList();
			var cantp_tx_pdu_list = uds_config.cantp_piParam.pCanTp_TxNsdu_piParam_Array.ToList();

			// add tx group
			byte count = (byte)canif_tx_pdu_list.Count;
			canif_tx_pdu_list.Add( new UDSWrapper.CanIf_Txpdu_piParam_Type() { Canid = 0x747, group = 0 } );
			cantp_tx_pdu_list.Add( new UDSWrapper.CanTp_TxNsdu_piParam_Type( count, count, count ) );
			count++;

			canif_tx_pdu_list.Add( new UDSWrapper.CanIf_Txpdu_piParam_Type() { Canid = 0x7df, group = 0 } );
			cantp_tx_pdu_list.Add( new UDSWrapper.CanTp_TxNsdu_piParam_Type( count, count, count ) );

			// add rx group
			count = (byte)canif_rx_pdu_list.Count;
			canif_rx_pdu_list.Add( new UDSWrapper.CanIf_Rxpdu_piParam_Type() { Canid = 0x74f, group = 0 } );
			cantp_rx_pdu_list.Add( new UDSWrapper.CanTp_RxNsdu_piParam_Type( count, count, count ) );

			uds_config.canif_piParam.pCanIf_Rxpdu_piParam_Array = canif_rx_pdu_list.ToArray();
			uds_config.canif_piParam.pCanIf_Txpdu_piParam_Array = canif_tx_pdu_list.ToArray();
			uds_config.cantp_piParam.pCanTp_RxNsdu_piParam_Array = cantp_rx_pdu_list.ToArray();
			uds_config.cantp_piParam.pCanTp_TxNsdu_piParam_Array = cantp_tx_pdu_list.ToArray();

			return uds_config;
		}

		#endregion

	}


	#region canRxTpIfMap / canRxConfigFile

	public class canRxTpIfMap : viewModelBase
	{
		public uint tpId { get; private set; }
		public uint canId { get; private set; }

		public canRxTpIfMap(uint tpId, uint canId) {
			this.tpId = tpId;
			this.canId = canId;
		}
	}

	public class canRxConfigFile : viewModelBase
	{
		public ObservableCollection<canRxTpIfMap> tpIfList { get; set; }

		public canRxConfigFile() {
			tpIfList = new ObservableCollection<canRxTpIfMap>();
			tpIfList.Add( new canRxTpIfMap( 0, 0 ) );
			tpIfList.Add( new canRxTpIfMap( 1, 1 ) );
			tpIfList.Add( new canRxTpIfMap( 2, 2 ) );
		}
	}

	#endregion

}
