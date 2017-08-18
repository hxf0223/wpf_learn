using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace erase_extern_flash.can_dev_types
{
	public class canDevType
	{
		public string dev { get; set; }
		public uint type { get; set; }
		public override string ToString() {
			return string.Format("<{0} | {1}>", type, dev);
		}
	}
}
