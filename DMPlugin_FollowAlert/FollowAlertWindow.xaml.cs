using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace DMPlugin_FollowAlert
{
    /// <summary>
    /// FollowAlertWindow.xaml 的交互逻辑
    /// </summary>
    public partial class FollowAlertWindow : Window, INotifyPropertyChanged
    {
        private const string keyword = "[关注人]";
        private const string defaultTpl = "感谢 " + keyword + " 的关注~~";
        private const int sleepTime = 5000;// 5s

        private FollowAlertMain main;

        private string _outputTpl;
        public string outputTpl
        {
            get { return _outputTpl; }
            set { if(_outputTpl == value) return; _outputTpl = value; onChanged(nameof(outputTpl)); }
        }
        private string _outputStr;
        public string outputStr
        {
            get { return _outputStr; }
            set { if(_outputStr == value) return; _outputStr = value; onChanged(nameof(outputStr)); }
        }

        internal Thread processThread;
        private Queue<string> followedNames = new Queue<string>();

        internal FollowAlertWindow(FollowAlertMain main)
        {
            this.main = main;
            InitializeComponent();
            Closing += (sender, e) => { e.Cancel = true; Hide(); };

            try
            {// 刷新文件
                Directory.CreateDirectory(FollowAlertMain.configPath);
                writeFile(string.Empty);
            }
            catch(Exception) { }
            try
            {
                outputTpl = File.ReadAllText(FollowAlertMain.tplPath);
            }
            catch(Exception)
            {
                outputTpl = defaultTpl;
            }

            processThread = new Thread(process)
            {
                Name = "FollowAlertOutputThread",
                IsBackground = true
            };
            processThread.Start();
        }

        private void process()
        {
            while(true)
            {
                if(followedNames.Count > 0)
                {
                    var name = followedNames.Dequeue();
                    var opt = getOutput(name);
                    outputStr = opt;
                    writeFile(opt);
                    Thread.Sleep(sleepTime);
                }
                else
                {
                    if(outputStr != string.Empty)
                    {
                        outputStr = string.Empty;
                        writeFile(string.Empty);
                    }
                    Thread.Sleep(200);
                }
            }
        }

        internal void addName(string name)
        {
            followedNames.Enqueue(name);
        }

        private void writeFile(string str)
        {
            try
            { File.WriteAllText(FollowAlertMain.outputPath, str); }
            catch(Exception ex)
            { main.Log("写文件错误：" + ex.Message); }
        }

        private string getOutput(string name)
        {
            return outputTpl.Replace(keyword, name);
        }

        private void OpenPath(object sender, RoutedEventArgs e)
        {
            if(!File.Exists(FollowAlertMain.outputPath))
            {
                Directory.CreateDirectory(FollowAlertMain.configPath);
                File.WriteAllText(FollowAlertMain.outputPath, string.Empty);
            }
            string argument = "/select,\"" + FollowAlertMain.outputPath + "\"";
            Process.Start("explorer.exe", argument);
        }

        private void TestOutput(object sender, RoutedEventArgs e)
        {
            this.addName(string.Empty);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void onChanged(string name)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
    }
}
