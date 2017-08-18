using System;
using System.ComponentModel;
using System.Threading;
using uds_comm;
using s19_process.rw_length_converter;
using System.Diagnostics;

namespace modules.data_upload.upload_worker
{
	public class dataUploadWorker
	{
		private const uint memSize128M = 128 * 1024 * 1024;
		private const uint lowAddrMask = 0x07FFFFFF;
		private const byte highAddrOffset = 27;		// 高5位作为地址标示符

		private const int recvBuffSize = 1024 * 8;
		private readonly byte[] _recv_buffer = new byte[recvBuffSize];
		private const int readRetryNum = 3;

		private readonly UDSComm _uds_comm;
		private s19RwLengthConverter _rw_len_conv;
		private readonly BackgroundWorker _bw;
		private readonly Thread _thread;


		#region 构造函数  dataUploadWorker

		public dataUploadWorker( UDSComm udsComm , BackgroundWorker bw ) {
			_uds_comm = udsComm;
			_bw = bw;
			init_rw_len_conv();
		}

		public dataUploadWorker( UDSComm udsComm , Thread myThread ) {
			_uds_comm = udsComm;
			_thread = myThread;
			init_rw_len_conv();
		}

		public dataUploadWorker( UDSComm udsComm ) {
			_uds_comm = udsComm;
			init_rw_len_conv();
		}

		~dataUploadWorker() {
			remove_all_section_event_handler();
			remove_all_page_upload_finished_event_handler();
		}

		protected void init_rw_len_conv() {
			_rw_len_conv = new s19RwLengthConverter( S19UnitMemSizeValueEnum.size_byte );
		}
		#endregion

		public byte[] qreadAddressData( uint address , ushort length ) {
			ushort new_mem_size = _rw_len_conv.convert_mem_size(length);
			uint new_addr = 0;
			if ( address >= memSize128M ) new_addr = (address & lowAddrMask) + (0x02 << highAddrOffset);
			else new_addr = (address & lowAddrMask) + (0x01 << highAddrOffset);

			var cmd_query = new UDSDataUploadQueryStart( new_addr , new_mem_size ) { TimeOutMs = 600 };
			_uds_comm.Transmit( cmd_query , readRetryNum );
			if ( !cmd_query.isSuccess() ) {
				Debug.WriteLine( "data upload worker, qreadAddressData UDSDataUploadQueryStart fail" );
				return null;
			}

			byte sequence_id = 1;
			int accu_num_received = 0;

			while ( accu_num_received < length ) {
				var data = read_addr_data(sequence_id++);
				if (!data.Item1 || null == data.Item2 || data.Item2.Length <= 0) break;		// in case of read error data

				Array.Copy(data.Item2, 0, _recv_buffer, accu_num_received, data.Item2.Length);
				accu_num_received += data.Item2.Length;
			}

			var cmd_end = new UDSDataDownloadQueryBlockEnd() { TimeOutMs = 600 };
			_uds_comm.Transmit( cmd_end , readRetryNum );
			if ( !cmd_end.isSuccess() ) {
				Debug.WriteLine( "data upload worker, qreadAddressData UDSDataDownloadQueryBlockEnd fail" );
				return null;
			}

			if ( 0 == accu_num_received ) return null;
			var all_data = new byte[accu_num_received];
			Array.Copy(_recv_buffer, all_data, all_data.Length);
			return all_data;
		}

		protected Tuple<bool, byte[]> read_addr_data( byte sequenceId ) {
			var cmd = new UDSDataUploadData(sequenceId) {TimeOutMs = 600};
			_uds_comm.Transmit( cmd , readRetryNum );
			bool b1 = cmd.isSuccess();
			if ( !b1 ) {
				Debug.WriteLine( "data upload worker, read_addr_data UDSDataUploadData fail" );
				return Tuple.Create( false, new byte[0]);
			}

			var data = cmd.getData();
			return Tuple.Create( true, data);
		}

		#region 事件通知及清理

		public event EventHandler onSectionEvent;
		public class uploadSectionEventArgs : EventArgs
		{

		}

		protected void upload_section_event_notify() {

		}

		protected void remove_all_section_event_handler() {
			if ( null == onSectionEvent ) return;
			var dary = onSectionEvent.GetInvocationList();
			if ( dary.Length <= 0 ) return;
			foreach ( var del in dary ) {
				onSectionEvent -= del as EventHandler;
			}
		}

		public event EventHandler onPageUploadFinishedEvent;
		public class pageUploadFinishedEventArgs : EventArgs
		{

		}

		protected void page_upload_finished_event_notify() {

		}

		protected void remove_all_page_upload_finished_event_handler() {
			if ( null == onPageUploadFinishedEvent ) return;
			var dary = onPageUploadFinishedEvent.GetInvocationList();
			if ( dary.Length <= 0 ) return;
			foreach ( var del in dary ) {
				onPageUploadFinishedEvent -= del as EventHandler;
			}
		}
		
		#endregion

		#region 工具  is_task_alive

		private bool is_task_alive() {
			if ( null != _bw ) return !(_bw.CancellationPending);
			else if ( null != _thread ) return _thread.IsAlive;
			return true;		// if no host thread or bw, process to end
		}

		#endregion

	}
}
