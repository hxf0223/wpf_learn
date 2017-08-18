using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace s19_process.rw_length_converter
{
	#region 烧写传输参数定义：S19UnitMemSizeValueEnum
	public enum S19UnitMemSizeValueEnum : ushort
	{
		size_byte = 1 ,
		size_256 = 256 ,
		size_512 = 512 ,
		size_1k = 1024 ,
		size_2k = 2048
	}
	#endregion

	public class s19RwLengthConverter
	{
		#region 烧写传输参数对应关系表 _mem_size_type_map

		private const byte typeOffset = 12;
		private const ushort typeMask = 0xF000;
		private const ushort sizeMask = 0x0FFF;

		private static readonly Dictionary<S19UnitMemSizeValueEnum , byte> _mem_size_type_map = new Dictionary<S19UnitMemSizeValueEnum , byte>() {
			{ S19UnitMemSizeValueEnum.size_byte, 0x00 },
			{ S19UnitMemSizeValueEnum.size_256, 0x01 },
			{ S19UnitMemSizeValueEnum.size_512, 0x02 },
			{ S19UnitMemSizeValueEnum.size_1k, 0x03 },
			{ S19UnitMemSizeValueEnum.size_2k, 0x04 }
		};

		#endregion

		public s19RwLengthConverter( S19UnitMemSizeValueEnum sizeUnitValue ) {
			Debug.Assert( Enum.IsDefined( typeof( S19UnitMemSizeValueEnum ) , sizeUnitValue ) );
			unitMemSizeValue = sizeUnitValue;
			memSizeType = _mem_size_type_map[unitMemSizeValue];
		}

		public S19UnitMemSizeValueEnum unitMemSizeValue { get; private set; }
		public int unitMemSizeValueInt { get { return (int)unitMemSizeValue; } }
		public byte memSizeType { get; private set; }

		public ushort convert_mem_size( uint realSize ) {
			var size_unit_value = (int)unitMemSizeValue;
			var size_in_unit = (ushort)(realSize / size_unit_value);
			var newsize = (ushort)(size_in_unit & sizeMask);
			newsize |= (ushort)(memSizeType << typeOffset);

			return newsize;
		}

	}
}
