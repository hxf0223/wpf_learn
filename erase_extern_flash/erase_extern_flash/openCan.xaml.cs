using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using erase_extern_flash.can_dev_types;

namespace erase_extern_flash
{
	/// <summary>
	/// openCan.xaml 的交互逻辑
	/// </summary>
	public partial class openCan : Window
	{
		private readonly canDevViewModel _can_dev_model;

		public openCan() {
			InitializeComponent();
			_can_dev_model = new canDevViewModel();
			this.DataContext = _can_dev_model;
		}

		public void addCanDevType(string strDev, uint iType) {
			_can_dev_model.canDevTypeList.Add(new canDevType() {dev = strDev, type = iType});
		}

		public void setSelDevType( string strDev, uint iType ) {
			foreach (var x in _can_dev_model.canDevTypeList) {
				if (x.type != iType) continue;
				_can_dev_model.selCanType = x;
				break;
			}
		}

		public canDevType getSelDevType() {
			return _can_dev_model.selCanType;
		}

		private void BtnOK_OnClick(object sender, RoutedEventArgs e) {
			this.DialogResult = true;
		}

		private void BtnCancel_OnClick(object sender, RoutedEventArgs e) {
			this.DialogResult = false;
		}
	}

}
