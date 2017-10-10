using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using uds_comm;

namespace SlaveIdConfigNet2
{
	public class slaveIdConfigBroadcastStatePattern
	{

		#region stateEnum

		public enum stateEnum
		{
			[Description("Init")]
			init = 0,
			[Description("Broadcast query slave Ids")]
			broadcast_query_ids,
			[Description("Waiting for slave response")]
			wait_for_slave_response,
			[Description("Internal alloc Ids")]
			internal_alloc_ids,
			[Description("Internal set Ids")]
			internal_set_ids,
			[Description("Internal set ok")]
			internal_set_ok,
			[Description("Fail")]
			fail,
			[Description("Ok")]
			ok
		}

		#endregion

		#region 事件参数 slaveIdConfigEventArgs

		public class slaveIdConfigEventArgs : EventArgs {
			public stateEnum stt { get; internal set; }
			public bool enter { get; internal set; }
		}

		#endregion

		private readonly BackgroundWorker _bw;
		public event EventHandler onSlaveIdConfigStatusEvent;

		public slaveIdConfigBroadcastStateBase broadcastState { get; internal set; }
		public stateEnum stateResult { get; private set; }

		public slaveIdConfigBroadcastStatePattern(BackgroundWorker bw, UDSComm udsComm) {
			broadcastState = new slaveIdConfigBroadcastStateInit(udsComm, this);
			stateResult = broadcastState.state;
			_bw = bw;
		}

		~slaveIdConfigBroadcastStatePattern() {
			remove_all_progress_event_handler();
		}

		public int process() {
			if (null == broadcastState)
				return -1;

			broadcastState.process();
			if (null != broadcastState) {
				stateResult = broadcastState.state;
				return 0;
			}

			return -1;
		}

		internal bool is_worker_running() {
			if ( null == _bw) return true;
			return !(_bw.CancellationPending);
		}

		#region 移除所有事件订阅 remove_all_progress_event_handler

		public void remove_all_progress_event_handler() {
			if ( null == onSlaveIdConfigStatusEvent) return;
			var dary = onSlaveIdConfigStatusEvent.GetInvocationList();
			foreach ( var del in dary ) { onSlaveIdConfigStatusEvent -= del as EventHandler; }
		}

		#endregion

		#region 事件通知 status_notify

		internal void status_notify(stateEnum sttEnum, bool enterFlag) {
			try {
				if ( null == onSlaveIdConfigStatusEvent) return;
				var arg = new slaveIdConfigEventArgs() {stt = sttEnum, enter = enterFlag};
				onSlaveIdConfigStatusEvent.BeginInvoke(this, arg, null, null);
			} catch (Exception e) { Trace.WriteLine(e); }
		}

		#endregion

	}


	public abstract class slaveIdConfigBroadcastStateBase
	{
		protected readonly UDSComm _uds_comm;
		protected readonly slaveIdConfigBroadcastStatePattern.stateEnum _stt;
		public slaveIdConfigBroadcastStatePattern.stateEnum state { get { return _stt; } }
		protected slaveIdConfigBroadcastStatePattern pattern { get; set; }

		protected slaveIdConfigBroadcastStateBase(UDSComm udsComm, slaveIdConfigBroadcastStatePattern pattern, slaveIdConfigBroadcastStatePattern.stateEnum stt) {
			_uds_comm = udsComm;
			this.pattern = pattern;
			_stt = stt;
		}

		protected bool pattern_null_or_pattern_exit() {
			return (null == pattern || !pattern.is_worker_running());
		}

		protected abstract slaveIdConfigBroadcastStateBase process_imp();

		protected virtual void update_state(slaveIdConfigBroadcastStateBase next) {
			if (pattern == null) return;
			pattern.broadcastState = next;
		}

		protected virtual void state_enter_notify() {
			
		}

		protected virtual void state_leave_notify() {
			
		}

		public virtual void process() {
			state_enter_notify();
			var next = process_imp();
			state_leave_notify();		// send leave notify before change pattern's broadcastState
			update_state( next );
		}

	}

	public class slaveIdConfigBroadcastStateInit : slaveIdConfigBroadcastStateBase
	{
		public slaveIdConfigBroadcastStateInit(UDSComm udsComm, slaveIdConfigBroadcastStatePattern pattern) :
			base( udsComm, pattern, slaveIdConfigBroadcastStatePattern.stateEnum.init ) { }

		protected override slaveIdConfigBroadcastStateBase process_imp() {
			if ( pattern_null_or_pattern_exit() || null == _uds_comm ) 
				return new slaveIdConfigBroadcastStateFail( _uds_comm, pattern );

			return new slaveIdConfigBroadcastStateQueryIds(_uds_comm, pattern);
		}
	}

	public class slaveIdConfigBroadcastStateQueryIds : slaveIdConfigBroadcastStateBase
	{
		public slaveIdConfigBroadcastStateQueryIds(UDSComm udsComm, slaveIdConfigBroadcastStatePattern pattern) :
			base( udsComm, pattern, slaveIdConfigBroadcastStatePattern.stateEnum.broadcast_query_ids ) { }

		protected override slaveIdConfigBroadcastStateBase process_imp() {
			if (pattern_null_or_pattern_exit() || null == _uds_comm) 
				return new slaveIdConfigBroadcastStateFail(_uds_comm, pattern);

			var cmd = new UDSDIDQueryCanIdV2 { TimeOutMs = 200 };
			_uds_comm.TransmitIgnoreHeartFailFlag( cmd );
			return new slaveIdConfigBroadcastStateWaitForSlaveResponse(_uds_comm, pattern);
		}
	}

	public class slaveIdConfigBroadcastStateWaitForSlaveResponse : slaveIdConfigBroadcastStateBase
	{
		public slaveIdConfigBroadcastStateWaitForSlaveResponse(UDSComm udsComm, slaveIdConfigBroadcastStatePattern pattern) :
			base( udsComm, pattern, slaveIdConfigBroadcastStatePattern.stateEnum.wait_for_slave_response ) { }

		protected override slaveIdConfigBroadcastStateBase process_imp() {
			if (pattern_null_or_pattern_exit() || null == _uds_comm) 
				return new slaveIdConfigBroadcastStateFail(_uds_comm, pattern);

			Thread.Sleep(500);
			return new slaveIdConfigBroadcastStateOk(_uds_comm, pattern);
		}
	}


	public class slaveIdConfigBroadcastStateFail : slaveIdConfigBroadcastStateBase
	{
		public slaveIdConfigBroadcastStateFail(UDSComm udsComm, slaveIdConfigBroadcastStatePattern pattern) :
			base( udsComm, pattern, slaveIdConfigBroadcastStatePattern.stateEnum.fail ) { }

		protected override slaveIdConfigBroadcastStateBase process_imp() {
			return null;
		}

		protected override void state_leave_notify() {
			if ( null != pattern ) pattern.status_notify( _stt, false );
		}
	}

	public class slaveIdConfigBroadcastStateOk : slaveIdConfigBroadcastStateBase
	{
		public slaveIdConfigBroadcastStateOk(UDSComm udsComm, slaveIdConfigBroadcastStatePattern pattern) :
			base( udsComm, pattern, slaveIdConfigBroadcastStatePattern.stateEnum.ok ) { }

		protected override slaveIdConfigBroadcastStateBase process_imp() {
			return null;
		}

		protected override void state_leave_notify() {
			if (null != pattern) pattern.status_notify(_stt, false);
		}
	}

}
