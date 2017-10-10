using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Input;
using slave_uds;
using SlaveIdConfigNet2;
using uds_comm;
using uds_comm.interop;

namespace SlaveIdConfigNet2Wpf.viewModel 
{

	#region observableObject

	public class observableObject : INotifyPropertyChanged {
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
		public Action _execute_command = null;		// A method prototype without return value, without param.
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

	public class bmuInfomation : observableObject
	{
		public class infoKv {
			public string key { get; set; }
			public string value { get; set; }
		}

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
				if (x.key == key) return x.value;
			}

			return string.Empty;
		}

		private void set_stt( string key, string v ) {
			foreach (var x in _bcu_stt_list) {
				if (x.key != key) continue;	
				x.value = v;
				raisePropertyChanged("bcuSttList");
			}
		}

		private bool kv_info_key_exist(string key) {
			var temp = _bcu_stt_list.FirstOrDefault(x => x.key == key);
			return (null != temp) ? true : false;
		}

		private infoKv create_kv_info(string key, string defValue) {
			var temp = new infoKv {key = key, value = defValue};
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
				set_stt(keyDin, value );
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
				if ( null != _selected_tp_if_map ) {
					var str = string.Format( "bmuInfomation {0}, selected canId changed: 0x{1:x}",
						_priority, _selected_tp_if_map.canId );
					Debug.WriteLine(str);
				}
			}
		}

		private readonly int _priority;
		public bmuInfomation(ObservableCollection<canRxTpIfMap> mapList, int priority ) {
			_bcu_stt_list = new ObservableCollection<infoKv>();
			hwid = swver = ain = din = "N/A";
			rxTpIfMapList = mapList;
			_priority = priority;
		}
	}
	
	public class bmuCollectionViewModel /*: observableObject*/
	{
		private ObservableCollection<canRxTpIfMap> _rx_tp_if_map_list;
		private ObservableCollection<bmuInfomation> _bmu_info_list;
		private readonly UDSComm _uds_comm;

		public delegateCommandNoParameter broadcastCommand { get; set; }

		public ObservableCollection<bmuInfomation> bmuList {
			get {
				return _bmu_info_list;
			}
			set {
				if (null != value)
					_bmu_info_list = value;
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

		public enum runStateEnum { idle, bc_before_canid_first_alloc, canid_first_alloc, bc_after_first_canid_alloc }
		private runStateEnum _run_state = runStateEnum.idle;

		public bmuCollectionViewModel( ObservableCollection<canRxTpIfMap> tpIfMapList) {
			_rx_tp_if_map_list = new ObservableCollection<canRxTpIfMap>();
			_bmu_info_list = new ObservableCollection<bmuInfomation>();
			_uds_comm = UDSComm.getInstance();

			broadcastCommand = new delegateCommandNoParameter {
				_execute_command = new Action(start_broadcast),
				_can_execute_command = new Func<bool>(is_broadcast_bw_stopped)
			};
		}

		public void test_add_bmu() {
            broadcastCommand.raiseCanExecuteChanged();
			this.bmuList.Add(new bmuInfomation(rxTpIfMapList, bmuList.Count));
		}

		private bool is_broadcast_bw_stopped() {
			if (_run_state == runStateEnum.idle)
				return true;
			return false;
		}

        #region test bw

        private BackgroundWorker _test_bw_worker;
        private void test_bw_finished_event_handler(object sender, RunWorkerCompletedEventArgs e)
        {
            broadcastCommand.raiseCanExecuteChanged();
			this.bmuList.Add( new bmuInfomation( rxTpIfMapList, bmuList.Count ) );
        }

        private void start_test_bw()
        {
            _test_bw_worker = new BackgroundWorker { WorkerSupportsCancellation = true };
            _test_bw_worker.DoWork += bgworker_test;
            _test_bw_worker.RunWorkerCompleted += test_bw_finished_event_handler;
            _test_bw_worker.RunWorkerAsync();
        }

        private void bgworker_test(object sender, DoWorkEventArgs e)
        {
            var bw = sender as BackgroundWorker;
            Thread.Sleep(300);

            if (bw.CancellationPending)
                e.Cancel = true;

            _test_bw_worker = null;
        }

        #endregion

        #region 广播任务

        private BackgroundWorker _broadcast_worker;

		private bool is_broadcast_bw_running() {
			try {
				return (null != _broadcast_worker && _broadcast_worker.IsBusy);
			} catch ( Exception e ) {
				Debug.WriteLine( e );
			}
			return false;
		}

		private void broadcast_finished_event_handler(object sender, RunWorkerCompletedEventArgs e) {
			if ( e.Cancelled || null != e.Error ) {
				_run_state = runStateEnum.idle;
				broadcastCommand.raiseCanExecuteChanged();
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
				return;
			}

			var resp_rxpduids = slave_uds_process.getSlaveResponseStrs( "CanIdV2" );
			var rxpduid_crc_list = new List<Tuple<uint, ushort>>();
			foreach (var x in resp_rxpduids) {
				var temp_tp_rxpdu_list = (from y in tp_rxpdu_list select y.CanTpChannelId).ToArray();
				int pos = Array.IndexOf(temp_tp_rxpdu_list, x.Key );
				if (pos >= 0 && pos < if_canid_list.Length) {
					ushort crc = ushort.Parse(x.Value);
					rxpduid_crc_list.Add(new Tuple<uint, ushort>(if_canid_list[pos].Canid, crc));
				}
			}

			slave_uds_process.Dispose();
			start_first_alloc(uds_config, rxpduid_crc_list, null);
		}

		private void start_broadcast() {
			_uds_comm.Stop();
			_run_state = runStateEnum.bc_before_canid_first_alloc;
			broadcastCommand.raiseCanExecuteChanged();
			bmuList.Clear();

			var slave_uds_process = new slaveUdsDataProcess();

			var config_path = udsConfigPath;
			var uds_config = UDSWrapper.Can_piParam_Type_Json.readFromJsonFile(config_path);
			var canif_rx_pdu_list = new List<UDSWrapper.CanIf_Rxpdu_piParam_Type>();
			var canif_tx_pdu_list = new List<UDSWrapper.CanIf_Txpdu_piParam_Type>();
			var cantp_rx_pdu_list = new List<UDSWrapper.CanTp_RxNsdu_piParam_Type>();
			var cantp_tx_pdu_list = new List<UDSWrapper.CanTp_TxNsdu_piParam_Type>();

			byte i = 0;
			for (i = 0; i < 15; i++) {
				canif_tx_pdu_list.Add(new UDSWrapper.CanIf_Txpdu_piParam_Type() {Canid = 0x770 + (uint) i, group = 0});
				cantp_tx_pdu_list.Add(new UDSWrapper.CanTp_TxNsdu_piParam_Type(i, i, i));
			}
			canif_tx_pdu_list.Add(new UDSWrapper.CanIf_Txpdu_piParam_Type() {Canid = 0x747, group = 0});
			cantp_tx_pdu_list.Add( new UDSWrapper.CanTp_TxNsdu_piParam_Type( i, i, i ) );
			i++;

			canif_tx_pdu_list.Add(new UDSWrapper.CanIf_Txpdu_piParam_Type() {Canid = 0x7df, group = 0});
			cantp_tx_pdu_list.Add(new UDSWrapper.CanTp_TxNsdu_piParam_Type(i, i, i));


			for ( i = 0; i < 15; i++ ) {
				canif_rx_pdu_list.Add(new UDSWrapper.CanIf_Rxpdu_piParam_Type() {Canid = 0x780 + (uint)i, group = 0});
				cantp_rx_pdu_list.Add(new UDSWrapper.CanTp_RxNsdu_piParam_Type(i, i, i));
			}
			canif_rx_pdu_list.Add(new UDSWrapper.CanIf_Rxpdu_piParam_Type() {Canid = 0x74f, group = 0});
			cantp_rx_pdu_list.Add(new UDSWrapper.CanTp_RxNsdu_piParam_Type(i, i, i));

			uds_config.enable_rx_master_cb_notify = 1;
			uds_config.canif_piParam.pCanIf_Rxpdu_piParam_Array = canif_rx_pdu_list.ToArray();
			uds_config.canif_piParam.pCanIf_Txpdu_piParam_Array = canif_tx_pdu_list.ToArray();
			uds_config.cantp_piParam.pCanTp_RxNsdu_piParam_Array = cantp_rx_pdu_list.ToArray();
			uds_config.cantp_piParam.pCanTp_TxNsdu_piParam_Array = cantp_tx_pdu_list.ToArray();
			uds_config.canif_piParam.txPdu_Host = 0x7df;

			bool berr = _uds_comm.Start(uds_config, slave_uds_process.rx_indication_cbk);
			start_broadcast_bw(broadcast_finished_event_handler, _uds_comm, uds_config, slave_uds_process);
		}

		private void start_broadcast_bw( RunWorkerCompletedEventHandler workerCallback, UDSComm udsComm,
			UDSWrapper.Can_piParam_Type_Json udsConfig, slaveUdsDataProcess slaveUdsProcess ) {
			if ( is_broadcast_bw_running() ) return;
			_broadcast_worker = new BackgroundWorker {WorkerSupportsCancellation = true};
			_broadcast_worker.DoWork += bgworker_broadcast;
			_broadcast_worker.RunWorkerCompleted += workerCallback;
			var bw_params = new object[] {udsComm, udsConfig, slaveUdsProcess};
			_broadcast_worker.RunWorkerAsync(bw_params);
		}

		private void stop_broadcast_bw() {
			if ( !is_broadcast_bw_running() ) return;
			_broadcast_worker.CancelAsync();
			while ( is_broadcast_bw_running() ) { }
			_broadcast_worker = null;
		}

		private void bgworker_broadcast(object sender, DoWorkEventArgs e) {
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
			_broadcast_worker = null;
		}

		#endregion

		#region 临时分配地址

		private enum firstAllocResultEnum { alloc_success, no_need_to_alloc, fail}
		private BackgroundWorker _first_alloc_worker;

		private bool is_first_alloc_bw_running() {
			try {
				return (null != _first_alloc_worker && _first_alloc_worker.IsBusy);
			} catch ( Exception e ) {
				Debug.WriteLine( e );
			}
			return false;
		}

		private void first_alloc_finished_event_handler( object sender, RunWorkerCompletedEventArgs e ) {
			if ( e.Cancelled || null != e.Error ) {
				_run_state = runStateEnum.idle;
				broadcastCommand.raiseCanExecuteChanged();
				return;
			}

			var objs = e.Result as object[];
			var bw_result = (firstAllocResultEnum) objs[0];
			var uds_config = objs[1] as UDSWrapper.Can_piParam_Type_Json;
			var old_canid_crc_list = objs[2] as List<Tuple<uint, ushort>>;
			var new_tp_if_map_list = objs[3] as List<Tuple<byte, uint, int>>;

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
				return;
			}

			var priority = 0;
			foreach (var x in new_tp_if_map_list) {
				var bmu_info = new bmuInfomation(rxTpIfMapList, priority++) {selectedTpIfMap = rxTpIfMapList[x.Item3]};
				this.bmuList.Add(bmu_info);
			}

			_run_state = runStateEnum.idle;
			broadcastCommand.raiseCanExecuteChanged();
		}

		private void start_first_alloc(UDSWrapper.Can_piParam_Type_Json oldUdsConfig, 
			List<Tuple<uint, ushort>> canIdCrcList, slaveUdsDataProcess slaveUdsProcess ) {
			_uds_comm.Stop();
			_run_state = runStateEnum.canid_first_alloc;
			broadcastCommand.raiseCanExecuteChanged();
			//var slave_uds_process = new slaveUdsDataProcess();

			string config_path = udsConfigPath;
			var uds_config = UDSWrapper.Can_piParam_Type_Json.readFromJsonFile( config_path );
			var canif_rx_pdu_list = new List<UDSWrapper.CanIf_Rxpdu_piParam_Type>();
			var canif_tx_pdu_list = new List<UDSWrapper.CanIf_Txpdu_piParam_Type>();
			var cantp_rx_pdu_list = new List<UDSWrapper.CanTp_RxNsdu_piParam_Type>();
			var cantp_tx_pdu_list = new List<UDSWrapper.CanTp_TxNsdu_piParam_Type>();

			for ( uint i = 0; i < 15; i++ ) {
				canif_tx_pdu_list.Add(new UDSWrapper.CanIf_Txpdu_piParam_Type() {Canid = 0x770 + i, group = 0});
				canif_rx_pdu_list.Add(new UDSWrapper.CanIf_Rxpdu_piParam_Type() {Canid = 0x780 + i, group = 0});
				cantp_tx_pdu_list.Add(new UDSWrapper.CanTp_TxNsdu_piParam_Type((byte) i, (byte) i, (byte) i));
				cantp_rx_pdu_list.Add(new UDSWrapper.CanTp_RxNsdu_piParam_Type((byte) i, (byte) i, (byte) i));
			}

			uds_config.enable_rx_master_cb_notify = 1;
			uds_config.canif_piParam.pCanIf_Rxpdu_piParam_Array = canif_rx_pdu_list.ToArray();
			uds_config.canif_piParam.pCanIf_Txpdu_piParam_Array = canif_tx_pdu_list.ToArray();
			uds_config.cantp_piParam.pCanTp_RxNsdu_piParam_Array = cantp_rx_pdu_list.ToArray();
			uds_config.cantp_piParam.pCanTp_TxNsdu_piParam_Array = cantp_tx_pdu_list.ToArray();

			bool berr = _uds_comm.Start(oldUdsConfig, null);
			start_first_alloc_bw(first_alloc_finished_event_handler, _uds_comm, oldUdsConfig, canIdCrcList, uds_config);
		}

		private void start_first_alloc_bw( RunWorkerCompletedEventHandler workerCallback, UDSComm udsComm,
			UDSWrapper.Can_piParam_Type_Json oldUdsConfig, List<Tuple<uint, ushort>> canIdCrcList,  
			UDSWrapper.Can_piParam_Type_Json udsConfig) {
			if (is_first_alloc_bw_running() )
				return;

			_first_alloc_worker = new BackgroundWorker { WorkerSupportsCancellation = true };
			_first_alloc_worker.DoWork += bgworker_first_alloc;
			_first_alloc_worker.RunWorkerCompleted += workerCallback;
			var bw_params = new object[] {udsComm, oldUdsConfig, canIdCrcList, udsConfig};
			_first_alloc_worker.RunWorkerAsync(bw_params);
		}

		private void stop_first_alloc_bw() {
			if ( !is_first_alloc_bw_running() )
				return;
			_first_alloc_worker.CancelAsync();
			while (is_first_alloc_bw_running() ) {
			}
			_first_alloc_worker = null;
		}

		private void bgworker_first_alloc( object sender, DoWorkEventArgs e ) {
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

			var new_tp_if_list = new List<Tuple<byte, uint, int>>();

			firstAllocResultEnum bw_result = firstAllocResultEnum.alloc_success;
			if ( old_canid_crc_list.Count > if_rx_canid_list.Length ) {
				bw_result = firstAllocResultEnum.fail;
				goto end_of_process;
			}

			var canid_valid_flag = true;
			foreach (var x in old_canid_crc_list) {
				var temp2_list = (from y in temp_if_rx_canid_list where y == x.Item1 select y).ToArray();
				if (temp2_list.Length != 1) {
					canid_valid_flag = false;
					break;
				}
			}

			if ( false == canid_valid_flag) {
				byte k = 0;
				foreach (var x in old_canid_crc_list) {
					var temp_old_if_rx_list = (from y in old_if_rx_canid_list select y.Canid).ToArray();
					int pos = Array.IndexOf(temp_old_if_rx_list, x.Item1 );
					if (pos < 0 || pos >= old_tp_tx_pduid_list.Length)
						continue;

					byte new_rxpduid = tp_rx_pduid_list[k].CanTpChannelId;
					var cmd_set_canidv2 = new UDSDIDSetCanIdV2(new_rxpduid, x.Item2) {TimeOutMs = 100};
					uds_comm.TransmitIgnoreHeartFailFlag(cmd_set_canidv2, (byte) pos);
					if (!cmd_set_canidv2.isPositiveResponse)
						continue;

					uint new_canid = if_rx_canid_list[k].Canid;		// k should be equal to rxpduid
					new_tp_if_list.Add(new Tuple<byte, uint, int>(new_rxpduid, new_canid, k));
					k++;
				}
			}
			else {
				bw_result = firstAllocResultEnum.no_need_to_alloc;
				new_tp_if_list.AddRange(from x in old_canid_crc_list
					let pos = Array.IndexOf(temp_if_rx_canid_list, x.Item1)
					select new Tuple<byte, uint, int>(temp_tp_rx_pduid_list[pos], x.Item1, pos));
			}

			Thread.Sleep(300);

			end_of_process:
			e.Result = new object[] { bw_result, uds_config, old_canid_crc_list, new_tp_if_list};
			if ( bw.CancellationPending )
				e.Cancel = true;

			_first_alloc_worker = null;
		}

		#endregion

		#region udsConfigPath

		public static string udsConfigPath {
			get { return Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, @"net_libs\uds_config.json"); }
		}

		#endregion

	}


	#region canRxTpIfMap / canRxConfigFile

	public class canRxTpIfMap : observableObject
	{
		public uint tpId { get; private set; }
		public uint canId { get; private set; }

		public canRxTpIfMap(uint tpId, uint canId) {
			this.tpId = tpId;
			this.canId = canId;
		}
	}

	public class canRxConfigFile : observableObject
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
