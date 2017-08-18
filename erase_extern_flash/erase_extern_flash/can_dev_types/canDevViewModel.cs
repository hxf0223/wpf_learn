using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace erase_extern_flash.can_dev_types
{
	class canDevViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		public ObservableCollection<canDevType> canDevTypeList { get; set; }

		public canDevViewModel() {
			canDevTypeList = new ObservableCollection<canDevType>();
			_sel_can_type = null;
		}

		private canDevType _sel_can_type;
		public canDevType selCanType {
			get { return _sel_can_type; }
			set {
				_sel_can_type = value;
				//raisePropertyChanged("selCanType");
			}
		}

		private void raisePropertyChanged( string propertyName ) {
			if ( PropertyChanged != null ) {
				PropertyChanged( this, new PropertyChangedEventArgs( propertyName ) );
			}
		}   
	}
}
