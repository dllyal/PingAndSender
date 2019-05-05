using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

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
        //请求地址
        string client_addr = "";
        //轮数
        int all_count = 0;
        //最大失败轮数
        int max_faild_count = 0;
        //失败轮数
        int faild_count = 0;
        //等待时间
        int wait_time = 0;

        string header_json_str = "";
        string parameter_json_str = "";

        HeaderObject header = null;
        ParamObject parameter = null;

        bool doThread = false;

        WindowState lastWindowState;
        bool shouldClose;

        public MainWindow()
        {
            InitializeComponent();

            Start_Button.IsEnabled = true;
            Stop_Button.IsEnabled = false;

            //关闭事件
            //this.Closing += Window_Closing;

            //启动子线程
            Thread thread = new Thread(Thread_DoWork);
            thread.Start();

            //启动定时器刷新状态和wait时间
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            timer.Interval = 1000;
            timer.Enabled = true;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            lastWindowState = WindowState;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!shouldClose)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void OnNotificationAreaIconDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Open();
            }
        }

        private void OnMenuItemOpenClick(object sender, EventArgs e)
        {
            Open();
        }

        private void Open()
        {
            Show();
            WindowState = lastWindowState;
        }

        private void OnMenuItemExitClick(object sender, EventArgs e)
        {
            shouldClose = true;
            Close();
            System.Environment.Exit(0);
        }

        //定时器定时执行的方法
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (wait_time > 0)
            {
                Action<Label, String> updateAction = new Action<Label, string>(UpdateLableContent);
                if (doThread)
                {
                    Lable_last_time.Dispatcher.BeginInvoke(updateAction, Lable_last_time, wait_time.ToString());
                }
                else
                {
                    Lable_last_time.Dispatcher.BeginInvoke(updateAction, Lable_last_time, "停止");
                    Lable_state.Dispatcher.BeginInvoke(updateAction, Lable_state, "停止");
                }
                wait_time--;
            }
        }

        //窗体关闭时触发的事件，全部进程全结束掉
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            System.Environment.Exit(0);
        }

        //开始按钮初始化加载信息
        private void Start_Button_Click(object sender, RoutedEventArgs e)
        {
            //获取数据
            test_addr = TextBox_test_addr.Text.ToString().Trim();
            round_time = int.Parse(TextBox_round_time.Text.ToString().Trim());
            time_out = int.Parse(TextBox_time_out.Text.ToString().Trim());
            time_out_count = int.Parse(TextBox_time_out_count.Text.ToString().Trim());
            max_faild_count = int.Parse(TextBox_fail_count.Text.ToString().Trim());
            client_addr = TextBox_client_addr.Text.ToString().Trim();
            header_json_str = new TextRange(TextBox_header_json.Document.ContentStart, TextBox_header_json.Document.ContentEnd).Text.ToString().Trim();
            parameter_json_str = new TextRange(TextBox_parameter_json.Document.ContentStart, TextBox_parameter_json.Document.ContentEnd).Text.ToString().Trim();

            header = JsonConvert.DeserializeObject<HeaderObject>(header_json_str);
            parameter = JsonConvert.DeserializeObject<ParamObject>(parameter_json_str);

            Console.WriteLine(header_json_str);
            if ("".Equals(test_addr))
            {
                MessageBox.Show("测试地址不能为空");
                return;
            }
            doThread = true;
            Start_Button.IsEnabled = false;
            Stop_Button.IsEnabled = true;
        }

        //暂停子进程中方法的执行
        private void Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            doThread = false;
            Start_Button.IsEnabled = true;
            Stop_Button.IsEnabled = false;
        }

        //更新UI上的Label的方法
        private void UpdateLableContent(Label lable, string text)
        {
            lable.Content = text;
        }

        //更新UI上的RichTextBox的方法
        private void UpdateRichTextBoxText(RichTextBox richTextBox, string text)
        {
            Run run = new Run(text);
            Paragraph p = new Paragraph();
            p.Inlines.Add(run);
            richTextBox.Document.Blocks.Add(p);
        }

        //线程执行的代码
        private void Thread_DoWork()
        {
            while (true)
            {
                if (doThread)
                {
                    all_count++;
                    Action<Label, String> updateAction = new Action<Label, string>(UpdateLableContent);
                    Action<RichTextBox, String> updateRichTextBoxAction = new Action<RichTextBox, string>(UpdateRichTextBoxText);
                    //ping测试
                    bool test_flag = true;
                    Ping pingSender = new Ping();
                    byte[] buffer = Encoding.ASCII.GetBytes("test");
                    int num = 0;
                    for (int i = 1; i <= time_out_count; i++)
                    {
                        string msg = "第" + all_count.ToString() + "轮，第" + i.ToString() + "次发送测试请求";

                        //更新轮次信息
                        Lable_round_msg.Dispatcher.BeginInvoke(updateAction, Lable_round_msg, msg);
                        //更新状态信息
                        Lable_state.Dispatcher.BeginInvoke(updateAction, Lable_state, "发送测试请求中...");
                        //初始化等待时间
                        wait_time = time_out;

                        PingReply pingReply = pingSender.Send(test_addr, time_out * 1000, buffer);

                        if (pingReply.Status == IPStatus.Success)
                        {
                            msg = "SUCCESS!!\n" + 
                                  "测试主机地址：" + pingReply.Address.ToString() + "\n" +
                                  "Ping往返时间：" + pingReply.RoundtripTime.ToString() + "\n" +
                                  "本次生存时间：" + pingReply.Options.Ttl + "\n" +
                                  "缓冲区的大小：" + pingReply.Buffer.Length;
                            Lable_test_result.Dispatcher.BeginInvoke(updateAction, Lable_test_result, msg);
                            faild_count = 0;
                            break;
                        }
                        else
                        {
                            num++;
                        }
                    }
                    if (num >= time_out_count)
                    {
                        test_flag = false;
                    }

                    //如果测试失败次数为尝试次数,就发送请求
                    if (!test_flag)
                    {
                        Lable_state.Dispatcher.BeginInvoke(updateAction, Lable_state, "发送小助手请求中...");

                        Console.WriteLine(time_out_count + "次请求失败");
                        Console.WriteLine("发送小助手请求");
                        var client = new RestClient(client_addr);
                        var request = new RestRequest(Method.POST);

                        if (header != null)
                        {
                            List<ParamItem> headers = header.header;
                            foreach (ParamItem param in headers)
                            {
                                request.AddHeader(param.name, param.value);
                            }
                        }
                        if (parameter != null)
                        {
                            List<ParamItem> parames = parameter.parameter;
                            foreach (ParamItem param in parames)
                            {
                                request.AddParameter(param.name, param.value, ParameterType.RequestBody);
                            }
                        }

                        /*
                        request.AddHeader("Postman-Token", "fcae1338-8d10-4e96-9faf-e1ceb577b55e");
                        request.AddHeader("cache-control", "no-cache");
                        request.AddHeader("content-type", "multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW");
                        request.AddParameter("multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW", "------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"policyid\"\r\n\r\n4\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"deviceid\"\r\n\r\n30973\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"itemsid\"\r\n\r\n74###||###75###||###78###||###24###||###2###||###6###||###4###||###5\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"checkres\"\r\n\r\n%3C%3Fxml+version%3D%221.0%22+encoding%3D%22gbk%22%3F%3E%3CResult%3E%3CDllVersion%3E5%2C+2%2C+0%2C+226\n%3C%2FDllVersion%3E%3CCheckType%3E%3CInsideName%3ECheckUnlawfulConnectOut%3C%2FInsideName%3E%3COutsideName\n%3E%CD%F8%C2%E7%C1%AC%BD%D3%BC%EC%B2%E9%3C%2FOutsideName%3E%3CDesc%3E%BC%EC%B2%E9%B5%B1%C7%B0%BC%C6%CB\n%E3%BB%FA%CA%C7%B7%F1%B4%E6%D4%DA%CD%F8%C2%E7%C1%AC%BD%D3%D0%D0%CE%AA%2C%BB%F2%D5%DF%CB%AB%CD%F8%BF%A8\n%2C%B2%A6%BA%C5%C9%CF%CD%F8%2C%CE%DE%CF%DF%C9%CF%CD%F8%B5%C8%C7%E9%BF%F6%2C%C8%E7%B9%FB%B4%E6%D4%DA%D2\n%D4%C9%CF%C7%E9%BF%F6%2C%B8%F8%D3%E8%CF%E0%D3%A6%B4%A6%C0%ED.%3C%2FDesc%3E%3CResult%3EYes%3C%2FResult\n%3E%3CMessage%3E%C3%BB%D3%D0%B7%A2%CF%D6%CE%A5%B9%E6%CD%F8%C2%E7%C1%AC%BD%D3%3C%2FMessage%3E%3C%2FCheckType\n%3E%3CInfo%3E%3CIsExistOpenWirelessNetCard%3ENo%3C%2FIsExistOpenWirelessNetCard%3E%3CWirelessNetCard\n%3E%3C%2FWirelessNetCard%3E%3C%2FInfo%3E%3C%2FResult%3E%23%23%23%7C%7C%23%23%23%3C%3Fxml+version%3D%221\n.0%22+encoding%3D%22gbk%22%3F%3E%3CResult%3E%3CDllVersion%3E2018%2C+7%2C+0%2C+0%3C%2FDllVersion%3E%3CCheckType\n%3E%3CInsideName%3ECheckPatch%3C%2FInsideName%3E%3COutsideName%3E%CF%B5%CD%B3%B2%B9%B6%A1%BC%EC%B2%E9\n%3C%2FOutsideName%3E%3CDesc%3E%BC%EC%B2%E9%B5%B1%C7%B0%CF%B5%CD%B3%CA%C7%B7%F1%B4%E6%D4%DA%C2%A9%B6%B4\n%A3%AC%C8%B7%B1%A3%CF%B5%CD%B3%B0%B2%C8%AB%A1%A3%3C%2FDesc%3E%3CUpdateList%3E%3C%2FUpdateList%3E%3CUpdateError\n%3E%3C%2FUpdateError%3E%3CResult%3EYes%3C%2FResult%3E%3CMessage%3E%C4%FA%D2%D1%B0%B2%D7%B0%C1%CB%B9%DC\n%C0%ED%D4%B1%D2%AA%C7%F3%B5%C4%B2%B9%B6%A1%3C%2FMessage%3E%3C%2FCheckType%3E%3C%2FResult%3E%23%23%23\n%7C%7C%23%23%23%3C%3Fxml+version%3D%221.0%22+encoding%3D%22gbk%22%3F%3E%3CResult%3E%3CDllVersion%3E5\n%2C+2%2C+0%2C+1%3C%2FDllVersion%3E%3CCheckType%3E%3CInsideName%3ECheckAntiVirusSoft%3C%2FInsideName%3E\n%3COutsideName%3E%C9%B1%B6%BE%C8%ED%BC%FE%BC%EC%B2%E9%3C%2FOutsideName%3E%3CDesc%3E%B7%C0%B2%A1%B6%BE\n%C8%ED%BC%FE%D3%D0%D6%FA%D3%DA%B1%A3%BB%A4%C4%FA%B5%C4%BC%C6%CB%E3%BB%FA%C3%E2%CA%DC%B4%F3%B6%E0%CA%FD\n%B2%A1%B6%BE%A1%A2%C8%E4%B3%E6%A1%A2%CC%D8%C2%E5%D2%C1%C4%BE%C2%ED%D2%D4%BC%B0%C6%E4%CB%FB%BD%F8%D0%D0\n%B6%F1%D2%E2%C6%C6%BB%B5%B5%C4%C8%EB%C7%D6%D5%DF%B5%C4%B9%A5%BB%F7%A1%A3+%CB%FC%C3%C7%BF%C9%D2%D4%C9\n%BE%B3%FD%CE%C4%BC%FE%A1%A2%B7%C3%CE%CA%B8%F6%C8%CB%CA%FD%BE%DD%BB%F2%CA%B9%D3%C3%C4%FA%B5%C4%BC%C6%CB\n%E3%BB%FA%B9%A5%BB%F7%C6%E4%CB%FB%BC%C6%CB%E3%BB%FA%A1%A3%CD%A8%D3%C3%C9%B1%B6%BE%C8%ED%BC%FE%A3%BA%D6\n%B8Windows%B2%D9%D7%F7%CF%B5%CD%B3%C4%DC%CA%B6%B1%F0%B5%C4%C8%CE%BA%CE%D2%BB%BF%EE%C9%B1%B6%BE%C8%ED\n%BC%FE%A3%AC%B8%C3%D1%A1%CF%EE%D6%A7%B3%D6Windows+XP+SP2%BC%B0%D2%D4%C9%CF%B0%E6%B1%BE%B5%C4%B2%D9%D7\n%F7%CF%B5%CD%B3%3C%2FDesc%3E%3CResult%3EYes%3C%2FResult%3E%3CMessage%3E%C9%B1%B6%BE%C8%ED%BC%FE%BC%EC\n%B2%E9%B7%FB%BA%CF%B9%DC%C0%ED%D4%B1%D2%AA%C7%F3%3C%2FMessage%3E%3CInfo%3E%3CAntiVirusName%3E360%C9%B1\n%B6%BE%3C%2FAntiVirusName%3E%3CSoftVersion%3E5.0.0.8150%3C%2FSoftVersion%3E%3CDatabaseVersion%3E20190321\n%3C%2FDatabaseVersion%3E%3CIsRuning%3EYes%3C%2FIsRuning%3E%3CIsOpenActiveDefense%3EYes%3C%2FIsOpenActiveDefense\n%3E%3C%2FInfo%3E%3C%2FCheckType%3E%3C%2FResult%3E%23%23%23%7C%7C%23%23%23%3C%3Fxml+version%3D%221.0%22\n+encoding%3D%22gbk%22%3F%3E%3CResult%3E%3CDllVersion%3E1.0.0.1%3C%2FDllVersion%3E%3CCheckType%3E%3CInsideName\n%3ECheckIsFirewallEnable%3C%2FInsideName%3E%3COutsideName%3E%D6%F7%BB%FA%B7%C0%BB%F0%C7%BD%BC%EC%B2%E2\n%3C%2FOutsideName%3E%3CDesc%3E%BC%EC%B2%E9Windows+NT%C4%DA%D6%C3%B7%C0%BB%F0%C7%BD%CA%C7%B7%F1%BF%AA\n%C6%F4%B2%A2%CC%E1%B9%A9%D0%DE%B8%B4%D1%A1%CF%EE%A1%A3%3C%2FDesc%3E%3CResult%3EYes%3C%2FResult%3E%3CMessage\n%3E%B7%C0%BB%F0%C7%BD%BF%AA%C6%F4%3C%2FMessage%3E%3C%2FCheckType%3E%3C%2FResult%3E%23%23%23%7C%7C%23\n%23%23%3C%3Fxml+version%3D%221.0%22+encoding%3D%22gbk%22%3F%3E%3CResult%3E%3CDllVersion%3E5%2C+2%2C+0\n%2C+109%3C%2FDllVersion%3E%3CCheckType%3E%3CInsideName%3ECheckGuestUser%3C%2FInsideName%3E%3COutsideName\n%3EGuest%C0%B4%B1%F6%D5%CA%BB%A7%BC%EC%B2%E9%3C%2FOutsideName%3E%3CDesc%3EGuest%C0%B4%B1%F6%D5%CA%BB\n%A7%A3%AC%BF%C9%D2%D4%B7%C3%CE%CA%BC%C6%CB%E3%BB%FA%A3%AC%B5%AB%CA%DC%B5%BD%CF%DE%D6%C6%A1%A3%C3%BB%D3\n%D0%D0%DE%B8%C4%CF%B5%CD%B3%C9%E8%D6%C3%BA%CD%BD%F8%D0%D0%B0%B2%D7%B0%B3%CC%D0%F2%B5%C4%C8%A8%CF%DE%A3\n%AC%D2%B2%C3%BB%D3%D0%B4%B4%BD%A8%D0%DE%B8%C4%C8%CE%BA%CE%CE%C4%B5%B5%B5%C4%C8%A8%CF%DE%A3%AC%D6%BB%C4\n%DC%CA%C7%B6%C1%C8%A1%BC%C6%CB%E3%BB%FA%CF%B5%CD%B3%D0%C5%CF%A2%BA%CD%CE%C4%BC%FE%A1%A3%C6%F4%D3%C3GUEST\n%D5%CA%BB%A7%A3%AC%CE%AA%BA%DA%BF%CD%C8%EB%C7%D6%B4%F2%BF%AA%C1%CB%B7%BD%B1%E3%D6%AE%C3%C5%A1%A3%CE%AA\n%C1%CB%B1%A3%D6%A4%B5%E7%C4%D4%CF%B5%CD%B3%B0%B2%C8%AB%A3%AC%BD%A8%D2%E9%BD%FB%D3%C3%B4%CB%D5%CA%BB%A7\n%A1%A3%3C%2FDesc%3E%3CDetailInfo%3E%3C%2FDetailInfo%3E%3CResult%3EYes%3C%2FResult%3E%3CMessage%3E%D2\n%D1%BD%FB%D3%C3Guest%D3%C3%BB%A7%3C%2FMessage%3E%3C%2FCheckType%3E%3C%2FResult%3E%23%23%23%7C%7C%23%23\n%23%3C%3Fxml+version%3D%221.0%22+encoding%3D%22gbk%22%3F%3E%3CResult%3E%3CDllVersion%3E5%2C+2%2C+0%2C\n+109%3C%2FDllVersion%3E%3CCheckType%3E%3CInsideName%3ECheckPasswordPolicy%3C%2FInsideName%3E%3COutsideName\n%3E%C3%DC%C2%EB%B2%DF%C2%D4%C9%E8%D6%C3%BC%EC%B2%E9%3C%2FOutsideName%3E%3CDesc%3E%BC%EC%B2%E9%C9%E8%B1\n%B8%B5%C4%CF%B5%CD%B3%C3%DC%C2%EB%CA%C7%B7%F1%C2%FA%D7%E3%D2%AA%C7%F3%A3%AC%B1%E3%D3%DA%B1%A3%D6%A4%CF\n%B5%CD%B3%B5%C4%B0%B2%C8%AB%D0%D4%A1%A3%3C%2FDesc%3E%3CDetailInfo%3E8%7C0%7C90%7C1%7C15%7C5%7C15%3C%2FDetailInfo\n%3E%3CResult%3EYes%3C%2FResult%3E%3CMessage%3E%C3%DC%C2%EB%B2%DF%C2%D4%C9%E8%D6%C3%B7%FB%BA%CF%D2%AA\n%C7%F3%3C%2FMessage%3E%3C%2FCheckType%3E%3C%2FResult%3E%23%23%23%7C%7C%23%23%23%3C%3Fxml+version%3D%221\n.0%22+encoding%3D%22gbk%22%3F%3E%3CResult%3E%3CDllVersion%3E5%2C+2%2C+0%2C+109%3C%2FDllVersion%3E%3CCheckType\n%3E%3CInsideName%3ECheckScreenProtection%3C%2FInsideName%3E%3COutsideName%3E%C6%C1%C4%BB%B1%A3%BB%A4\n%C9%E8%D6%C3%BC%EC%B2%E9%3C%2FOutsideName%3E%3CDesc%3E%C6%C1%C4%BB%B1%A3%BB%A4%B3%CC%D0%F2%D7%EE%B3%F5\n%CA%C7%B1%BB%D3%C3%C0%B4%B1%A3%BB%A4%CF%D4%CA%BE%C6%F7%B5%C4%A3%AC%CF%D6%D4%DA%B4%F3%BC%D2%B6%E0%D3%C3\n%C6%C1%B1%A3%B5%C4%C3%DC%C2%EB%C0%B4%B1%A3%BB%A4%B5%E7%C4%D4%D4%DA%D6%F7%C8%CB%C0%EB%BF%AA%CA%B1%B2%BB\n%B1%BB%CB%FB%C8%CB%CA%B9%D3%C3%A3%AC%B4%EF%B5%BD%B1%A3%BB%A4%B8%F6%C8%CB%D2%FE%CB%BD%BA%CD%D0%C5%CF%A2\n%B0%B2%C8%AB%A1%A3%3C%2FDesc%3E%3CResult%3EYes%3C%2FResult%3E%3CMessage%3E%C6%C1%B1%A3%C9%E8%D6%C3%B7\n%FB%BA%CF%B9%DC%C0%ED%D4%B1%B5%C4%D2%AA%C7%F3%3C%2FMessage%3E%3C%2FCheckType%3E%3C%2FResult%3E%23%23\n%23%7C%7C%23%23%23%3C%3Fxml+version%3D%221.0%22+encoding%3D%22gbk%22%3F%3E%3CResult%3E%3CDllVersion%3E5\n%2C+2%2C+0%2C+109%3C%2FDllVersion%3E%3CCheckType%3E%3CInsideName%3ECheckVulnerablePassword%3C%2FInsideName\n%3E%3COutsideName%3E%C8%F5%BF%DA%C1%EE%D5%CA%BB%A7%BC%EC%B2%E9%3C%2FOutsideName%3E%3CDesc%3E%C8%F5%BF\n%DA%C1%EE%BC%B4%C8%DD%D2%D7%B1%BB%C6%C6%D2%EB%B5%C4%C3%DC%C2%EB%A3%AC%CF%F1%BC%F2%B5%A5%B5%C4%CA%FD%D7\n%D6%D7%E9%BA%CF+%C8%E712345%BB%F2%D5%DF%D3%EB%D5%CA%BA%C5%CF%E0%CD%AC%B5%C4%CA%FD%D7%D6%D7%E9%BA%CF%B3\n%C9%B5%C4%C3%DC%C2%EB%A3%AC%B5%C8%B6%BC%CA%C7%C8%F5%BF%DA%C1%EE%A1%A3%D3%B5%D3%D0%C8%F5%BF%DA%C1%EE%D5\n%CA%BB%A7%B5%C4%BB%E1%B8%F8%BA%DA%BF%CD%CC%E1%B9%A9%BC%AB%B4%F3%B5%C4%B1%E3%C0%FB%A3%AC%BD%F8%B6%F8%D3\n%B0%CF%EC%CF%B5%CD%B3%B5%C4%B0%B2%C8%AB%A1%A3%3C%2FDesc%3E%3CDetailInfo%3E%7C%7C%7C%3C%2FDetailInfo%3E\n%3CResult%3EYes%3C%2FResult%3E%3CMessage%3E%C3%BB%D3%D0%B7%A2%CF%D6%D3%C3%BB%A7%B4%E6%D4%DA%C8%F5%C3\n%DC%C2%EB%3C%2FMessage%3E%3C%2FCheckType%3E%3C%2FResult%3E\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"processtime\"\r\n\r\n9\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"is_guest\"\r\n\r\n0\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"roleid\"\r\n\r\n10\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"user_name\"\r\n\r\nding_yyun\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"password\"\r\n\r\nk6FnfSzdD53s%252BF5gADA8uv0wHCPFjELhu%252B1eKE1bTa6AdkwxX%252BCraxP6TJuFg%252BD8aNq3lUkMDDIB8I9qkSER7WchCV1raZR8G3OFtbYCdUbPKLgeGZWAxGQZ2xyIPEXmzmuhxq5CvBJ2Z1oReXcW3Lrd2onXt\n%252BQDXpLIY3W0Hjk%3D\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"auth_type\"\r\n\r\nADAutoLogin\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"is_auto_auth\"\r\n\r\nNo\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"is_safecheck\"\r\n\r\n1\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"isActive\"\r\n\r\n1\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"repair\"\r\n\r\n\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"reCheck\"\r\n\r\n\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"ControlPostion\"\r\n\r\n1\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"ChangeTime\"\r\n\r\n2019-03-21+11%3A27%3A53\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"CacheCheckItem\"\r\n\r\n%3C%3Fxml+version%3D%221.0%22+encoding%3D%22gbk%22%3F%3E%0D%0A%09%09%09%09%3CCheckType%3E%0D%0A%09%09\n%09%09%09%3CInsideName%3ECheckUnlawfulConnectOut%3C%2FInsideName%3E%0D%0A%09%09%09%09%09%3COutsideName\n%3E%CD%F8%C2%E7%C1%AC%BD%D3%BC%EC%B2%E9%3C%2FOutsideName%3E%0D%0A%09%09%09%09%09%3CDesc%3E%BC%EC%B2%E9\n%B5%B1%C7%B0%BC%C6%CB%E3%BB%FA%CA%C7%B7%F1%B4%E6%D4%DA%CD%F8%C2%E7%C1%AC%BD%D3%D0%D0%CE%AA%2C%BB%F2%D5\n%DF%CB%AB%CD%F8%BF%A8%2C%B2%A6%BA%C5%C9%CF%CD%F8%2C%CE%DE%CF%DF%C9%CF%CD%F8%B5%C8%C7%E9%BF%F6%2C%C8%E7\n%B9%FB%B4%E6%D4%DA%D2%D4%C9%CF%C7%E9%BF%F6%2C%B8%F8%D3%E8%CF%E0%D3%A6%B4%A6%C0%ED.%3C%2FDesc%3E%0D%0A\n%09%09%09%09%09%3CDLL%3EMsacAssRuntimeCheck.dll%3C%2FDLL%3E%0D%0A%09%09%09%09%09%3CDLLFunc%3ECheckUnlawfulConnectOut\n%3C%2FDLLFunc%3E%0D%0A%09%09%09%09%09%3COption%3E%0D%0A%09%09%09%09%09%09%3CServerIp%3E%3C%2FServerIp\n%3E%0D%0A++++++++++++++++++++++++%3CServerPort%3E%3C%2FServerPort%3E%0D%0A%09%09%09%09%09%09%3CWhatToCheck\n%3E%0D%0A%09%09%09%09%09+++++++%3CCheckDial%3ENo%3C%2FCheckDial%3E%0D%0A%09%09%09%09%09+++++++%3CCheckDoubleNetCard\n%3EYes%3C%2FCheckDoubleNetCard%3E%0D%0A%09%09%09%09%09+++++++%3CCheckIsCanUseWWW%3ENo%3C%2FCheckIsCanUseWWW\n%3E%0D%0A%09%09%09%09%09%09%09%3CExceptCardName%3EVMware%7Cvirtual%7CSangfor%7CSSL%3C%2FExceptCardName\n%3E%0D%0A%09%09%09%09%09%09%09%3CCheckWWWInterval%3E10%3C%2FCheckWWWInterval%3E%0D%0A%09%09%09%09%09\n%09%09%3CCheckDialInterval%3E10%3C%2FCheckDialInterval%3E%0D%0A%09%09%09%09%09%09%09%3CCheckNetCardInterval\n%3E300%3C%2FCheckNetCardInterval%3E%0D%0A%09%09%09%09%09%09%09%3CCheckWirelessNetCard%3EYes%3C%2FCheckWirelessNetCard\n%3E%0D%0A%09%09%09%09%09%09%09%3CCheckWirelessNetCardInterval%3E300%3C%2FCheckWirelessNetCardInterval\n%3E%0D%0A%09%09%09%09%09++++%3C%2FWhatToCheck%3E%0D%0A%09%09%09%09%09++++%3CWhatToDo%3E%0D%0A%09%09%09\n%09%09+++++++%3CPromptUser%3E%C4%FA%CA%B9%D3%C3%C1%CB%B7%C7%B7%A8%B5%C4%C1%AC%BD%D3%B7%BD%CA%BD%B7%C3\n%CE%CA%C1%CB%CD%F8%C2%E7%A3%AC%C7%EB%C1%A2%BC%B4%D6%D5%D6%B9%B8%C3%D0%D0%CE%AA%21%3C%2FPromptUser%3E\n%0D%0A%09%09%09%09%09+++++++%3CCutDial%3ENo%3C%2FCutDial%3E%0D%0A%09%09%09%09%09+++++++%3CCutNet%3ENo\n%3C%2FCutNet%3E%0D%0A%09%09%09%09%09+++++++%3CIsAlert%3EYes%3C%2FIsAlert%3E%0D%0A%09%09%09%09%09++++\n+++%3CPrompt%3EYes%3C%2FPrompt%3E%0D%0A%09%09%09%09%09%09+++%3CForbidNetCard%3EYes%3C%2FForbidNetCard\n%3E%0D%0A%09%09%09%09%09%09+++%3CForbidWirelessNetCard%3EYes%3C%2FForbidWirelessNetCard%3E%0D%0A%09%09\n%09%09%09++++%3C%2FWhatToDo%3E+%0D%0A%09%09%09%09%09%3C%2FOption%3E%0D%0A%09%09%09%09%3C%2FCheckType\n%3E%23%23%23%7C%7C%23%23%23%3C%3Fxml+version%3D%221.0%22+encoding%3D%22gbk%22%3F%3E%0D%0A%09%09%09%09\n%3CCheckType%3E%0D%0A%09%09%09%09%09%3CInsideName%3ECheckPatch%3C%2FInsideName%3E%0D%0A%09%09%09%09%09\n%3COutsideName%3E%CF%B5%CD%B3%B2%B9%B6%A1%BC%EC%B2%E9%3C%2FOutsideName%3E%0D%0A%09%09%09%09%09%3CDesc\n%3E%BC%EC%B2%E9%B5%B1%C7%B0%CF%B5%CD%B3%CA%C7%B7%F1%B4%E6%D4%DA%C2%A9%B6%B4%A3%AC%C8%B7%B1%A3%CF%B5%CD\n%B3%B0%B2%C8%AB%A1%A3%3C%2FDesc%3E%0D%0A%09%09%09%09%09%3CDLL%3EMsacCheckPatchNew.dll%3C%2FDLL%3E%0D\n%0A%09%09%09%09%09%3CDLLFunc%3ECheckPatch%3C%2FDLLFunc%3E%0D%0A%09%09%09%09%09%3COption%3E%0D%0A%09%09\n%09%09%09%09%3CServerIp%3E%3C%2FServerIp%3E%0D%0A%09%09%09%09%09%09%3CServerPort%3E%3C%2FServerPort%3E\n%0D%0A%09%09%09%09%09%09%3CPatchListFile%3ENeedScanUpdateList_4.xml%3C%2FPatchListFile%3E%0D%0A%09%09\n%09%09%09%09%3CUpdateType%3E17%7C1%3C%2FUpdateType%3E%0D%0A%09%09%09%09%09%09%3CExceptType%3E0%3C%2FExceptType\n%3E%0D%0A%09%09%09%09%09%09%3CExceptRevisionID%3E275869400%2C275869600%3C%2FExceptRevisionID%3E%3CIsCheckOffice\n%3E0%3C%2FIsCheckOffice%3E%3CIsCheckWin10%3E0%3C%2FIsCheckWin10%3E%3CIsKey%3E%3C%2FIsKey%3E%3CIsAutoRepair\n%3E%3C%2FIsAutoRepair%3E%0D%0A%09%09%09%09%09%3C%2FOption%3E%0D%0A%09%09%09%09%09%3C%2FCheckType%3E%23\n%23%23%7C%7C%23%23%23%3C%3Fxml+version%3D%221.0%22+encoding%3D%22gbk%22%3F%3E%0D%0A%09%09%09%3CCheckType\n%3E%0D%0A%09%09%09%09%3CInsideName%3ECheckAntiVirusSoft%3C%2FInsideName%3E%0D%0A%09%09%09%09%3COutsideName\n%3E%C9%B1%B6%BE%C8%ED%BC%FE%BC%EC%B2%E9%3C%2FOutsideName%3E%0D%0A%09%09%09%09%3CDesc%3E%B7%C0%B2%A1%B6\n%BE%C8%ED%BC%FE%D3%D0%D6%FA%D3%DA%B1%A3%BB%A4%C4%FA%B5%C4%BC%C6%CB%E3%BB%FA%C3%E2%CA%DC%B4%F3%B6%E0%CA\n%FD%B2%A1%B6%BE%A1%A2%C8%E4%B3%E6%A1%A2%CC%D8%C2%E5%D2%C1%C4%BE%C2%ED%D2%D4%BC%B0%C6%E4%CB%FB%BD%F8%D0\n%D0%B6%F1%D2%E2%C6%C6%BB%B5%B5%C4%C8%EB%C7%D6%D5%DF%B5%C4%B9%A5%BB%F7%A1%A3+%CB%FC%C3%C7%BF%C9%D2%D4\n%C9%BE%B3%FD%CE%C4%BC%FE%A1%A2%B7%C3%CE%CA%B8%F6%C8%CB%CA%FD%BE%DD%BB%F2%CA%B9%D3%C3%C4%FA%B5%C4%BC%C6\n%CB%E3%BB%FA%B9%A5%BB%F7%C6%E4%CB%FB%BC%C6%CB%E3%BB%FA%A1%A3%CD%A8%D3%C3%C9%B1%B6%BE%C8%ED%BC%FE%A3%BA\n%D6%B8Windows%B2%D9%D7%F7%CF%B5%CD%B3%C4%DC%CA%B6%B1%F0%B5%C4%C8%CE%BA%CE%D2%BB%BF%EE%C9%B1%B6%BE%C8\n%ED%BC%FE%A3%AC%B8%C3%D1%A1%CF%EE%D6%A7%B3%D6Windows+XP+SP2%BC%B0%D2%D4%C9%CF%B0%E6%B1%BE%B5%C4%B2%D9\n%D7%F7%CF%B5%CD%B3%3C%2FDesc%3E%0D%0A%09%09%09%09%3CDLL%3EMsacCheckAntiVirusSoft.dll%3C%2FDLL%3E%0D%0A\n%09%09%09%09%3CDLLFunc%3ECheckAntiVirusSoft%3C%2FDLLFunc%3E%0D%0A%09%09%09%09%3COption%3E%0D%0A%09%09\n%09%09%3CServerIp%3E%3C%2FServerIp%3E++++%0D%0A++++++++++++++++%3CServerPort%3E%3C%2FServerPort%3E%3CBaseCheckItem\n%3E%0D%0A%09%09%09%09%09%3CIsCheck%3ENo%3C%2FIsCheck%3E%0D%0A++++++++++++++++++++%3CIsMobile%3ENo%3C\n%2FIsMobile%3E++++%0D%0A%09%09%09%09%09%3CAntiVirusName%3E%B0%D9%B6%C8%C9%B1%B6%BE%3C%2FAntiVirusName\n%3E%0D%0A%09%09%09%09%09%3CIsRuning%3EYes%3C%2FIsRuning%3E%0D%0A%09%09%09%09%09%3CSoftCompare%3EMoreThan\n%3C%2FSoftCompare%3E%0D%0A%09%09%09%09%09%3CSoftVersion%3E1.0%3C%2FSoftVersion%3E%0D%0A%09%09%09%09%09\n%3CDBCompareType%3EDay%3C%2FDBCompareType%3E%0D%0A%09%09%09%09%09%3CDBCompare%3ELessThan%3C%2FDBCompare\n%3E%0D%0A%09%09%09%09%09%3CDBVersion%3E15%3C%2FDBVersion%3E%0D%0A%09%09%09%09%09%3CRepair%3E%3CAntVersion\n%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg\n%3E%3C%2FAntVersion%3E%3CDBVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath\n%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FDBVersion%3E%3C%2FRepair%3E%0D%0A%09%09%09%09%09%3CActiveDefenseCheck\n%3ENo%3C%2FActiveDefenseCheck%3E%0D%0A%09%09%09%09%3C%2FBaseCheckItem%3E%3CBaseCheckItem%3E%0D%0A%09\n%09%09%09%09%3CIsCheck%3ENo%3C%2FIsCheck%3E%0D%0A++++++++++++++++++++%3CIsMobile%3ENo%3C%2FIsMobile%3E\n++++%0D%0A%09%09%09%09%09%3CAntiVirusName%3E%C8%FC%C3%C5%CC%FA%BF%CB%3C%2FAntiVirusName%3E%0D%0A%09%09\n%09%09%09%3CIsRuning%3EYes%3C%2FIsRuning%3E%0D%0A%09%09%09%09%09%3CSoftCompare%3EMoreThan%3C%2FSoftCompare\n%3E%0D%0A%09%09%09%09%09%3CSoftVersion%3E8.0%3C%2FSoftVersion%3E%0D%0A%09%09%09%09%09%3CDBCompareType\n%3EDay%3C%2FDBCompareType%3E%0D%0A%09%09%09%09%09%3CDBCompare%3ELessThan%3C%2FDBCompare%3E%0D%0A%09%09\n%09%09%09%3CDBVersion%3E15%3C%2FDBVersion%3E%0D%0A%09%09%09%09%09%3CRepair%3E%3CAntVersion%3E%3CWay%3E\n%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FAntVersion\n%3E%3CDBVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg\n%3E%3C%2FSoftArg%3E%3C%2FDBVersion%3E%3C%2FRepair%3E%0D%0A%09%09%09%09%09%3CActiveDefenseCheck%3ENo%3C\n%2FActiveDefenseCheck%3E%0D%0A%09%09%09%09%3C%2FBaseCheckItem%3E%3CBaseCheckItem%3E%0D%0A%09%09%09%09\n%09%3CIsCheck%3ENo%3C%2FIsCheck%3E%0D%0A++++++++++++++++++++%3CIsMobile%3ENo%3C%2FIsMobile%3E++++%0D\n%0A%09%09%09%09%09%3CAntiVirusName%3E%C8%F0%D0%C7%3C%2FAntiVirusName%3E%0D%0A%09%09%09%09%09%3CIsRuning\n%3EYes%3C%2FIsRuning%3E%0D%0A%09%09%09%09%09%3CSoftCompare%3EMoreThan%3C%2FSoftCompare%3E%0D%0A%09%09\n%09%09%09%3CSoftVersion%3E20.14.12%3C%2FSoftVersion%3E%0D%0A%09%09%09%09%09%3CDBCompareType%3EDay%3C\n%2FDBCompareType%3E%0D%0A%09%09%09%09%09%3CDBCompare%3ELessThan%3C%2FDBCompare%3E%0D%0A%09%09%09%09%09\n%3CDBVersion%3E15%3C%2FDBVersion%3E%0D%0A%09%09%09%09%09%3CRepair%3E%3CAntVersion%3E%3CWay%3E%3C%2FWay\n%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FAntVersion\n%3E%3CDBVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg\n%3E%3C%2FSoftArg%3E%3C%2FDBVersion%3E%3C%2FRepair%3E%0D%0A%09%09%09%09%09%3CActiveDefenseCheck%3ENo%3C\n%2FActiveDefenseCheck%3E%0D%0A%09%09%09%09%3C%2FBaseCheckItem%3E%3CBaseCheckItem%3E%0D%0A%09%09%09%09\n%09%3CIsCheck%3ENo%3C%2FIsCheck%3E%0D%0A++++++++++++++++++++%3CIsMobile%3ENo%3C%2FIsMobile%3E++++%0D\n%0A%09%09%09%09%09%3CAntiVirusName%3E%C8%F0%D0%C7%28ESM%29%3C%2FAntiVirusName%3E%0D%0A%09%09%09%09%09\n%3CIsRuning%3EYes%3C%2FIsRuning%3E%0D%0A%09%09%09%09%09%3CSoftCompare%3EMoreThan%3C%2FSoftCompare%3E\n%0D%0A%09%09%09%09%09%3CSoftVersion%3E24.00.93.48%3C%2FSoftVersion%3E%0D%0A%09%09%09%09%09%3CDBCompareType\n%3EDay%3C%2FDBCompareType%3E%0D%0A%09%09%09%09%09%3CDBCompare%3ELessThan%3C%2FDBCompare%3E%0D%0A%09%09\n%09%09%09%3CDBVersion%3E15%3C%2FDBVersion%3E%0D%0A%09%09%09%09%09%3CRepair%3E%3CAntVersion%3E%3CWay%3E\n%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FAntVersion\n%3E%3CDBVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg\n%3E%3C%2FSoftArg%3E%3C%2FDBVersion%3E%3C%2FRepair%3E%0D%0A%09%09%09%09%09%3CActiveDefenseCheck%3ENo%3C\n%2FActiveDefenseCheck%3E%0D%0A%09%09%09%09%3C%2FBaseCheckItem%3E%3CBaseCheckItem%3E%0D%0A%09%09%09%09\n%09%3CIsCheck%3ENo%3C%2FIsCheck%3E%0D%0A++++++++++++++++++++%3CIsMobile%3ENo%3C%2FIsMobile%3E++++%0D\n%0A%09%09%09%09%09%3CAntiVirusName%3EMcAfee%3C%2FAntiVirusName%3E%0D%0A%09%09%09%09%09%3CIsRuning%3EYes\n%3C%2FIsRuning%3E%0D%0A%09%09%09%09%09%3CSoftCompare%3EMoreThan%3C%2FSoftCompare%3E%0D%0A%09%09%09%09\n%09%3CSoftVersion%3E8.5%3C%2FSoftVersion%3E%0D%0A%09%09%09%09%09%3CDBCompareType%3EDay%3C%2FDBCompareType\n%3E%0D%0A%09%09%09%09%09%3CDBCompare%3ELessThan%3C%2FDBCompare%3E%0D%0A%09%09%09%09%09%3CDBVersion%3E15\n%3C%2FDBVersion%3E%0D%0A%09%09%09%09%09%3CRepair%3E%3CAntVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C\n%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FAntVersion%3E%3CDBVersion\n%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg\n%3E%3C%2FDBVersion%3E%3C%2FRepair%3E%0D%0A%09%09%09%09%09%3CActiveDefenseCheck%3ENo%3C%2FActiveDefenseCheck\n%3E%0D%0A%09%09%09%09%3C%2FBaseCheckItem%3E%3CBaseCheckItem%3E%0D%0A%09%09%09%09%09%3CIsCheck%3ENo%3C\n%2FIsCheck%3E%0D%0A++++++++++++++++++++%3CIsMobile%3ENo%3C%2FIsMobile%3E++++%0D%0A%09%09%09%09%09%3CAntiVirusName\n%3E%BF%A8%B0%CD%CB%B9%BB%F9%3C%2FAntiVirusName%3E%0D%0A%09%09%09%09%09%3CIsRuning%3EYes%3C%2FIsRuning\n%3E%0D%0A%09%09%09%09%09%3CSoftCompare%3EMoreThan%3C%2FSoftCompare%3E%0D%0A%09%09%09%09%09%3CSoftVersion\n%3E6.0%3C%2FSoftVersion%3E%0D%0A%09%09%09%09%09%3CDBCompareType%3EDay%3C%2FDBCompareType%3E%0D%0A%09\n%09%09%09%09%3CDBCompare%3ELessThan%3C%2FDBCompare%3E%0D%0A%09%09%09%09%09%3CDBVersion%3E15%3C%2FDBVersion\n%3E%0D%0A%09%09%09%09%09%3CRepair%3E%3CAntVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath\n%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FAntVersion%3E%3CDBVersion%3E%3CWay%3E%3C%2FWay\n%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FDBVersion%3E\n%3C%2FRepair%3E%0D%0A%09%09%09%09%09%3CActiveDefenseCheck%3ENo%3C%2FActiveDefenseCheck%3E%0D%0A%09%09\n%09%09%3C%2FBaseCheckItem%3E%3CBaseCheckItem%3E%0D%0A%09%09%09%09%09%3CIsCheck%3ENo%3C%2FIsCheck%3E%0D\n%0A++++++++++++++++++++%3CIsMobile%3ENo%3C%2FIsMobile%3E++++%0D%0A%09%09%09%09%09%3CAntiVirusName%3E\n%BD%F0%C9%BD%B6%BE%B0%D4%3C%2FAntiVirusName%3E%0D%0A%09%09%09%09%09%3CIsRuning%3EYes%3C%2FIsRuning%3E\n%0D%0A%09%09%09%09%09%3CSoftCompare%3EMoreThan%3C%2FSoftCompare%3E%0D%0A%09%09%09%09%09%3CSoftVersion\n%3E6.0%3C%2FSoftVersion%3E%0D%0A%09%09%09%09%09%3CDBCompareType%3EDay%3C%2FDBCompareType%3E%0D%0A%09\n%09%09%09%09%3CDBCompare%3ELessThan%3C%2FDBCompare%3E%0D%0A%09%09%09%09%09%3CDBVersion%3E15%3C%2FDBVersion\n%3E%0D%0A%09%09%09%09%09%3CRepair%3E%3CAntVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath\n%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FAntVersion%3E%3CDBVersion%3E%3CWay%3E%3C%2FWay\n%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FDBVersion%3E\n%3C%2FRepair%3E%0D%0A%09%09%09%09%09%3CActiveDefenseCheck%3ENo%3C%2FActiveDefenseCheck%3E%0D%0A%09%09\n%09%09%3C%2FBaseCheckItem%3E%3CBaseCheckItem%3E%0D%0A%09%09%09%09%09%3CIsCheck%3ENo%3C%2FIsCheck%3E%0D\n%0A++++++++++++++++++++%3CIsMobile%3ENo%3C%2FIsMobile%3E++++%0D%0A%09%09%09%09%09%3CAntiVirusName%3E\n%BD%AD%C3%F1%3C%2FAntiVirusName%3E%0D%0A%09%09%09%09%09%3CIsRuning%3EYes%3C%2FIsRuning%3E%0D%0A%09%09\n%09%09%09%3CSoftCompare%3EMoreThan%3C%2FSoftCompare%3E%0D%0A%09%09%09%09%09%3CSoftVersion%3E11.0%3C%2FSoftVersion\n%3E%0D%0A%09%09%09%09%09%3CDBCompareType%3EDay%3C%2FDBCompareType%3E%0D%0A%09%09%09%09%09%3CDBCompare\n%3ELessThan%3C%2FDBCompare%3E%0D%0A%09%09%09%09%09%3CDBVersion%3E15%3C%2FDBVersion%3E%0D%0A%09%09%09\n%09%09%3CRepair%3E%3CAntVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath\n%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FAntVersion%3E%3CDBVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl\n%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FDBVersion%3E%3C%2FRepair%3E%0D%0A\n%09%09%09%09%09%3CActiveDefenseCheck%3ENo%3C%2FActiveDefenseCheck%3E%0D%0A%09%09%09%09%3C%2FBaseCheckItem\n%3E%3CBaseCheckItem%3E%0D%0A%09%09%09%09%09%3CIsCheck%3ENo%3C%2FIsCheck%3E%0D%0A++++++++++++++++++++\n%3CIsMobile%3ENo%3C%2FIsMobile%3E++++%0D%0A%09%09%09%09%09%3CAntiVirusName%3EKill%3C%2FAntiVirusName\n%3E%0D%0A%09%09%09%09%09%3CIsRuning%3EYes%3C%2FIsRuning%3E%0D%0A%09%09%09%09%09%3CSoftCompare%3EMoreThan\n%3C%2FSoftCompare%3E%0D%0A%09%09%09%09%09%3CSoftVersion%3E8.1%3C%2FSoftVersion%3E%0D%0A%09%09%09%09%09\n%3CDBCompareType%3EDay%3C%2FDBCompareType%3E%0D%0A%09%09%09%09%09%3CDBCompare%3ELessThan%3C%2FDBCompare\n%3E%0D%0A%09%09%09%09%09%3CDBVersion%3E15%3C%2FDBVersion%3E%0D%0A%09%09%09%09%09%3CRepair%3E%3CAntVersion\n%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg\n%3E%3C%2FAntVersion%3E%3CDBVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath\n%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FDBVersion%3E%3C%2FRepair%3E%0D%0A%09%09%09%09%09%3CActiveDefenseCheck\n%3ENo%3C%2FActiveDefenseCheck%3E%0D%0A%09%09%09%09%3C%2FBaseCheckItem%3E%3CBaseCheckItem%3E%0D%0A%09\n%09%09%09%09%3CIsCheck%3ENo%3C%2FIsCheck%3E%0D%0A++++++++++++++++++++%3CIsMobile%3ENo%3C%2FIsMobile%3E\n++++%0D%0A%09%09%09%09%09%3CAntiVirusName%3ENOD32%3C%2FAntiVirusName%3E%0D%0A%09%09%09%09%09%3CIsRuning\n%3EYes%3C%2FIsRuning%3E%0D%0A%09%09%09%09%09%3CSoftCompare%3EMoreThan%3C%2FSoftCompare%3E%0D%0A%09%09\n%09%09%09%3CSoftVersion%3E2.7%3C%2FSoftVersion%3E%0D%0A%09%09%09%09%09%3CDBCompareType%3EDay%3C%2FDBCompareType\n%3E%0D%0A%09%09%09%09%09%3CDBCompare%3ELessThan%3C%2FDBCompare%3E%0D%0A%09%09%09%09%09%3CDBVersion%3E15\n%3C%2FDBVersion%3E%0D%0A%09%09%09%09%09%3CRepair%3E%3CAntVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C\n%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FAntVersion%3E%3CDBVersion\n%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg\n%3E%3C%2FDBVersion%3E%3C%2FRepair%3E%0D%0A%09%09%09%09%09%3CActiveDefenseCheck%3ENo%3C%2FActiveDefenseCheck\n%3E%0D%0A%09%09%09%09%3C%2FBaseCheckItem%3E%3CBaseCheckItem%3E%0D%0A%09%09%09%09%09%3CIsCheck%3ENo%3C\n%2FIsCheck%3E%0D%0A++++++++++++++++++++%3CIsMobile%3ENo%3C%2FIsMobile%3E++++%0D%0A%09%09%09%09%09%3CAntiVirusName\n%3EMicrosoft+Security+Essentials%3C%2FAntiVirusName%3E%0D%0A%09%09%09%09%09%3CIsRuning%3EYes%3C%2FIsRuning\n%3E%0D%0A%09%09%09%09%09%3CSoftCompare%3EMoreThan%3C%2FSoftCompare%3E%0D%0A%09%09%09%09%09%3CSoftVersion\n%3E1.0%3C%2FSoftVersion%3E%0D%0A%09%09%09%09%09%3CDBCompareType%3EDay%3C%2FDBCompareType%3E%0D%0A%09\n%09%09%09%09%3CDBCompare%3ELessThan%3C%2FDBCompare%3E%0D%0A%09%09%09%09%09%3CDBVersion%3E15%3C%2FDBVersion\n%3E%0D%0A%09%09%09%09%09%3CRepair%3E%3CAntVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath\n%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FAntVersion%3E%3CDBVersion%3E%3CWay%3E%3C%2FWay\n%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FDBVersion%3E\n%3C%2FRepair%3E%0D%0A%09%09%09%09%09%3CActiveDefenseCheck%3ENo%3C%2FActiveDefenseCheck%3E%0D%0A%09%09\n%09%09%3C%2FBaseCheckItem%3E%3CBaseCheckItem%3E%0D%0A%09%09%09%09%09%3CIsCheck%3EYes%3C%2FIsCheck%3E\n%0D%0A++++++++++++++++++++%3CIsMobile%3ENo%3C%2FIsMobile%3E++++%0D%0A%09%09%09%09%09%3CAntiVirusName\n%3E360%C9%B1%B6%BE%3C%2FAntiVirusName%3E%0D%0A%09%09%09%09%09%3CIsRuning%3EYes%3C%2FIsRuning%3E%0D%0A\n%09%09%09%09%09%3CSoftCompare%3ELessThan%3C%2FSoftCompare%3E%0D%0A%09%09%09%09%09%3CSoftVersion%3E5.0\n%3C%2FSoftVersion%3E%0D%0A%09%09%09%09%09%3CDBCompareType%3EDay%3C%2FDBCompareType%3E%0D%0A%09%09%09\n%09%09%3CDBCompare%3ELessThan%3C%2FDBCompare%3E%0D%0A%09%09%09%09%09%3CDBVersion%3E15%3C%2FDBVersion\n%3E%0D%0A%09%09%09%09%09%3CRepair%3E%3CAntVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath\n%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FAntVersion%3E%3CDBVersion%3E%3CWay%3E%3C%2FWay\n%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FDBVersion%3E\n%3C%2FRepair%3E%0D%0A%09%09%09%09%09%3CActiveDefenseCheck%3ENo%3C%2FActiveDefenseCheck%3E%0D%0A%09%09\n%09%09%3C%2FBaseCheckItem%3E%3CBaseCheckItem%3E%0D%0A%09%09%09%09%09%3CIsCheck%3ENo%3C%2FIsCheck%3E%0D\n%0A++++++++++++++++++++%3CIsMobile%3ENo%3C%2FIsMobile%3E++++%0D%0A%09%09%09%09%09%3CAntiVirusName%3E\n%C7%F7%CA%C6%3C%2FAntiVirusName%3E%0D%0A%09%09%09%09%09%3CIsRuning%3EYes%3C%2FIsRuning%3E%0D%0A%09%09\n%09%09%09%3CSoftCompare%3EMoreThan%3C%2FSoftCompare%3E%0D%0A%09%09%09%09%09%3CSoftVersion%3E16.0.0%3C\n%2FSoftVersion%3E%0D%0A%09%09%09%09%09%3CDBCompareType%3EDay%3C%2FDBCompareType%3E%0D%0A%09%09%09%09\n%09%3CDBCompare%3ELessThan%3C%2FDBCompare%3E%0D%0A%09%09%09%09%09%3CDBVersion%3E15%3C%2FDBVersion%3E\n%0D%0A%09%09%09%09%09%3CRepair%3E%3CAntVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath\n%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FAntVersion%3E%3CDBVersion%3E%3CWay%3E%3C%2FWay\n%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FDBVersion%3E\n%3C%2FRepair%3E%0D%0A%09%09%09%09%09%3CActiveDefenseCheck%3ENo%3C%2FActiveDefenseCheck%3E%0D%0A%09%09\n%09%09%3C%2FBaseCheckItem%3E%3CBaseCheckItem%3E%0D%0A%09%09%09%09%09%3CIsCheck%3ENo%3C%2FIsCheck%3E%0D\n%0A++++++++++++++++++++%3CIsMobile%3ENo%3C%2FIsMobile%3E++++%0D%0A%09%09%09%09%09%3CAntiVirusName%3E\n%D0%A1%BA%EC%C9%A1%3C%2FAntiVirusName%3E%0D%0A%09%09%09%09%09%3CIsRuning%3EYes%3C%2FIsRuning%3E%0D%0A\n%09%09%09%09%09%3CSoftCompare%3EMoreThan%3C%2FSoftCompare%3E%0D%0A%09%09%09%09%09%3CSoftVersion%3E9.0\n%3C%2FSoftVersion%3E%0D%0A%09%09%09%09%09%3CDBCompareType%3EDay%3C%2FDBCompareType%3E%0D%0A%09%09%09\n%09%09%3CDBCompare%3ELessThan%3C%2FDBCompare%3E%0D%0A%09%09%09%09%09%3CDBVersion%3E15%3C%2FDBVersion\n%3E%0D%0A%09%09%09%09%09%3CRepair%3E%3CAntVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath\n%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FAntVersion%3E%3CDBVersion%3E%3CWay%3E%3C%2FWay\n%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FDBVersion%3E\n%3C%2FRepair%3E%0D%0A%09%09%09%09%09%3CActiveDefenseCheck%3ENo%3C%2FActiveDefenseCheck%3E%0D%0A%09%09\n%09%09%3C%2FBaseCheckItem%3E%3CBaseCheckItem%3E%0D%0A%09%09%09%09%09%3CIsCheck%3ENo%3C%2FIsCheck%3E%0D\n%0A++++++++++++++++++++%3CIsMobile%3ENo%3C%2FIsMobile%3E++++%0D%0A%09%09%09%09%09%3CAntiVirusName%3E\n%BF%C9%C5%A3%C3%E2%B7%D1%C9%B1%B6%BE%3C%2FAntiVirusName%3E%0D%0A%09%09%09%09%09%3CIsRuning%3EYes%3C%2FIsRuning\n%3E%0D%0A%09%09%09%09%09%3CSoftCompare%3EMoreThan%3C%2FSoftCompare%3E%0D%0A%09%09%09%09%09%3CSoftVersion\n%3E1.0%3C%2FSoftVersion%3E%0D%0A%09%09%09%09%09%3CDBCompareType%3EDay%3C%2FDBCompareType%3E%0D%0A%09\n%09%09%09%09%3CDBCompare%3ELessThan%3C%2FDBCompare%3E%0D%0A%09%09%09%09%09%3CDBVersion%3E15%3C%2FDBVersion\n%3E%0D%0A%09%09%09%09%09%3CRepair%3E%3CAntVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath\n%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FAntVersion%3E%3CDBVersion%3E%3CWay%3E%3C%2FWay\n%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FDBVersion%3E\n%3C%2FRepair%3E%0D%0A%09%09%09%09%09%3CActiveDefenseCheck%3ENo%3C%2FActiveDefenseCheck%3E%0D%0A%09%09\n%09%09%3C%2FBaseCheckItem%3E%3CBaseCheckItem%3E%0D%0A%09%09%09%09%09%3CIsCheck%3ENo%3C%2FIsCheck%3E%0D\n%0A++++++++++++++++++++%3CIsMobile%3ENo%3C%2FIsMobile%3E++++%0D%0A%09%09%09%09%09%3CAntiVirusName%3EAvast\n%3C%2FAntiVirusName%3E%0D%0A%09%09%09%09%09%3CIsRuning%3EYes%3C%2FIsRuning%3E%0D%0A%09%09%09%09%09%3CSoftCompare\n%3EMoreThan%3C%2FSoftCompare%3E%0D%0A%09%09%09%09%09%3CSoftVersion%3E5.0%3C%2FSoftVersion%3E%0D%0A%09\n%09%09%09%09%3CDBCompareType%3EDay%3C%2FDBCompareType%3E%0D%0A%09%09%09%09%09%3CDBCompare%3ELessThan\n%3C%2FDBCompare%3E%0D%0A%09%09%09%09%09%3CDBVersion%3E15%3C%2FDBVersion%3E%0D%0A%09%09%09%09%09%3CRepair\n%3E%3CAntVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg\n%3E%3C%2FSoftArg%3E%3C%2FAntVersion%3E%3CDBVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath\n%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FDBVersion%3E%3C%2FRepair%3E%0D%0A%09%09%09%09\n%09%3CActiveDefenseCheck%3ENo%3C%2FActiveDefenseCheck%3E%0D%0A%09%09%09%09%3C%2FBaseCheckItem%3E%3CBaseCheckItem\n%3E%0D%0A%09%09%09%09%09%3CIsCheck%3ENo%3C%2FIsCheck%3E%0D%0A++++++++++++++++++++%3CIsMobile%3ENo%3C\n%2FIsMobile%3E++++%0D%0A%09%09%09%09%09%3CAntiVirusName%3EAVG%3C%2FAntiVirusName%3E%0D%0A%09%09%09%09\n%09%3CIsRuning%3EYes%3C%2FIsRuning%3E%0D%0A%09%09%09%09%09%3CSoftCompare%3EMoreThan%3C%2FSoftCompare\n%3E%0D%0A%09%09%09%09%09%3CSoftVersion%3E1.0%3C%2FSoftVersion%3E%0D%0A%09%09%09%09%09%3CDBCompareType\n%3EDay%3C%2FDBCompareType%3E%0D%0A%09%09%09%09%09%3CDBCompare%3ELessThan%3C%2FDBCompare%3E%0D%0A%09%09\n%09%09%09%3CDBVersion%3E15%3C%2FDBVersion%3E%0D%0A%09%09%09%09%09%3CRepair%3E%3CAntVersion%3E%3CWay%3E\n%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FAntVersion\n%3E%3CDBVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg\n%3E%3C%2FSoftArg%3E%3C%2FDBVersion%3E%3C%2FRepair%3E%0D%0A%09%09%09%09%09%3CActiveDefenseCheck%3ENo%3C\n%2FActiveDefenseCheck%3E%0D%0A%09%09%09%09%3C%2FBaseCheckItem%3E%3CBaseCheckItem%3E%0D%0A%09%09%09%09\n%09%3CIsCheck%3ENo%3C%2FIsCheck%3E%0D%0A++++++++++++++++++++%3CIsMobile%3ENo%3C%2FIsMobile%3E++++%0D\n%0A%09%09%09%09%09%3CAntiVirusName%3E%CE%A2%B5%E3%3C%2FAntiVirusName%3E%0D%0A%09%09%09%09%09%3CIsRuning\n%3EYes%3C%2FIsRuning%3E%0D%0A%09%09%09%09%09%3CSoftCompare%3EMoreThan%3C%2FSoftCompare%3E%0D%0A%09%09\n%09%09%09%3CSoftVersion%3E1.0%3C%2FSoftVersion%3E%0D%0A%09%09%09%09%09%3CDBCompareType%3EDay%3C%2FDBCompareType\n%3E%0D%0A%09%09%09%09%09%3CDBCompare%3ELessThan%3C%2FDBCompare%3E%0D%0A%09%09%09%09%09%3CDBVersion%3E15\n%3C%2FDBVersion%3E%0D%0A%09%09%09%09%09%3CRepair%3E%3CAntVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C\n%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FAntVersion%3E%3CDBVersion\n%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg\n%3E%3C%2FDBVersion%3E%3C%2FRepair%3E%0D%0A%09%09%09%09%09%3CActiveDefenseCheck%3ENo%3C%2FActiveDefenseCheck\n%3E%0D%0A%09%09%09%09%3C%2FBaseCheckItem%3E%3CBaseCheckItem%3E%0D%0A%09%09%09%09%09%3CIsCheck%3ENo%3C\n%2FIsCheck%3E%0D%0A++++++++++++++++++++%3CIsMobile%3ENo%3C%2FIsMobile%3E++++%0D%0A%09%09%09%09%09%3CAntiVirusName\n%3ELANDesk+Antivirus%3C%2FAntiVirusName%3E%0D%0A%09%09%09%09%09%3CIsRuning%3EYes%3C%2FIsRuning%3E%0D\n%0A%09%09%09%09%09%3CSoftCompare%3EMoreThan%3C%2FSoftCompare%3E%0D%0A%09%09%09%09%09%3CSoftVersion%3E8\n.0%3C%2FSoftVersion%3E%0D%0A%09%09%09%09%09%3CDBCompareType%3EDay%3C%2FDBCompareType%3E%0D%0A%09%09%09\n%09%09%3CDBCompare%3ELessThan%3C%2FDBCompare%3E%0D%0A%09%09%09%09%09%3CDBVersion%3E15%3C%2FDBVersion\n%3E%0D%0A%09%09%09%09%09%3CRepair%3E%3CAntVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath\n%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FAntVersion%3E%3CDBVersion%3E%3CWay%3E%3C%2FWay\n%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FDBVersion%3E\n%3C%2FRepair%3E%0D%0A%09%09%09%09%09%3CActiveDefenseCheck%3ENo%3C%2FActiveDefenseCheck%3E%0D%0A%09%09\n%09%09%3C%2FBaseCheckItem%3E%3CBaseCheckItem%3E%0D%0A%09%09%3CIsCheck%3EYes%3C%2FIsCheck%3E%0D%0A+++\n+++++%3CIsMobile%3ENo%3C%2FIsMobile%3E++++%0D%0A%09%09%3CAntiVirusName%3EWindows+Defender%3C%2FAntiVirusName\n%3E%0D%0A%09%09%3CIsRuning%3EYes%3C%2FIsRuning%3E%0D%0A%09%09%3CSoftCompare%3EMoreThan%3C%2FSoftCompare\n%3E%0D%0A%09%09%3CSoftVersion%3E1.0%3C%2FSoftVersion%3E%0D%0A%09%09%3CDBCompareType%3EDay%3C%2FDBCompareType\n%3E%0D%0A%09%09%3CDBCompare%3ELessThan%3C%2FDBCompare%3E%0D%0A%09%09%3CDBVersion%3E15%3C%2FDBVersion\n%3E%0D%0A%09%09%3CRepair%3E%3CAntVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C\n%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FAntVersion%3E%3CDBVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl\n%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FDBVersion%3E%3C%2FRepair\n%3E%0D%0A%09%3C%2FBaseCheckItem%3E%3CBaseCheckItem%3E%0D%0A%09%09%09%09%09%3CIsCheck%3ENo%3C%2FIsCheck\n%3E%0D%0A++++++++++++++++++++%3CIsMobile%3ENo%3C%2FIsMobile%3E++++%0D%0A%09%09%09%09%09%3CAntiVirusName\n%3E%CD%A8%D3%C3%C9%B1%B6%BE%C8%ED%BC%FE%3C%2FAntiVirusName%3E%0D%0A%09%09%09%09%09%3CIsRuning%3ENo%3C\n%2FIsRuning%3E%0D%0A%09%09%09%09%09%3CSoftCompare%3E%3C%2FSoftCompare%3E%0D%0A%09%09%09%09%09%3CSoftVersion\n%3E%3C%2FSoftVersion%3E%0D%0A%09%09%09%09%09%3CDBCompareType%3E%3C%2FDBCompareType%3E%0D%0A%09%09%09\n%09%09%3CDBCompare%3E%3C%2FDBCompare%3E%0D%0A%09%09%09%09%09%3CDBVersion%3E%3C%2FDBVersion%3E%0D%0A%09\n%09%09%09%09%3CRepair%3E%3CAntVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl%3E%3CSoftPath%3E%3C%2FSoftPath\n%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FAntVersion%3E%3CDBVersion%3E%3CWay%3E%3C%2FWay%3E%3CUrl%3E%3C%2FUrl\n%3E%3CSoftPath%3E%3C%2FSoftPath%3E%3CSoftArg%3E%3C%2FSoftArg%3E%3C%2FDBVersion%3E%3C%2FRepair%3E%0D%0A\n%09%09%09%09%09%3CActiveDefenseCheck%3ENo%3C%2FActiveDefenseCheck%3E%0D%0A%09%09%09%09%3C%2FBaseCheckItem\n%3E%3CSystemTime%3E0000-00-00+00%3A00%3A00%3C%2FSystemTime%3E%3C%2FOption%3E%0D%0A%09%09%09%09%3CRepair\n%3E%0D%0A%09%09%09%09%09%3CNoInstall%3E%0D%0A%09%09%09%09%09%09%3CWay%3EUrl%3C%2FWay%3E%0D%0A%09%09%09\n%09%09%09%3CUrl%3Eftp%3A%2F%2F192.168.3.214%2Fsoft%2FAnti-virus%2F360sd%2F%3C%2FUrl%3E%0D%0A%09%09%09\n%09%09%09%3CSoftPath%3E%3C%2FSoftPath%3E%0D%0A%09%09%09%09%09%09%3CSoftArg%3E%3C%2FSoftArg%3E%0D%0A%09\n%09%09%09%09%3C%2FNoInstall%3E%0D%0A%09%09%09%09%09%3CAntVersion%3E%0D%0A%09%09%09%09%09%09%3CWay%3ESoft\n%3C%2FWay%3E%0D%0A%09%09%09%09%09%09%3CUrl%3E%3C%2FUrl%3E%0D%0A%09%09%09%09%09%09%3CSoftPath%3E%3C%2FSoftPath\n%3E%0D%0A%09%09%09%09%09%09%3CSoftArg%3E%3C%2FSoftArg%3E%0D%0A%09%09%09%09%09%3C%2FAntVersion%3E%0D%0A\n%09%09%09%09%09%3CDBVersion%3E%0D%0A%09%09%09%09%09%09%3CWay%3ESoft%3C%2FWay%3E%0D%0A%09%09%09%09%09\n%09%3CUrl%3E%3C%2FUrl%3E%0D%0A%09%09%09%09%09%09%3CSoftPath%3E%3C%2FSoftPath%3E%0D%0A%09%09%09%09%09\n%09%3CSoftArg%3E%3C%2FSoftArg%3E%0D%0A%09%09%09%09%09%3C%2FDBVersion%3E%0D%0A%09%09%09%09%3C%2FRepair\n%3E%0D%0A%09%09%09%3C%2FCheckType%3E%23%23%23%7C%7C%23%23%23%3C%3Fxml+version%3D%221.0%22+encoding%3D\n%22gbk%22%3F%3E%3CCheckType%3E%3CInsideName%3ECheckIsFirewallEnable%3C%2FInsideName%3E%3COutsideName\n%3E%D6%F7%BB%FA%B7%C0%BB%F0%C7%BD%BC%EC%B2%E2%3C%2FOutsideName%3E%3CDesc%3E%BC%EC%B2%E9Windows+NT%C4\n%DA%D6%C3%B7%C0%BB%F0%C7%BD%CA%C7%B7%F1%BF%AA%C6%F4%B2%A2%CC%E1%B9%A9%D0%DE%B8%B4%D1%A1%CF%EE%A1%A3%3C\n%2FDesc%3E%3CDLL%3EMsacCheckIsFirewallEnable.dll%3C%2FDLL%3E%3CDLLFunc%3ECheckIsFirewallEnable%3C%2FDLLFunc\n%3E%3COption%3E%3CServerIp%3E%3C%2FServerIp%3E%3CServerPort%3E%3C%2FServerPort%3E%3CSystemTime%3E0000-00-00\n+00%3A00%3A00%3C%2FSystemTime%3E%3C%2FOption%3E%3C%2FCheckType%3E%23%23%23%7C%7C%23%23%23%3C%3Fxml+version\n%3D%221.0%22+encoding%3D%22gbk%22%3F%3E%3CCheckType%3E%3CInsideName%3ECheckGuestUser%3C%2FInsideName\n%3E%3COutsideName%3EGuest%C0%B4%B1%F6%D5%CA%BB%A7%BC%EC%B2%E9%3C%2FOutsideName%3E%3CDesc%3EGuest%C0%B4\n%B1%F6%D5%CA%BB%A7%A3%AC%BF%C9%D2%D4%B7%C3%CE%CA%BC%C6%CB%E3%BB%FA%A3%AC%B5%AB%CA%DC%B5%BD%CF%DE%D6%C6\n%A1%A3%C3%BB%D3%D0%D0%DE%B8%C4%CF%B5%CD%B3%C9%E8%D6%C3%BA%CD%BD%F8%D0%D0%B0%B2%D7%B0%B3%CC%D0%F2%B5%C4\n%C8%A8%CF%DE%A3%AC%D2%B2%C3%BB%D3%D0%B4%B4%BD%A8%D0%DE%B8%C4%C8%CE%BA%CE%CE%C4%B5%B5%B5%C4%C8%A8%CF%DE\n%A3%AC%D6%BB%C4%DC%CA%C7%B6%C1%C8%A1%BC%C6%CB%E3%BB%FA%CF%B5%CD%B3%D0%C5%CF%A2%BA%CD%CE%C4%BC%FE%A1%A3\n%C6%F4%D3%C3GUEST%D5%CA%BB%A7%A3%AC%CE%AA%BA%DA%BF%CD%C8%EB%C7%D6%B4%F2%BF%AA%C1%CB%B7%BD%B1%E3%D6%AE\n%C3%C5%A1%A3%CE%AA%C1%CB%B1%A3%D6%A4%B5%E7%C4%D4%CF%B5%CD%B3%B0%B2%C8%AB%A3%AC%BD%A8%D2%E9%BD%FB%D3%C3\n%B4%CB%D5%CA%BB%A7%A1%A3%3C%2FDesc%3E%3CDLL%3EMsacCheckSecuritySet.dll%3C%2FDLL%3E%3CDLLFunc%3ECheckGuestUser\n%3C%2FDLLFunc%3E%3COption%3E%3CServerIp%3E%3C%2FServerIp%3E%3CServerPort%3E%3C%2FServerPort%3E%3C%2FOption\n%3E%3CRepair%3E%3CFunc%3EForbidGuestUser%3C%2FFunc%3E%3CParam%3E%3C%2FParam%3E%3CIsPrompt%3EYes%3C%2FIsPrompt\n%3E%3CPromptTimeOut%3E120%3C%2FPromptTimeOut%3E%3COffLineValid%3ENo%3C%2FOffLineValid%3E%3CReverify%3ENo\n%3C%2FReverify%3E%3CIsAlarm%3ENo%3C%2FIsAlarm%3E%3C%2FRepair%3E%3C%2FCheckType%3E%23%23%23%7C%7C%23%23\n%23%3C%3Fxml+version%3D%221.0%22+encoding%3D%22gbk%22%3F%3E%3CCheckType%3E%3CInsideName%3ECheckPasswordPolicy\n%3C%2FInsideName%3E%3COutsideName%3E%C3%DC%C2%EB%B2%DF%C2%D4%C9%E8%D6%C3%BC%EC%B2%E9%3C%2FOutsideName\n%3E%3CDesc%3E%BC%EC%B2%E9%C9%E8%B1%B8%B5%C4%CF%B5%CD%B3%C3%DC%C2%EB%CA%C7%B7%F1%C2%FA%D7%E3%D2%AA%C7\n%F3%A3%AC%B1%E3%D3%DA%B1%A3%D6%A4%CF%B5%CD%B3%B5%C4%B0%B2%C8%AB%D0%D4%A1%A3%3C%2FDesc%3E%3CDLL%3EMsacCheckSecuritySet\n.dll%3C%2FDLL%3E%3CDLLFunc%3ECheckPasswordPolicy%3C%2FDLLFunc%3E%3COption%3E%3CServerIp%3E%3C%2FServerIp\n%3E%3CServerPort%3E%3C%2FServerPort%3E%3CPasswordComplexity%3ENo%3C%2FPasswordComplexity%3E%3CPasswordMinLen\n%3E6%3C%2FPasswordMinLen%3E%3CPasswordMaxTime%3E90%3C%2FPasswordMaxTime%3E%3C%2FOption%3E%3CRepair%3E\n%3CFunc%3EChangePasswordPolicy%3C%2FFunc%3E%3CParam%3ENo%7C6%7C90%3C%2FParam%3E%3CIsPrompt%3EYes%3C%2FIsPrompt\n%3E%3CPromptTimeOut%3E120%3C%2FPromptTimeOut%3E%3COffLineValid%3ENo%3C%2FOffLineValid%3E%3CReverify%3ENo\n%3C%2FReverify%3E%3CIsAlarm%3ENo%3C%2FIsAlarm%3E%3C%2FRepair%3E%3C%2FCheckType%3E%23%23%23%7C%7C%23%23\n%23%3C%3Fxml+version%3D%221.0%22+encoding%3D%22gbk%22%3F%3E%3CCheckType%3E%3CInsideName%3ECheckScreenProtection\n%3C%2FInsideName%3E%3COutsideName%3E%C6%C1%C4%BB%B1%A3%BB%A4%C9%E8%D6%C3%BC%EC%B2%E9%3C%2FOutsideName\n%3E%3CDesc%3E%C6%C1%C4%BB%B1%A3%BB%A4%B3%CC%D0%F2%D7%EE%B3%F5%CA%C7%B1%BB%D3%C3%C0%B4%B1%A3%BB%A4%CF\n%D4%CA%BE%C6%F7%B5%C4%A3%AC%CF%D6%D4%DA%B4%F3%BC%D2%B6%E0%D3%C3%C6%C1%B1%A3%B5%C4%C3%DC%C2%EB%C0%B4%B1\n%A3%BB%A4%B5%E7%C4%D4%D4%DA%D6%F7%C8%CB%C0%EB%BF%AA%CA%B1%B2%BB%B1%BB%CB%FB%C8%CB%CA%B9%D3%C3%A3%AC%B4\n%EF%B5%BD%B1%A3%BB%A4%B8%F6%C8%CB%D2%FE%CB%BD%BA%CD%D0%C5%CF%A2%B0%B2%C8%AB%A1%A3%3C%2FDesc%3E%3CDLL\n%3EMsacCheckSecuritySet.dll%3C%2FDLL%3E%3CDLLFunc%3ECheckScreenProtection%3C%2FDLLFunc%3E%3COption%3E\n%3CServerIp%3E%3C%2FServerIp%3E%3CServerPort%3E%3C%2FServerPort%3E%3CProtectionActive%3EYes%3C%2FProtectionActive\n%3E%3CWaitTime%3E10%3C%2FWaitTime%3E%3CUsePassword%3EYes%3C%2FUsePassword%3E%3C%2FOption%3E%3CRepair\n%3E%3CFunc%3EChangeScreenProtectionSet%3C%2FFunc%3E%3CParam%3EYes%7C10%7CYes%3C%2FParam%3E%3CIsPrompt\n%3EYes%3C%2FIsPrompt%3E%3CPromptTimeOut%3E120%3C%2FPromptTimeOut%3E%3COffLineValid%3ENo%3C%2FOffLineValid\n%3E%3CReverify%3ENo%3C%2FReverify%3E%3CIsAlarm%3ENo%3C%2FIsAlarm%3E%3C%2FRepair%3E%3C%2FCheckType%3E\n%23%23%23%7C%7C%23%23%23%3C%3Fxml+version%3D%221.0%22+encoding%3D%22gbk%22%3F%3E%3CCheckType%3E%3CInsideName\n%3ECheckVulnerablePassword%3C%2FInsideName%3E%3COutsideName%3E%C8%F5%BF%DA%C1%EE%D5%CA%BB%A7%BC%EC%B2\n%E9%3C%2FOutsideName%3E%3CDesc%3E%C8%F5%BF%DA%C1%EE%BC%B4%C8%DD%D2%D7%B1%BB%C6%C6%D2%EB%B5%C4%C3%DC%C2\n%EB%A3%AC%CF%F1%BC%F2%B5%A5%B5%C4%CA%FD%D7%D6%D7%E9%BA%CF+%C8%E712345%BB%F2%D5%DF%D3%EB%D5%CA%BA%C5%CF\n%E0%CD%AC%B5%C4%CA%FD%D7%D6%D7%E9%BA%CF%B3%C9%B5%C4%C3%DC%C2%EB%A3%AC%B5%C8%B6%BC%CA%C7%C8%F5%BF%DA%C1\n%EE%A1%A3%D3%B5%D3%D0%C8%F5%BF%DA%C1%EE%D5%CA%BB%A7%B5%C4%BB%E1%B8%F8%BA%DA%BF%CD%CC%E1%B9%A9%BC%AB%B4\n%F3%B5%C4%B1%E3%C0%FB%A3%AC%BD%F8%B6%F8%D3%B0%CF%EC%CF%B5%CD%B3%B5%C4%B0%B2%C8%AB%A1%A3%3C%2FDesc%3E\n%3CDLL%3EMsacCheckSecuritySet.dll%3C%2FDLL%3E%3CDLLFunc%3ECheckVulnerablePassword%3C%2FDLLFunc%3E%3COption\n%3E%3CExceptUser%3E%3C%2FExceptUser%3E%3CPasswordDict%3Epassword.dict%3C%2FPasswordDict%3E%3CServerIp\n%3E%3C%2FServerIp%3E%3CServerPort%3E%3C%2FServerPort%3E%3CIsWebRq%3E0%3C%2FIsWebRq%3E%3C%2FOption%3E\n%3CRepair%3E%3CFunc%3ESetUserPassword%3C%2FFunc%3E%3CParam%3E%3C%2FParam%3E%3CIsPrompt%3EYes%3C%2FIsPrompt\n%3E%3CPromptTimeOut%3E120%3C%2FPromptTimeOut%3E%3COffLineValid%3ENo%3C%2FOffLineValid%3E%3CDisableAccountCheck\n%3EYes%3C%2FDisableAccountCheck%3E%3CReverify%3ENo%3C%2FReverify%3E%3CIsAlarm%3ENo%3C%2FIsAlarm%3E%3C\n%2FRepair%3E%3C%2FCheckType%3E%23%23%23%7C%7C%23%23%23\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"Res\"\r\n\r\n\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"LastAuthID\"\r\n\r\n0\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"TradeRunTime\"\r\n\r\n%23activeC%3AIsiSetLangue%3A0.091%23activeC%3ASetServerIp%3A0.15%23activeC%3AGetDeviceInfo%3A0.17%23ajax\n%3Agetdeviceinfoprocess%3A0.041%23ajax%3Afast_auth%3A0.016%23ajax%3Aget_authflag%3A0.032%23activeC%3ACheckDomainUser\n%3A0.266%23ajax%3Anet_auth%3A0.085%23ajax%3Aget_server_time%3A0.026%23ajax%3Agetcheckpolicy%3A0.022%23activeC\n%3AGetDir%3A0.085%23activeC%3AReadStrFromFile%3A0.19%23activeC%3ACheckUnlawfulConnectOut%3A0.179%23activeC\n%3ACheckSystemProcess%3A0.167%23ajax%3Agetcheckitem%3A0.023%23activeC%3ACheckUnlawfulConnectOut%3A0.262\n%23ajax%3Agetcheckitem%3A0.018%23ajax%3Aget_asc_ipport%3A0.025%23activeC%3ACheckPatch%3A0.381%23ajax\n%3Agetcheckitem%3A0.015%23activeC%3ACheckAntiVirusSoft%3A0.85%23ajax%3Agetcheckitem%3A0.015%23activeC\n%3ACheckIsFirewallEnable%3A0.154%23ajax%3Agetcheckitem%3A0.016%23activeC%3ACheckGuestUser%3A0.162%23ajax\n%3Agetcheckitem%3A0.017%23activeC%3ACheckPasswordPolicy%3A0.156%23ajax%3Agetcheckitem%3A0.017%23activeC\n%3ACheckScreenProtection%3A0.148%23ajax%3Agetcheckitem%3A0.016%23activeC%3ACheckVulnerablePassword%3A1\n.267\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"tokenkey\"\r\n\r\n27e7f032037670502c818d9d7bfde1a9\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW--", ParameterType.RequestBody);
                        */
                        IRestResponse response = client.Execute(request);
                        Console.WriteLine(response.Content);
                        TextBox_send_result.Dispatcher.BeginInvoke(updateRichTextBoxAction, TextBox_send_result, response.Content);
                        faild_count++;
                    }

                    Lable_state.Dispatcher.BeginInvoke(updateAction, Lable_state, "等待轮回中...");

                    //如果测试连续失败超过10轮,下次请求为30分钟后
                    if (faild_count >= max_faild_count)
                    {
                        Lable_state.Dispatcher.BeginInvoke(updateAction, Lable_state, "连续失败超过10轮,30分钟后进行下次请求");
                        //初始化等待时间
                        wait_time = 1800;
                        Thread.Sleep(1800 * 1000);
                        faild_count = 0;
                    }
                    else
                    {
                        //初始化等待时间
                        wait_time = round_time;
                        Thread.Sleep(round_time * 1000);
                    }

                }
                else
                {
                    Thread.Sleep(1000);
                }
            }

        }

        
    }

    public class ParamItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string value { get; set; }
    }

    public class HeaderObject
    {
        /// <summary>
        /// 
        /// </summary>
        public List<ParamItem> header { get; set; }
    }

    public class ParamObject
    {
        /// <summary>
        /// 
        /// </summary>
        public List<ParamItem> parameter { get; set; }
    }

}
