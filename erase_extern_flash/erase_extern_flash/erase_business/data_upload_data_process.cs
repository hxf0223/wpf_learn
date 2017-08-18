using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

using uds_comm;
using models.flash_partition_model;
using modules.data_upload.upload_worker;

namespace modules.data_upload.data_process
{
	public class dataUploadDataProcess : IDisposable
	{
		private readonly BackgroundWorker _bw;
		private readonly UDSComm _uds_comm;
		private readonly flashPartitionModel _model;
		private readonly dataUploadWorker _read_worker;

		private bool _disposed;

		#region 构造及析构

		public dataUploadDataProcess( UDSComm udsComm , BackgroundWorker bw ) {
			_uds_comm = udsComm; _bw = bw;
			_model = flashPartitionModel.getInstance();
			_read_worker = new dataUploadWorker(udsComm, bw);
		}

		~dataUploadDataProcess() {
			Dispose(false);
		}

		public void Dispose() {
			this.Dispose( true );
			GC.SuppressFinalize( this );
		}

		protected virtual void Dispose( bool disposing ) {
			if ( !_disposed ) {
				if ( disposing ) {
					//closeDumpFile();
				}

				_disposed = true;
			}
		}

		#endregion

		#region 读取存储在块第一个扇区上的时间信息  readAllBlockHeadDateTime

		public List<KeyValuePair<FlashSortIndex , uint>> readAllBlockHeadDateTime() {
			var dt_list = new List<KeyValuePair<FlashSortIndex , uint>>();
			ushort start_bid = _model.storageBlocksStartId;
			var sort_table = (from fsid in _model.sortTable
				where fsid.pid == 0 && fsid.sid == 0 && fsid.bid >= start_bid
				select fsid).ToList();

			foreach ( var fsid in sort_table ) {
				var addr = _model.getAddress( fsid.bid , fsid.pid , fsid.sid );
				var buffer = _read_worker.qreadAddressData( addr , 4 );
				if ( null == buffer || buffer.Length < 4 ) {
					Debug.WriteLine( "readAllBlockHeadDateTime fail at {0}" , fsid );
					return null;
				}

				var temp = buffer.Reverse().ToArray();
				uint dt32 = BitConverter.ToUInt32( temp , 0 );
				dt_list.Add(new KeyValuePair<FlashSortIndex, uint>(fsid, dt32));
			}

			return dt_list;
		}

		public List<KeyValuePair<FlashSortIndex , uint>> readAllBlockHeadDateTime( int rangeMin , int rangeMax ) {
			var sort_list = _model.buildSubSortSortTable(rangeMin, rangeMax);
			var dt_list = new List<KeyValuePair<FlashSortIndex , uint>>();
			var fsid_list =
			(from x in sort_list
				let fsid = _model.lineSectionIdToFlashSortIndex(x)
				where fsid.pid == 0 && fsid.sid == 0
				select fsid).ToList();

			foreach (var fsid in fsid_list) {
				var addr = _model.getAddress( fsid.bid , fsid.pid , fsid.sid );
				var buffer = _read_worker.qreadAddressData( addr , 4 );
				if ( null == buffer || buffer.Length < 4 ) {
					Debug.WriteLine( "readAllBlockHeadDateTime fail at {0}" , fsid );
					return null;
				}

				var temp = buffer.Reverse().ToArray();
				var dt32 = BitConverter.ToUInt32( temp , 0 );
				dt_list.Add(new KeyValuePair<FlashSortIndex, uint>(fsid, dt32));
			}

			return dt_list;
		}

		#endregion
		

		#region 读取扇区数据  readSectionData

		public List<flashSectionData> readSectionData( FlashSortIndex fsId ) {
			var datalist = new List<flashSectionData>();
			if ( false == _model.isValidFlashSortIndex( fsId ) ) return datalist;

			uint addr = _model.getAddress( fsId.bid , fsId.pid , fsId.sid );
			var buffer = _read_worker.qreadAddressData( addr , _model.bytesPerSection );
			if ( null == buffer || buffer.Length < _model.bytesPerSection ) return datalist;

			var fsdata = new flashSectionData( _model , fsId );
			fsdata.fill_data( buffer , 0 );

			addr = _model.getSectionSpareStartAddress( fsId.bid , fsId.pid , fsId.sid );
			buffer = _read_worker.qreadAddressData( addr , _model.bytesPerSectionSpare );
			if ( null == buffer || buffer.Length < _model.bytesPerSectionSpare ) return datalist;

			fsdata.fill_spare_data( buffer , 0 );
			datalist.Add( fsdata );

			Debug.WriteLine( fsdata );
			return datalist;
		}

		#endregion

		#region 读取页面数据  readPageData

		public List<flashSectionData> readPageData( List<int> seqList , ref int posOfList ) {
			if ( null == seqList || posOfList >= seqList.Count ) {
				Debug.WriteLine( "DataUploadDataProcess readPageData exceed range." );
				return new List<flashSectionData>();
			}

			var fsid_read = _model.sortTable[seqList[posOfList]];
			uint addr = _model.getAddress(fsid_read.bid, fsid_read.pid, fsid_read.sid);
			var len = (ushort)(_model.getRemainSizeOfPageFromSection( fsid_read ) + _model.bytesPerPageSpare);
			var buffer = _read_worker.qreadAddressData(addr, len);

			if ( null == buffer || buffer.Length < len ) {
				Debug.WriteLine( "DataUploadDataProcess readPageData fail...." );
				var fake_read_num = (ushort)(_model.sectionsPerPage - fsid_read.sid);
				posOfList += fake_read_num;
				return new List<flashSectionData>();
			}

			var sections_read_num = (ushort)(_model.sectionsPerPage - fsid_read.sid);
			var section_data_arr = new flashSectionData[sections_read_num];
			for ( ushort id = 0; id < sections_read_num; id++ )
				section_data_arr[id] = new flashSectionData( _model , fsid_read + id );

			int buffer_pos = 0;
			for ( ushort id = 0; id < sections_read_num; id++ ) {
				buffer_pos += section_data_arr[id].fill_data(buffer, buffer_pos);
			}

			buffer_pos += fsid_read.sid * _model.bytesPerSectionSpare;
			for ( ushort id = 0; id < sections_read_num; id++ ) {
				buffer_pos += section_data_arr[id].fill_spare_data(buffer, buffer_pos);
			}

			for ( ushort id = 0; id < sections_read_num; id++ ) {
				//Debug.WriteLine( section_data_arr[id] );
			}

			posOfList += sections_read_num;
			return section_data_arr.ToList();
		}

		public List<flashSectionData> readPageData( FlashSortIndex fsId , ushort startSectionId , ushort endSectionId ) {
			var datalist = new List<flashSectionData>();
			if ( false == _model.isValidFlashSortIndex( fsId ) ) return datalist;
			if ( startSectionId > endSectionId ) return datalist;
			if ( false == _model.isValidSectionId( startSectionId ) || false == _model.isValidSectionId( endSectionId ) )
				return datalist;

			fsId = new FlashSortIndex(fsId.bid, fsId.pid, 0);
			var addr = _model.getAddress(fsId.bid, fsId.pid);
			var len = (ushort)(_model.bytesPerPage + _model.bytesPerPageSpare);
			var buffer = _read_worker.qreadAddressData(addr, len);

			if ( null == buffer || buffer.Length < len ) return datalist;

			int buffer_pos = 0;
			int spare_buff_pos = _model.bytesPerPage;
			for ( ushort i = 0; i < startSectionId; i++ ) {
				buffer_pos += _model.bytesPerSection;
				spare_buff_pos += _model.bytesPerSectionSpare;
			}

			for ( var i = startSectionId; i <= endSectionId; i++ ) {
				var sdata = new flashSectionData(_model, fsId + i);
				buffer_pos += sdata.fill_data(buffer, buffer_pos);
				spare_buff_pos += sdata.fill_spare_data(buffer, spare_buff_pos);
				datalist.Add(sdata);
			}

			foreach (var x in datalist) {
				Debug.WriteLine(x);
			}

			return datalist;

		}

		public List<flashSectionData> readPageData(FlashSortIndex fsId, ushort startSectionId, ushort endSectionId,
			int retry) {
			for (int i = 0; i < retry; i++) {
				var list = readPageData(fsId, startSectionId, endSectionId);
				if (list.Count > 0) return list;
			}

			return new List<flashSectionData>();
		}

		public List<flashSectionData> readPageData( FlashSortIndex fsId ) {
			return readPageData( fsId , 0 , 0 );
		}

		#endregion

	}

	#region 扇区数据定义 flashSectionData

	public class flashSectionData
	{
		protected FlashSortIndex _sort_id;
		protected byte[] _buffer , _buff_spare;
		protected byte _spare_dt_offset , _spare_check_offset;
		protected byte _spare_dt_len , _spare_check_len;
		private const int _block_first_section_ts_size = 4;

		public flashSectionData( flashPartitionModel model , FlashSortIndex sortId ) {
			_buffer = new byte[model.bytesPerSection];
			_buff_spare = new byte[model.bytesPerPageSpare / model.sectionsPerPage];

			_spare_dt_offset = model.oobFreeTypeData[0].dateTimeOffset;
			_spare_dt_len = model.oobFreeTypeData[0].dateTimeLength;
			_spare_check_offset = model.oobFreeTypeData[0].checkOffset;
			_spare_check_len = model.oobFreeTypeData[0].checkLength;

			_sort_id = sortId;
		}

		public override string ToString() {
			string nl = Environment.NewLine;
			return string.Format("FlashSectionData: {0}{1}data: {2}{3}spare data: {4}",
				_sort_id, nl, BitConverter.ToString(_buffer), nl, BitConverter.ToString(_buff_spare));
		}

		public FlashSortIndex flashSortId {
			get { return _sort_id; }
		}

		#region 填充数据 fill_data   fill_spare_data

		internal int fill_data( byte[] source , int pos ) {
			Array.Copy( source , pos , _buffer , 0 , _buffer.Length );
			return _buffer.Length;
		}

		internal int fill_spare_data( byte[] source , int pos ) {
			Array.Copy( source , pos , _buff_spare , 0 , _buff_spare.Length );
			return _buff_spare.Length;
		}

		#endregion

		#region 获取数据 getData  getOriginalData

		public byte[] getData() {
			if (_sort_id.pid != 0 || _sort_id.sid != 0) {
				var temp = new byte[_buffer.Length - 1];
				Array.Copy(_buffer, temp, temp.Length);
				return temp;
			}
			else {
				var temp = new byte[_buffer.Length - 1 - _block_first_section_ts_size];
				Array.Copy(_buffer, _block_first_section_ts_size, temp, 0, temp.Length);
				return temp;
			}
		}

		public byte[] getOriginalData() {
			return _buffer;
		}

		#endregion

		#region 从附加Flash中获取属性   sectionDateTime32   rebootId

		public uint sectionDateTime32 {
			get {
				var temp = new byte[_spare_dt_len];
				Array.Copy( _buff_spare , _spare_dt_offset , temp , 0 , _spare_dt_len );
				var dt32 = Convert.ToUInt32( temp.Reverse().ToArray() );
				return dt32;
			}
		}

		public ushort rebootId {
			get {
				var temp = new byte[_spare_check_len];
				Array.Copy( _buff_spare , _spare_check_offset , temp , 0 , _spare_check_len );
				return temp[0];
			}
		}

		#endregion

	}

	#endregion

}
