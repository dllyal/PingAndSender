using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
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

namespace PingAndSender
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {   
        //测试地址
        string test_addr = "";
        //一轮时间
        int round_time = 0;
        //超时时间
        int time_out = 0;
        //超时次数
        int time_out_count = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Start_Button_Click(object sender, RoutedEventArgs e)
        {   
            //获取数据
            test_addr = TextBox_test_addr.Text.ToString().Trim();
            round_time = int.Parse(TextBox_round_time.Text.ToString().Trim());
            time_out = int.Parse(TextBox_time_out.Text.ToString().Trim());
            time_out_count = int.Parse(TextBox_time_out_count.Text.ToString().Trim());
            if ("".Equals(test_addr))
            {
                MessageBox.Show("测试地址不能为空");
                return;
            }

            Ping pingSender = new Ping();
            byte[] buffer = Encoding.ASCII.GetBytes("test");
            for(int i = 1; i <= time_out_count; i++)
            {
                Console.WriteLine("发送测试请求"+i.ToString());
                PingReply pingReply = pingSender.Send(test_addr, time_out*1000, buffer);
                if (pingReply.Status == IPStatus.Success)
                {
                    Console.WriteLine("SUCCESS!!");
                    Console.WriteLine("测试主机地址：" + pingReply.Address.ToString());
                    Console.WriteLine("Ping往返时间：" + pingReply.RoundtripTime.ToString());
                    Console.WriteLine("本次生存时间：" + pingReply.Options.Ttl);
                    Console.WriteLine("缓冲区的大小：" + pingReply.Buffer.Length);
                    break;
                }
            }
            
        }
    }
}
