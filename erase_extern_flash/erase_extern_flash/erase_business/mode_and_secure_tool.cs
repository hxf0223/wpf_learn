using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Text.RegularExpressions;
using uds_comm;

namespace modules.mode_and_secure_tool
{

	public class modeSecureTool
	{
		public static UDSDIDQueryBCUBasicState.HostRunModeEnum uds_query_bcu_run_mode( UDSComm udsComm , out bool bSuccess ) {		// normal, calibration
			var cmd = new UDSDIDQueryBCUBasicState();
			udsComm.TransmitIgnoreHeartFailFlag(cmd);
			Debug.WriteLine("uds_query_bcu_run_mode, rx data: " + cmd.RxDataString);
			bSuccess = cmd.isPositiveResponse;
			return cmd.getHostRunMode();
		}

		public static int normal_goto_ext_mode( UDSComm udsComm , BackgroundWorker bw ) {
			if ( null != bw && bw.CancellationPending ) 
				return 0;

			var cmd_ext = new UDSDefaultModeToDiagsysExtMode();
			udsComm.Transmit( cmd_ext );
			if ( false == cmd_ext.isSuccess() ) {
				Debug.WriteLine( "failed at " + get_my_method_name() );
				return -1;
			}

			return 0;
		}

		public static int query_seed( UDSComm udsComm , BackgroundWorker bw , out byte[] seedOut ) {
			seedOut = new byte[0];
			if ( null != bw && bw.CancellationPending ) 
				return 0;

			var cmd_seed = new UDSSecureAccessQuerySeed();
			udsComm.Transmit( cmd_seed );
			seedOut = cmd_seed.getSeed();

			if ( !cmd_seed.isSuccess() || seedOut == null ) {
				Debug.WriteLine( "failed at " + get_my_method_name() );
				return -1;
			}

			return 0;
		}

		public static int validate_key( UDSComm udsComm , BackgroundWorker bw , byte[] seedIn ) {
			if ( null != bw && bw.CancellationPending ) 
				return 0;

			var key_arr = udsSecure.udsSecure.CalculateKey_m( seedIn );
			var cmd = new UDSSecureSendKey( key_arr );
			udsComm.Transmit( cmd );
			if ( false == cmd.isSuccess() ) {
				Debug.WriteLine( "failed at " + get_my_method_name() );
				return -1;
			}

			return 0;
		}

		public static bool uds_ext_mode_goto_default_mode( UDSComm udsComm ) {
			var cmd = new UDSDiagsysextToDefaultMode();
			udsComm.Transmit( cmd );
			if ( false == cmd.isSuccess() ) {
				Debug.WriteLine( "failed at " + get_my_method_name() );
			}

			return cmd.isSuccess();
		}

		#region 调试工具 get_my_method_name

		[MethodImpl( MethodImplOptions.NoInlining )]
		public static string get_my_method_name() {
			var st = new StackTrace( new StackFrame( 1 ) );
			return st.GetFrame( 0 ).GetMethod().Name;
		}

		#endregion

	}

	public class parseVerFromInfo
	{
		private static readonly Regex _pattern = new Regex("[TR][0-9]+\\.[0-9]+(?:\\.[0-9]+)?");
		public static string getVersionFromSystemInfoString(string info) {
			var result = _pattern.Match(info).Value;
			if (result.Length <= 0) return result;
			return result.Substring(1);
		}
	}

}
