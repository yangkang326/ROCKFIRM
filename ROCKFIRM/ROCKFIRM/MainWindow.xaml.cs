using Microsoft.Win32;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ROCKFIRM
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private string strWriteFilePath;

        private ISheet sheet;

        private DispatcherTimer collect_timer;

        private FileStream fs;

        private IWorkbook workbook;

        private int nRow;

        private recorddata recorddata_a = new recorddata();

        private bool exelcreated = false;

        private RotateTransform rotateTransform = new RotateTransform();

        private bool con1;

        private bool con2;
        public MainWindow()
        {
            InitializeComponent();
            Int32 nRow = 0;
            recorddata recorddata = new recorddata();
            collect_timer = new DispatcherTimer();
            collect_timer.Interval = TimeSpan.FromMilliseconds(50);
            collect_timer.Tick += new EventHandler(timer1_Tick);
            dataline.SetLeftCurve("俯仰角", null, Colors.Red);
            dataline.SetLeftCurve("翻滚角", null, Colors.Green);
            dataline.ValueMaxLeft = 90f;
            dataline.ValueMinLeft = -90f;
            dataline.IsRenderRightCoordinate = false;
            recorddata_a.connect_client1();
            control1.ipad = "192.168.1.10";
            control2.ipad = "192.168.1.11";
        }
		private void CreatExcel()
		{
			Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
			dlg.Filter = "xls|*.xls";
			bool? result = dlg.ShowDialog();
			if (result == true)
				strWriteFilePath = dlg.FileName;
			else
				return;
			fs = new System.IO.FileStream(strWriteFilePath, System.IO.FileMode.Create);
			if (strWriteFilePath.Contains(".xls"))
				workbook = new NPOI.HSSF.UserModel.HSSFWorkbook();
			else
				return;
			sheet = workbook.CreateSheet();
			IRow row = sheet.CreateRow(nRow);
			row.CreateCell(0).SetCellValue("时间");
			row.CreateCell(1).SetCellValue("俯仰角");
			row.CreateCell(2).SetCellValue("翻滚角");
			exelcreated = true;
		}
		private void connect_modbus(object sender, RoutedEventArgs e)
        {
			if (recorddata_a.connect_client0())
			{
				con1 = true;
			}
			else
			{
				con1 = false;
			}
			if (recorddata_a.connect_client1())
			{
				con2 = true;
			}
			else
			{
				con2 = false;
			}

			if (con1 & con2)
			{
				collect_timer.Start();
				conmodbus.Visibility = Visibility.Hidden;
			}
		}
		private void timer1_Tick(object sender, EventArgs e)
		{
			bool flag = !(con1 | con2);
			if (flag)
			{
				collect_timer.Stop();
				conmodbus.Visibility = Visibility.Visible;
                System.Windows.MessageBox.Show("控制器链接丢失，请重连");
			}
			bool flag2 = con2;
			if (flag2)
			{
				com2.IndicatorColor = Color.FromRgb(0, 255, 0);
				bool flag3 = nRow == 0;
				if (flag3)
				{
					CreatExcel();
					nRow++;
				}
				else
				{
					IRow row = sheet.CreateRow(nRow);
					row.CreateCell(0).SetCellValue(DateTime.Now.ToString("yyyy-MM-dd") + "-" + DateTime.Now.ToString("hh:mm:ss"));
					row.CreateCell(1).SetCellValue(recorddata_a.fydeg);
					row.CreateCell(2).SetCellValue(recorddata_a.fgdeg);
					bool flag4 = recorddata_a.getmeter();
					if (flag4)
					{
						bool flag5 = recorddata_a.fydeg != null & recorddata_a.fgdeg != null;
						if (flag5)
						{
							dataline.AddCurveData(new string[]
							{
								"FY",
								"FG"
							}, new float[]
							{
								(float)int.Parse(recorddata_a.fydeg),
								(float)int.Parse(recorddata_a.fgdeg)
							});
							status.Margin = new Thickness(0.0, (double)(int.Parse(recorddata_a.fydeg) * 2), 0.0, 0.0);
							rotateTransform.Angle = (double)int.Parse(recorddata_a.fgdeg);
							status.RenderTransform = rotateTransform;
							string text = recorddata_a.fgdeg;
							nRow++;
						}
					}
				}
				bool flag6 = !recorddata_a.connect_client1();
				if (flag6)
				{
					con2 = false;
				}
			}
			else
			{
				com2.IndicatorColor = Color.FromRgb(255, 0, 0);
			}
			bool flag7 = con1;
			if (flag7)
			{
				com1.IndicatorColor = Color.FromRgb(0, 255, 0);
				bool flag8 = recorddata_a.getledstatus();
				if (flag8)
				{
					bool flag9 = recorddata_a.led1_status;
					if (flag9)
					{
						led1_status.Content = "开>关";
						led1_status.Background = new SolidColorBrush(Colors.LightGreen);
					}
					else
					{
						led1_status.Content = "关>开";
						led1_status.Background = new SolidColorBrush(Colors.IndianRed);
					}
					bool flag10 = recorddata_a.led2_status;
					if (flag10)
					{
						led2_status.Content = "开>关";
						led2_status.Background = new SolidColorBrush(Colors.LightGreen);
					}
					else
					{
						led2_status.Content = "关>开";
						led2_status.Background = new SolidColorBrush(Colors.IndianRed);
					}
				}
				else
				{
					con1 = false;
				}
			}
			else
			{
				com1.IndicatorColor = Color.FromRgb(255, 0, 0);
				bool flag11 = recorddata_a.connect_client0();
				if (flag11)
				{
					con1 = true;
				}
				else
				{
					con1 = false;
				}
			}
		}
		private void led1_status_Click(object sender, RoutedEventArgs e)
		{
			bool flag = con1;
			if (flag)
			{
				recorddata_a.chang_led1_status();
			}
			//else
			//{
			//	bool flag2 = recorddata_a.connect_client0();
			//	if (flag2)
			//	{
			//		con1 = true;
			//	}
			//	else
			//	{
			//		con1 = false;
			//	}
			//}
		}

		private void led2_status_Click(object sender, RoutedEventArgs e)
		{
			bool flag = con1;
			if (flag)
			{
				recorddata_a.chang_led2_status();
			}
			//else
			//{
			//	bool flag2 = recorddata_a.connect_client0();
			//	if (flag2)
			//	{
			//		con1 = true;
			//	}
			//	else
			//	{
			//		con1 = false;
			//	}
			//}
		}
		private void Window_Closed(object sender, EventArgs e)
		{
			
		}

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
			bool flag = exelcreated;
			if (flag)
			{
				workbook.Write(fs);
				fs.Close();
			}
			System.Environment.Exit(0);
		}
    }
}
