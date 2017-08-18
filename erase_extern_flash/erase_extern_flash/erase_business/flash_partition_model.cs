using models.config_base;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using uds_comm;

namespace models.flash_partition_model
{

	#region 页面后额外存储空间  NandOobFreeType

	[JsonObject( MemberSerialization.OptIn )]
	public class NandOobFreeType
	{
#pragma warning disable 0649
		[JsonProperty( PropertyName = "dt_offset" )]
		internal byte _dt_offset;		// 相对于Page 2048字节开始的偏移量
		[JsonProperty( PropertyName = "dt_length" )]
		internal byte _dt_length;

		[JsonProperty( PropertyName = "check_offset" )]
		internal byte _check_offset;		// 相对于Page 2048字节开始的偏移量
		[JsonProperty( PropertyName = "check_length" )]
		internal byte _check_length;
#pragma warning restore 0649

		public byte dateTimeOffset { get { return _dt_offset; } }
		public byte dateTimeLength { get { return _dt_length; } }

		public byte checkOffset { get { return _check_offset; } }
		public byte checkLength { get { return _check_length; } }
	}

	#endregion

	[JsonObject( MemberSerialization.OptIn )]
	public class flashPartitionModel : JsonConfig_Base
	{

		#region 单例模式（非线程安全）

		private static volatile flashPartitionModel _instance = null;
		private static readonly object syslock = new object();
		public static flashPartitionModel getInstance() {
			if ( _instance == null ) {
				/*lock ( syslock ) {
					if ( _instance == null )
						_instance = new FlashPartitionModel(); } */
				_instance = flashPartitionModel.createFromFile(AppDomain.CurrentDomain.SetupInformation.ApplicationBase +
																@"nand_flash_model\flash_model.json");
			}
			return _instance;
		}

		#endregion

		protected flashPartitionModel() {
			_oob_free_type_data = new List<NandOobFreeType>();
			sortTable = new List<FlashSortIndex>();
			sortSortTable = new List<int>();
		}

		public static flashPartitionModel createFromFile( string filePath ) {
			return read_from_file<flashPartitionModel>( filePath );
		}

		protected uint get_page_start_address( ushort blockId , ushort pageIdInBlock ) {
			long addr64 = blockId * blockSizeInBytes + pageIdInBlock * bytesPerPageAll;
			return (uint)addr64;
		}

		public uint getAddress( ushort blockId , ushort pageIdInBlock ) {
			return get_page_start_address( blockId , pageIdInBlock );
		}

		public uint getAddress( ushort blockId , ushort pageIdInBlock , ushort sectionIdInPage ) {
			return get_page_start_address( blockId , pageIdInBlock ) + (uint)(bytesPerSection * sectionIdInPage);
		}

		public uint getPageSpareStartAddress( ushort blockId , ushort pageId ) {
			return get_page_start_address( blockId , pageId ) + _bytes_per_page;
		}

		public uint getSectionSpareStartAddress( ushort blockId , ushort pageId , ushort sectionId ) {
			return (uint)(getPageSpareStartAddress( blockId , pageId ) + (sectionId * bytesPerSectionSpare));
		}

		public ushort getRemainSizeOfPageFromSection( FlashSortIndex sortIdx ) {
			return (ushort)(_bytes_per_page - (bytesPerSection * sortIdx.sid));
		}

		public int getFlashSortIndexDelta( FlashSortIndex next , FlashSortIndex first ) {
			int all_section_num = _blocks_per_chip * _pages_per_block * _section_num_per_page;
			int id_next = next.bid * _pages_per_block * _section_num_per_page;
			id_next += next.pid * _section_num_per_page + next.sid;

			int id_first = first.bid * _pages_per_block * _section_num_per_page;
			id_first += first.pid * _section_num_per_page + first.sid;

			int delta = (id_next - id_first + all_section_num) % all_section_num;
			return delta;
		}

		public bool isValidFlashSortIndex( FlashSortIndex sortIdx ) {
			if ( null == sortIdx ) return false;
			if ( sortIdx.bid >= _blocks_per_chip ) return false;
			if ( sortIdx.pid >= _pages_per_block ) return false;
			if ( sortIdx.sid >= _section_num_per_page ) return false;
			return true;
		}

		public bool isValidSectionId( ushort id ) {
			return id < _section_num_per_page;
		}

		[OnDeserialized]
		internal void OnDeserializedMethod( StreamingContext context ) {
			int size = _pages_per_block * bytesPerPageAll;
			blockSizeInBytes = (uint)size;
			build_sort_table();
		}

#pragma warning disable 0649
		[JsonProperty( PropertyName = "chip_id" )]
		private uint _chip_id;
		[JsonProperty( PropertyName = "chip_name" )]
		private string _chip_name;
		[JsonProperty( PropertyName = "blocks_per_chip" )]
		private ushort _blocks_per_chip;

		[JsonProperty( PropertyName = "storage_blocks_start_id" )]
		private ushort _storage_blocks_start_id;

		public uint chipId { get { return _chip_id; } }
		public string chipName { get { return _chip_name; } }
		public ushort blocksPerChip { get { return _blocks_per_chip; } }
		public ushort storageBlocksStartId { get { return _storage_blocks_start_id; } }

		[JsonProperty( PropertyName = "pages_per_block" )]
		private ushort _pages_per_block;
		[JsonProperty( PropertyName = "bytes_per_page" )]
		private ushort _bytes_per_page;
		[JsonProperty( PropertyName = "bytes_per_page_spare" )]
		private ushort _bytes_per_page_spare;

		[JsonProperty( PropertyName = "section_num_per_page" )]
		private ushort _section_num_per_page;
#pragma warning restore 0649

		public ushort pagesPerBlock { get { return _pages_per_block; } }
		public ushort bytesPerPage { get { return _bytes_per_page; } }
		public ushort bytesPerPageSpare { get { return _bytes_per_page_spare; } }
		public ushort bytesPerPageAll { get { return (ushort)(_bytes_per_page + _bytes_per_page_spare); } }
		public ushort bytesPerSection { get { return (ushort)(_bytes_per_page / _section_num_per_page); } }
		public ushort bytesPerSectionSpare { get { return (ushort)(_bytes_per_page_spare / _section_num_per_page); } }
		public ushort sectionsPerPage { get { return _section_num_per_page; } }

		[JsonProperty( PropertyName = "oob_free_type_data" )]
		private List<NandOobFreeType> _oob_free_type_data;
		public List<NandOobFreeType> oobFreeTypeData { get { return _oob_free_type_data; } }

		public uint getDateTimeAddress( ushort blockId , ushort pageIdInBlock , ushort sectionIdInPage ) {
			uint addr32 = get_page_start_address( blockId , pageIdInBlock ) + bytesPerPage;
			addr32 += _oob_free_type_data[sectionIdInPage].dateTimeOffset;
			return addr32;
		}


		public uint blockSizeInBytes { get; private set; }

		public List<FlashSortIndex> sortTable { get; private set; }
		public List<int> sortSortTable { get; private set; }

		#region 获取二级索引表子表 buildSubSortSortTable

		public List<int> buildSubSortSortTable( int leftId , int rightId ) {
			var sort_table = new List<int>();
			if ( leftId < sortSortTable[0] || leftId > sortSortTable[sortSortTable.Count - 1] ) return sort_table;
			if ( rightId < sortSortTable[0] || rightId > sortSortTable[sortSortTable.Count - 1] ) return sort_table;
			int left = sortSortTable.IndexOf( leftId ) , right = sortSortTable.IndexOf( rightId );
			if ( left < 0 || right < 0 ) return sort_table;

			if ( left > right ) {
				sort_table.AddRange( sortSortTable.GetRange( left , sortSortTable.Count - left ) );
				sort_table.AddRange( sortSortTable.GetRange( 0 , right ) );
			}
			else {
				sort_table.AddRange( sortSortTable.GetRange( left , right - left + 1 ) );
			}

			return sort_table;
		}

		#endregion

		#region 索引转换 flashSortIndexToLineSectionId  lineSectionIdToFlashSortIndex

		public FlashSortIndex lineSectionIdToFlashSortIndex( int lineSectionId ) {
			int block_id = lineSectionId / (_pages_per_block * _section_num_per_page);
			int remain = lineSectionId % (_pages_per_block * _section_num_per_page);
			int page_id = remain / _section_num_per_page;
			int section_id = remain % _section_num_per_page;
			if ( block_id >= _blocks_per_chip ) return null;
			if ( page_id >= _pages_per_block ) return null;
			if ( section_id >= _section_num_per_page ) return null;
			return new FlashSortIndex( (ushort)block_id , (ushort)page_id , (ushort)section_id );
		}

		public int flashSortIndexToLineSectionId( FlashSortIndex fsId ) {
			if (null == fsId || !isValidFlashSortIndex(fsId)) return 0;
			int id = _pages_per_block * _section_num_per_page * fsId.bid;
			id += _section_num_per_page * fsId.pid + fsId.sid;
			return id;
		}

		#endregion

		public void print_sort_sort_table() {
			foreach ( var x in sortSortTable ) Debug.Write( string.Format("{0}, ", x) );
			Debug.WriteLine( "" );
		}

		#region 工具： build_sort_table

		private void build_sort_table() {
			sortTable.Clear();
			for ( ushort bid = 0; bid < _blocks_per_chip; bid++ ) {
				for ( ushort pid = 0; pid < _pages_per_block; pid++ ) {
					for ( ushort sid = 0; sid < _section_num_per_page; sid++ )
						sortTable.Add( new FlashSortIndex( bid , pid , sid ) );
				}
			}

			sortSortTable.Clear();
			int start_section_id = _storage_blocks_start_id * _pages_per_block * _section_num_per_page;
			sortSortTable = Enumerable.Range( start_section_id , sortTable.Count - start_section_id ).ToList();
		}

		#endregion

	}

	#region 页面/扇区索引 FlashSortIndex
#pragma warning disable 0660, 0661

	public class FlashSortIndex
	{
		private readonly ushort _bid;
		private readonly ushort _pid;
		private readonly ushort _sid;
		public ushort bid { get { return _bid; } }
		public ushort pid { get { return _pid; } }
		public ushort sid { get { return _sid; } }

		public override string ToString() {
			return string.Format("FlashSortIndex " + "<{0}, {1}, {2}>", _bid, _pid, _sid);
		}

		public FlashSortIndex( ushort bid , ushort pid , ushort sid ) {
			_bid = bid;
			_pid = pid;
			_sid = sid;
		}

		public FlashSortIndex( FlashSortIndex idx ) {
			_bid = idx._bid;
			_pid = idx._pid;
			_sid = idx._sid;
		}

		public static FlashSortIndex operator +( FlashSortIndex fsid , ushort sectionDelta ) {
			return new FlashSortIndex( fsid.bid , fsid.pid , (ushort)(fsid.sid + sectionDelta) );
		}

		public static bool operator ==( FlashSortIndex f1 , FlashSortIndex f2 ) {
			if ( Equals( f1 , null ) || Equals( f2 , null ) ) return false;
			return f1._bid == f2._bid && f1._pid == f2._pid && f1._sid == f2._sid;
		}

		public static bool operator !=( FlashSortIndex f1 , FlashSortIndex f2 ) {
			if ( Equals( f1 , null ) || Equals( f2 , null ) ) return true;
			return f1._bid != f2._bid || f1._pid != f2._pid || f1._sid != f2._sid;
		}

	}

#pragma warning restore 0660, 0661
	#endregion

	#region 数据结构 FlashPageSpareData

	public class FlashPageSpareData
	{
		private readonly List<NandOobFreeType> _oob_free_type_data;
		private readonly ushort[] _reboot_id_list;
		private readonly uint[] _dt32_list;
		private ushort _blk_id , _page_id;
		private readonly byte[] _buffer;

		public FlashPageSpareData( flashPartitionModel model , ushort blkId , ushort pageId ) {
			_oob_free_type_data = model.oobFreeTypeData.Copy();
			_reboot_id_list = new ushort[_oob_free_type_data.Count];
			_dt32_list = new uint[_oob_free_type_data.Count];
			_buffer = new byte[model.bytesPerPageSpare];
			_blk_id = blkId; _page_id = pageId;
		}

		public void fillData( byte[] buffer , int pos ) {
			int len = ((buffer.Length - pos) > _buffer.Length) ? _buffer.Length : (buffer.Length - pos);
			Array.Copy( buffer , pos , _buffer , 0 , len );

			for ( int i = 0; i < _reboot_id_list.Length; i++ ) {
				int offset = _oob_free_type_data[i].checkOffset;
				int len2 = _oob_free_type_data[i].checkLength;
				var temp = new byte[len2];
				Array.Copy( _buffer , offset , temp , 0 , len2 ); Array.Reverse( temp );
				_reboot_id_list[i] = BitConverter.ToUInt16( temp , 0 );

				offset = _oob_free_type_data[i].dateTimeOffset;
				len2 = _oob_free_type_data[i].dateTimeLength;
				temp = new byte[len2];
				Array.Copy( _buffer , offset , temp , 0 , len2 ); Array.Reverse( temp );
				_dt32_list[i] = BitConverter.ToUInt32( temp , 0 );
			}
		}

		public uint getDateTime32( int id ) {
			if ( id >= 0 && id < _dt32_list.Length )
				return _dt32_list[id];
			return 0;
		}

		public DateTime getDateTime( int id ) {
			uint dt32 = getDateTime32( id );
			return udsTime1.convert_dt32_to_datetime( dt32 );
		}

		public ushort getRebootId( int id ) {
			if ( id >= 0 && id < _reboot_id_list.Length )
				return _reboot_id_list[id];
			return 0;
		}

	}

	#endregion

}
