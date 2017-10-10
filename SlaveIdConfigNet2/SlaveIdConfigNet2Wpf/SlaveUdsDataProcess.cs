using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using native_pipe;
using uds_comm;
using uds_comm.interop;

namespace slave_uds
{
	// http://buiba.blogspot.co.uk/2009/06/using-winapi-createfile-readfile.html
	// https://github.com/DynamsoftRD/windows-pipe-communication

	public sealed class slaveUdsDataProcess : IDisposable
	{
		private bool _disposed;
		private readonly Stopwatch _stop_watch;
		private readonly System.Windows.Threading.DispatcherTimer _tmr, _tmr2;
		private readonly Dictionary<byte[], sortableDidCmd> _did_sort_dicts;

		private IntPtr _pipe_write;
		private IntPtr _pipe_read;

		#region 初始化

		public slaveUdsDataProcess() {
			_did_sort_dicts = new Dictionary<byte[], sortableDidCmd>();
			init_did_sort_dicts();

			string guid = Guid.NewGuid().ToString().Replace('-', '_');
			string pipe_name = "\\\\.\\pipe\\slaveUdsDataProcess_" + guid;

			_pipe_write = Pipe.CreateNamedPipe(pipe_name, (uint) PipeOpenModeFlags.PIPE_ACCESS_OUTBOUND,
				(uint) (PipeModeFlags.PIPE_TYPE_BYTE), 1, 1024 * 1024, 512, 0, IntPtr.Zero);
			if (_pipe_write == Pipe.INVALID_HANDLE) {
				Debug.WriteLine("slaveUdsDataProcess create pipe fail.");
			}

			_pipe_read = Pipe.CreateFile( pipe_name, (uint)(DesireMode.GENERIC_READ), 0, IntPtr.Zero, 3, 128, IntPtr.Zero );
			if ( _pipe_read == Pipe.INVALID_HANDLE ) {
				Debug.WriteLine( "slaveUdsDataProcess open pipe fail." );
			}

			 if ( _pipe_write != Pipe.INVALID_HANDLE && _pipe_read != Pipe.INVALID_HANDLE ) {
				 //var native_overlapped = new NativeOverlapped();
				 bool bresult = Pipe.ConnectNamedPipe(_pipe_write, IntPtr.Zero);
			 }

			_stop_watch = Stopwatch.StartNew();
			_tmr = new System.Windows.Threading.DispatcherTimer() {Interval = new TimeSpan(0, 0, 0, 0, 50)};
			_tmr.Tick +=timer_Tick;
			_tmr.Start();

			_tmr2 = new System.Windows.Threading.DispatcherTimer() {Interval = new TimeSpan(0, 0, 2)};
			_tmr2.Tick += timer2_Tick;
			//_tmr2.Start();

		}

		private void init_did_sort_dicts() {
			var cmd_list = new List<sortableDidCmd> {
				new sortableDidCmdHwIdString(),
				new sortableDidCmdCanId(),
				new sortableDidCmdCanIdV2(),
				new sortableDidCmdSetCanIdV2(),
				new sortableDidCmdSwVerString()
			};
			cmd_list.ForEach(x => _did_sort_dicts.Add(x.pRespHead, x));
		}

		~slaveUdsDataProcess() {
			Dispose(false);
		}

		public void Dispose() {
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		private void Dispose( bool fDisposing ) {
			if (_disposed)
				return;

			if ( fDisposing ) {
			}

			if ( _pipe_read != Pipe.INVALID_HANDLE ) {
				Pipe.CloseHandle( _pipe_read );
				_pipe_read = Pipe.INVALID_HANDLE;
			}

			if ( _pipe_write != Pipe.INVALID_HANDLE ) {
				Pipe.CloseHandle( _pipe_write );
				_pipe_write = Pipe.INVALID_HANDLE;
			}

			_disposed = true;
		}

		#endregion

		private void timer2_Tick(object sender, EventArgs e) {
			foreach (var x in _did_sort_dicts.Values) {
				Trace.WriteLine(x);
			}
		}

		#region 管道接收端

		private void timer_Tick( object sender, EventArgs e ) {
			byte[] bytes = null;
			if (_pipe_read == Pipe.INVALID_HANDLE)
				goto end_of_process;

			int size = Marshal.SizeOf(typeof(UDSWrapper.App_uds_rx_buff_type));
			uint dwreaded = 0, dwavailspace = 0, dwusless = 0;

			if (!Pipe.PeekNamedPipe(_pipe_read, IntPtr.Zero, 0, ref dwreaded, ref dwavailspace, ref dwusless)) {
				Debug.WriteLine("peekNamedPipe fail");
				goto end_of_process;
			}

			if (dwavailspace < size) {
				//Debug.WriteLine("pipe read: no enough data to read");
				goto end_of_process;
			}

			bytes = new byte[size];
			//var native_overlapped = new NativeOverlapped();
			bool bresult = Pipe.ReadFile(_pipe_read, bytes, (uint) size, ref dwreaded, IntPtr.Zero);
			if (false == bresult || dwreaded < size ) {
				Debug.WriteLine("pipe ReadFile fail");
				goto end_of_process;
			}

			var rx_data = structBytesConv.bytesToStruct<UDSWrapper.App_uds_rx_buff_type>(bytes);
			var resp = new sortableDidResponse(rx_data.rxpduid, rx_data.buffer, rx_data.length);
			foreach (var x in _did_sort_dicts.Values) x.update(resp);

		end_of_process:
			long ellapsed = _stop_watch.ElapsedMilliseconds;
			foreach (var x in _did_sort_dicts.Values) 
				x.updateTimeOut(ellapsed);
			_stop_watch.Restart();

		}

		#endregion

		public List<byte[]> getAllSlaveCommands() {
			var list = _did_sort_dicts.Values.Select(x => x.cmd).ToList();
			return list;
		}

		public List<KeyValuePair<byte, string>> getSlaveResponseStrs(string key) {
			foreach ( var x in _did_sort_dicts.Values ) {
				if ( x.hint != key ) continue;
				return x.getRespStringList();
			}

			return new List<KeyValuePair<byte, string>>();
		}

		public List<sortableDidResponse> getSlaveResponse(string key) {
			foreach (var x in _did_sort_dicts.Values) {
				if (x.hint != key) continue;
				return x.respArray.ToList();
			}

			return new List<sortableDidResponse>();
		}

		public void clear() {
			foreach (var x in _did_sort_dicts.Values)
				x.clear();
		}

		public void clear(string key) {
			foreach (var x in _did_sort_dicts.Values) {
				if (x.hint != key) continue;
				x.clear();
				return;
			}
		}

		#region rx_indication_cbk

		public void rx_indication_cbk( ref UDSWrapper.App_uds_rx_buff_type appUdsRxBuff ) {
			dump_rx_indication_cbk(ref appUdsRxBuff);

			var found = false;
			if (_pipe_write == Pipe.INVALID_HANDLE) return;
			foreach (var x in _did_sort_dicts.Values) {
				if (!x.isThisCommandResp(appUdsRxBuff.buffer))
					continue;
				//dump_rx_indication_cbk(ref appUdsRxBuff);
				found = true;
				break;
			}

			if ( !found || !_tmr.IsEnabled )
				return;

			uint dwwritten = 0;
			//var native_overlapped = new NativeOverlapped();
			var bytes = structBytesConv.structToBytes(appUdsRxBuff);
			bool bresult = Pipe.WriteFile(_pipe_write, bytes, (uint) bytes.Length, ref dwwritten, IntPtr.Zero);

		}

		private static void dump_rx_indication_cbk(ref UDSWrapper.App_uds_rx_buff_type rxBuff) {
			var temp = new byte[rxBuff.length];
			Array.Copy(rxBuff.buffer, temp, temp.Length );
			Debug.WriteLine("net cbk len {0}, data {1}", rxBuff.length, BitConverter.ToString(temp));
		}

		#endregion

	}

	#region sortableDidResponse

	[Serializable]
	public sealed class sortableDidResponse : serializableCloneableBase<sortableDidResponse>
	{
		public byte rxPduId { get; private set; }
		public byte[] response { get; private set; }

		public long timeoutMs { get; set; }
		public long timeCounter { get; set; }
		public bool isTimeOut { get; private set; }

		public sortableDidResponse(byte rxPduId, byte[] resp, int len ) {
			if (null == resp || len < 0) len = 0;
			if (null != resp && resp.Length < len) len = resp.Length;

			this.rxPduId = rxPduId;
			this.response = new byte[len];
			if (null != resp) Array.Copy(resp, this.response, len);

			isTimeOut = false;
			timeoutMs = 600;
			timeCounter = 0;
		}

		public bool isNegativeResponse {
			get
			{
				if (null == response || response.Length <= 0) return true;
				if ( response[ 0 ] == sortableDidCmd.negativeResponseSid ) return true;
				return false;
			}
		}

		public void upateTimeOut(long ellapsedMs) {
			// TODO: prepare to process
		}

	}

	#endregion

	#region sortableDidCmd

	public abstract class sortableDidCmd
	{
		public byte[] cmd { get; private set; }
		public byte[] pRespHead { get; private set; }
		public sortableDidResponse[] respArray { get; private set; }
		public string hint { get; private set; }

		public const byte positiveResponseAddValue = 0x40;
		public const byte negativeResponseSid = 0x7f;

		protected sortableDidCmd(byte[] data, int offset, int len, string hint) {
			init_create(data, offset, len);
			this.hint = hint;
		}

		protected sortableDidCmd(UDSCommandBase cmd, string hint) {
			if (cmd == null) 
				throw new ArgumentException("无效命令数组");

			init_create(cmd.TxData, 0, cmd.TxData.Length);
			this.hint = hint;
		}

		protected void init_create(byte[] data, int offset, int len) {
			if (null == data || data.Length <= 0 || offset < 0 || len <= 0 || (offset + len) > data.Length)
				throw new ArgumentException("无效命令数组");

			this.cmd = new byte[ len ];
			this.pRespHead = new byte[ len ];
			Array.Copy( data, offset, this.cmd, 0, len );
			Array.Copy( data, offset, this.pRespHead, 0, len );
			this.pRespHead[ 0 ] += positiveResponseAddValue;

			respArray = new sortableDidResponse[ 0 ];
		}

		public virtual void update(sortableDidResponse resp) {
			if (null == resp) return;
			if (!isThisCommandResp(resp.response)) return;

			int pos = Array.FindIndex(respArray, x => x.rxPduId == resp.rxPduId);
			if (pos < 0) {
				var list = respArray.ToList();
				list.Add(resp.Clone());
				list.Sort((x, y) => x.rxPduId.CompareTo(y.rxPduId));
				respArray = list.ToArray();
			}
			else {
				respArray[pos] = resp.Clone();
			}
		}

		public virtual void updateTimeOut( long ellapsedMs ) {
			foreach (var x in respArray)
				x.upateTimeOut(ellapsedMs);
		}

		public virtual void clear() {
			respArray = new sortableDidResponse[0];
		}

		public virtual bool isThisCommandResp(byte[] resp) {
			if (null == resp || resp.Length < pRespHead.Length)
				return false;

			var pos = 0;
			return pRespHead.All(x => resp[pos++].Equals(x));
		}

		protected virtual byte[] get_trimed_rx_buff(sortableDidResponse resp) {
			if (null == resp || resp.response.Length <= pRespHead.Length) return new byte[0];
			if ( resp.isNegativeResponse ) return resp.response;

			var temp2 = new byte[resp.response.Length - pRespHead.Length];
			Array.Copy(resp.response, pRespHead.Length, temp2, 0, resp.response.Length - pRespHead.Length);
			return temp2;
		}

		public abstract List<KeyValuePair<byte, string>> getRespStringList();
		public override string ToString() {
			var sb = new StringBuilder(hint + ": ");
			var list = getRespStringList();
			list.ForEach(x => sb.AppendFormat("<{0}, {1}> ", x.Key, x.Value));
			return sb.ToString();
		}

		#region 工具函数

		public static bool isSubArrayEqual<T>( T[] source, T[] compare, int start ) where T : IEquatable<T> {
			if ( compare.Length > source.Length - start ) return false;
			return compare.All(t => source[start++].Equals(t));
		}

		#endregion

	}

	#endregion

	#region sortableDidCmdHwIdString

	public class sortableDidCmdHwIdString : sortableDidCmd
	{
		public sortableDidCmdHwIdString() : base(new UDSDIDBCUHWIDString(), "HWID") {	}

		public override List<KeyValuePair<byte, string>> getRespStringList() {
			var list = new List<KeyValuePair<byte, string>>();
			foreach (var x in respArray) {
				if (!x.isNegativeResponse) {
					var temp = get_trimed_rx_buff(x);
					var ba = new udsByteArray(temp, 0, temp.Length);
					list.Add(new KeyValuePair<byte, string>(x.rxPduId, ba.ValueString));
				}
				else {
					list.Add(new KeyValuePair<byte, string>(x.rxPduId, udsByteArray.InstanceNA().ValueString));
				}
			}

			return list;
		}
	}

	#endregion

	#region sortableDidCmdCanId

	public class sortableDidCmdCanId : sortableDidCmd
	{
		public sortableDidCmdCanId() : base(new UDSDIDQueryCanId(), "CanId") {
		}

		public override List<KeyValuePair<byte, string>> getRespStringList() {
			return (from x in respArray
				let temp = get_trimed_rx_buff(x)
				where !x.isNegativeResponse && temp.Length > 0
				let id = temp[0]
				select new KeyValuePair<byte, string>(x.rxPduId, ((int) id).ToString())).ToList();
		}
	}

	#endregion

	#region sortableDidCmdCanIdV2

	public class sortableDidCmdCanIdV2 : sortableDidCmd
	{
		public sortableDidCmdCanIdV2()
			: base( new UDSDIDQueryCanIdV2(), "CanIdV2" ) {
		}

		public override List<KeyValuePair<byte, string>> getRespStringList() {
			return (from x in respArray
				let temp = get_trimed_rx_buff(x)
				where !x.isNegativeResponse && temp.Length >= 2
				let crc = new udsUint16NoInv2(temp, 0)
				select new KeyValuePair<byte, string>(x.rxPduId, crc.value.ToString())).ToList();
		}
	}

	#endregion

	#region sortableDidCmdSetCanIdV2

	public class sortableDidCmdSetCanIdV2 : sortableDidCmd
	{
		private static readonly byte[] _cmd_head_data;
		static sortableDidCmdSetCanIdV2() {
			var temp = new UDSDIDSetCanIdV2(0, 0);
			_cmd_head_data = new byte[3];
			Array.Copy(temp.TxData, _cmd_head_data, 3);
		}

		public sortableDidCmdSetCanIdV2()
			: base( _cmd_head_data, 0, _cmd_head_data.Length, "SetCanIdV2" ) {
		}

		public override List<KeyValuePair<byte, string>> getRespStringList() {
			return (from x in respArray
				where !x.isNegativeResponse
				select new KeyValuePair<byte, string>(x.rxPduId, string.Empty)).ToList();
		}
	}

	#endregion

	#region sortableDidCmdSwVerString

	public class sortableDidCmdSwVerString : sortableDidCmd
	{
		public sortableDidCmdSwVerString()
			: base( new UDSDIDBCUSWVersionString(), "SWVersion" ) {
		}

		public override List<KeyValuePair<byte, string>> getRespStringList() {
			var list = new List<KeyValuePair<byte, string>>();
			foreach ( var x in respArray ) {
				if ( !x.isNegativeResponse ) {
					var temp = get_trimed_rx_buff( x );
					var ba = new udsString( temp, 0, temp.Length );
					list.Add( new KeyValuePair<byte, string>( x.rxPduId, ba.ValueString ) );
				}
				else {
					list.Add( new KeyValuePair<byte, string>( x.rxPduId, udsString.InstanceNA().ValueString ) );
				}
			}

			return list;
		}
	}

	#endregion

	#region sortableDidCmdAllHlssStatus

	public class sortableDidCmdAllHlssStatus : sortableDidCmd
	{
		public sortableDidCmdAllHlssStatus()
			: base(new UDSDIDGetAllHLSSStatus(), "AllHlssStatus") {
		}

		public override List<KeyValuePair<byte, string>> getRespStringList() {
			var list = new List<KeyValuePair<byte, string>>();
			foreach (var x in respArray) {
				if (!x.isNegativeResponse) {
					var temp = BitConverter.ToString(x.response);
					list.Add(new KeyValuePair<byte, string>(x.rxPduId, temp));
				}
				else {
					list.Add(new KeyValuePair<byte, string>(x.rxPduId, udsString.InstanceNA().ValueString));
				}
			}

			return list;
		}
	}

	#endregion

	#region structBytesConv

	public class structBytesConv
	{
		public static byte[] structToBytes( object structObj ) {
			int size = Marshal.SizeOf(structObj);
			var buffer = Marshal.AllocHGlobal(size);
			try {
				Marshal.StructureToPtr(structObj, buffer, false);
				var bytes = new byte[size];
				Marshal.Copy(buffer, bytes, 0, size);
				return bytes;
			} finally {
				Marshal.FreeHGlobal( buffer );
			}
		}

		public static T bytesToStruct<T>( byte[] bytes ) {
			int size = Marshal.SizeOf(typeof(T));
			var buffer = Marshal.AllocHGlobal(size);
			try {
				Marshal.Copy(bytes, 0, buffer, size);
				return (T) Marshal.PtrToStructure(buffer, typeof(T));
			} finally {
				Marshal.FreeHGlobal( buffer );
			}
		}

	}

	#endregion

	#region  serializableCloneableBase

	// http://blog.csdn.net/yapingxin/article/details/12754015

	public interface ICloneable<T> : ICloneable
	{
		new T Clone();
	}

	[Serializable]
	public class serializableCloneableBase<T> : ICloneable<T> where T : serializableCloneableBase<T>
	{
		public T Clone() {
			var stream = new MemoryStream();
			var formatter = new BinaryFormatter();
			formatter.Serialize( stream, this );
			stream.Position = 0;
			return formatter.Deserialize( stream ) as T;
		}

		object ICloneable.Clone() {
			return this.Clone();
		}
	}

	#endregion

}
