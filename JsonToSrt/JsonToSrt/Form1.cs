using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json.Linq;
using JsonPath;
using Newtonsoft.Json;

namespace JsonToSrt
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //打开源文件夹
        private void button2_Click(object sender, EventArgs e)
        {
            this.folderBrowserDialog1.ShowDialog();
            this.textBox1.Text = this.folderBrowserDialog1.SelectedPath;
        }

        //打开目标文件夹
        private void button3_Click(object sender, EventArgs e)
        {
            this.folderBrowserDialog2.ShowDialog();
            this.textBox2.Text = this.folderBrowserDialog2.SelectedPath;
        }
        //转换
        private void button1_Click(object sender, EventArgs e)
        {
            this.button1.Enabled = false;
            if (this.textBox1.Text == "" || this.textBox2.Text == "")
            {
                MessageBox.Show("请选择json所在的目录和目标目录");
                this.button1.Enabled = true;
                return;
            }
            DirectoryInfo di1 = new DirectoryInfo(this.textBox1.Text);
            if (!di1.Exists)
            {
                MessageBox.Show("您选择的json目录不存在");
                this.button1.Enabled = true;
                return;
            }

            DirectoryInfo di2 = new DirectoryInfo(this.textBox2.Text);
            if (!di2.Exists)
            {
                MessageBox.Show("您选择的目标目录不存在");
                this.button1.Enabled = true;
                return;
            }
            List<FileInfo> fileList = GetAllFiles(di1);

            int errorCount = 0;
            int sumCount = 0;
            foreach (var file in fileList.Where(t => t.Extension.ToLower().Contains("json")))
            {
                writeLog("===============================", 0);
                writeLog("开始处理文件" + file.FullName, 0);
                try
                {
                    handleFile(file, di2);
                }
                catch (Exception ex)
                {
                    this.button1.Enabled = true;
                    writeLog("文件读取异常，跳过。" + file.FullName + "  " + ex.Message, 1);
                    errorCount++;
                    continue;
                }
                sumCount++;

            }
            MessageBox.Show("转换完成。转换文件：" + sumCount + " 错误文件：" + errorCount);
            this.button1.Enabled = true;
        }

        private void handleFile(FileInfo file, DirectoryInfo targetDi)
        {
            string fileName = file.Name;
            string txtFileName = fileName + ".txt";
            writeLog("开始读取：" + file.Name, 0);
            StreamReader sr = new StreamReader(file.FullName, Encoding.UTF8);
            String line;
            StringBuilder sBuilder = new StringBuilder();
            while ((line = sr.ReadLine()) != null)
            {
                sBuilder.Append(line);
            }

            writeLog(sBuilder.ToString(), 0);
            string txtContent = "";

            JObject json = (JObject)JsonConvert.DeserializeObject(sBuilder.ToString());

            var context = new JsonPathContext { ValueSystem = new JsonNetValueSystem() };
            var results = context.SelectNodes(json, "$..results.*").Select(node => node.Value);
            if (results != null && results.Count() > 0)
            {
                for (int i = 0; i < results.Count(); i++)
                {
                    JObject jo = (JObject)results.ElementAt(i);
                    //行号
                    txtContent += (i + 1) + "\r\n";
                    //获取alternatives
                    JObject jo0 = (JObject)context.SelectNodes(jo, "alternatives.*").Select(node => node.Value).Single();
                    //提取开始时间
                    JArray jastMin = (JArray)context.SelectNodes(jo0, "timestamps.*").Select(node => node.Value).First();
                    string st = jastMin[1].ToString();
                    string stTime = GetDateTimeBySeconds(Double.Parse(st));
                    txtContent += stTime + " -->";
                    //提取结束时间
                    JArray jastMax = (JArray)context.SelectNodes(jo0, "timestamps.*").Select(node => node.Value).Last();
                    string et = jastMax[2].ToString();
                    string etTime = GetDateTimeBySeconds(Double.Parse(et));
                    txtContent += etTime + " \r\n";
                    //提取内容
                    string transcript = context.SelectNodes(jo0, "transcript").Select(node => node.Value).First().ToString();
                    txtContent += transcript;
                    txtContent += "\r\n\r\n";
                }
            }
            writeTargetFile(txtContent, targetDi, file);

        }
        /// <summary>  
        /// 根据秒数得到timeofday  
        /// </summary>  
        /// <param name="seconds"></param>  
        /// <returns></returns>  
        public string GetDateTimeBySeconds(double seconds)
        {
            string time = DateTime.Parse(DateTime.Now.ToString("1970-01-01 00:00:00.0")).AddSeconds(seconds).TimeOfDay.ToString();
            return time.Length > 11 ? time.Substring(0, 8) + "," + time.Substring(9, 3) : time;
        }

        static List<FileInfo> FileList;
        //递归查找文件夹下面所有的文件
        public static List<FileInfo> GetAllFiles(DirectoryInfo dir)
        {
            FileList = new List<FileInfo>();
            FileInfo[] allFile = dir.GetFiles();
            foreach (FileInfo fi in allFile)
            {
                FileList.Add(fi);
            }
            DirectoryInfo[] allDir = dir.GetDirectories();
            foreach (DirectoryInfo d in allDir)
            {
                GetAllFiles(d);
            }
            return FileList;
        }
        //写入日志信息
        public static void writeLog(string log, int logType)
        {
            if (null != log && !"".Equals(log))
            {
                log = DateTime.Now + " " + log;
                string filePath = "";
                if (logType == 0)
                {
                    filePath = Environment.CurrentDirectory + "\\正常日志.txt";
                }
                else
                {
                    filePath = Environment.CurrentDirectory + "\\异常日志.txt";
                }


                StreamWriter sw = File.AppendText(filePath);

                sw.WriteLine(log);

                sw.Flush();

                sw.Close();

            }
        }

        //写入目标文件
        public static void writeTargetFile(string txtContent, DirectoryInfo targetDi, FileInfo newFileInfo)
        {
            if (null != txtContent && !"".Equals(txtContent))
            {
                string path = targetDi.FullName + "\\" + newFileInfo.Name.Replace("json", "srt");
                FileInfo fi = new FileInfo(path);
                if (fi.Exists)
                {
                    fi.Delete();
                }

                StreamWriter sw = File.CreateText(path);

                sw.WriteLine(txtContent);

                sw.Flush();

                sw.Close();

            }
        }
    }
}
