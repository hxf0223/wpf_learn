using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SlaveIdConfigNet2Wpf.viewModel;

namespace SlaveIdConfigNet2Wpf
{
	/// <summary>
	/// MainWindow.xaml 的交互逻辑
	/// </summary>
	public partial class MainWindow : Window
	{
		private canRxConfigFile _can_rx_config_file;
		private bmuCollectionViewModel _bmu_collection_vm;

		public MainWindow() {
			InitializeComponent();
			_can_rx_config_file = new canRxConfigFile();
			_bmu_collection_vm = new bmuCollectionViewModel(_can_rx_config_file.tpIfList);
			lbMain.ItemsSource = _bmu_collection_vm.bmuList;
			this.DataContext = _bmu_collection_vm;

			string app_title = Properties.Resources.ResourceManager.GetString( "app_title" );
			string app_version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
			this.Title = app_title + " V" + app_version;
		}


	}
}
