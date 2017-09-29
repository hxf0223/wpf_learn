using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace WpfApp1
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        // http://www.cnblogs.com/zoexia/archive/2014/11/30/4134012.html
        // http://www.cnblogs.com/dino623/p/TemplatedControlAndItemsControl.html
        public ObservableCollection<ModelFile> CollectionModelFile = new ObservableCollection<ModelFile>();

        public MainWindow()
        {
            InitializeComponent();

            CollectionModelFile.Add(new ModelFile() { FileName = "WPF进化史", AuthorName = "王鹏", UpTime = "2014-10-10" });
            CollectionModelFile.Add(new ModelFile() { FileName = "WPF概论", AuthorName = "大飞", UpTime = "2014-10-10" });
            CollectionModelFile.Add(new ModelFile() { FileName = "WPF之美", AuthorName = "小虫", UpTime = "2014-10-11" });
            CollectionModelFile.Add(new ModelFile() { FileName = "WPF之道", AuthorName = "青草", UpTime = "2014-11-11" });
            CollectionModelFile.Add(new ModelFile() { FileName = "WPF之禅", AuthorName = "得瑟鬼", UpTime = "2014-11-11" });
            CollectionModelFile.Add(new ModelFile() { FileName = "WPF入门", AuthorName = "今晚吃什么", UpTime = "2014-11-11" });
            CollectionModelFile.Add(new ModelFile() { FileName = "WPF神技", AuthorName = "无间道王二", UpTime = "2014-12-12" });
            CollectionModelFile.Add(new ModelFile() { FileName = "WPF不为人知之密", AuthorName = "星期八", UpTime = "2014-12-12" });
            CollectionModelFile.Add(new ModelFile() { FileName = "WPF之革命", AuthorName = "两把刀", UpTime = "2014-12-12" });

            lbMain.ItemsSource = CollectionModelFile;

            ICollectionView cv = CollectionViewSource.GetDefaultView(lbMain.ItemsSource);
            cv.GroupDescriptions.Add(new PropertyGroupDescription("UpTime"));
        }
    }

    public class ModelFile
    {
        public string FileName { get; set; }
        public string AuthorName { get; set; }
        public string UpTime { get; set; }
    }

   
}
