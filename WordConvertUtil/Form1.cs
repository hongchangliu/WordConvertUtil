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

namespace WordConvertUtil
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
            foreach (var file in fileList.Where(t => t.Extension.ToLower().Contains("txt")))
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
                    return;
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

            JArray ja = (JArray)JsonConvert.DeserializeObject(sBuilder.ToString());
            int c = ja.Count();
            if (c == 0)
            {
                writeLog("无数据，故跳过：" + file.Name, 0);
                return;
            }
            //角色集合
            List<string> roleList = new List<string>();
            var context = new JsonPathContext { ValueSystem = new JsonNetValueSystem() };
            //bool bgEquals0 = equals0(ja, context);需求变更
            bool bgEquals0 = true;

            double wordBdMin = Double.MaxValue;
            double edMax = 0;
            string wordNameLast = "";

            if (bgEquals0)//bg都等于0，就将wordbd*10最小值 +worded*10最大值+ wordsName的组合写入txt
            {
                #region 处理等于0
                for (int i = 0; i < ja.Count(); i++)
                {
                    JObject jo = (JObject)ja[i];
                    double wordbdTemp = 0;
                    if (i == 0)
                    {
                        try
                        {
                            JsonPathNode[] pathNodes = context.SelectNodes(jo, "$..wordBg");
                            foreach (JsonPathNode pathNode in pathNodes)
                            {
                                wordbdTemp = double.Parse(pathNode.Value.ToString()) * 10;
                                wordBdMin = wordBdMin < wordbdTemp ? wordBdMin : wordbdTemp;
                            }
                        }
                        catch (Exception)
                        {
                            //wordbdTemp = 0;
                        }
                    }
                    else
                    {
                        try
                        {
                            JsonPathNode pathNode = context.SelectNodes(jo, "$..bg").Single();
                            wordBdMin = double.Parse(pathNode.Value.ToString());
                        }
                        catch (Exception)
                        {
                            wordBdMin = 0;
                        }
                    }


                    try
                    {
                        JsonPathNode edNode = context.SelectNodes(jo, "$..ed").Single();
                        edMax = double.Parse(edNode.Value.ToString());

                    }
                    catch (Exception)
                    {
                        edMax = 0;
                    }

                    //获取speaker
                    string speaker = "";
                    try
                    {
                        speaker = context.SelectNodes(jo, "$..speaker").First().Value.ToString();
                    }
                    catch (Exception)
                    {
                        speaker = "";
                    }
                    string word = (speaker.Equals("1") ? "客服" : "客户") + ":" + context.SelectNodes(jo, "$..onebest").Single().Value.ToString();

                    txtContent += wordBdMin + "\t" + edMax + "\t" + word + "\t";

                    double bgDouble = 0;
                    if (i != 0)
                    {
                        try
                        {
                            string bg = context.SelectNodes(jo, "$..bg").Single().Value.ToString();
                            bgDouble = double.Parse(bg);
                        }
                        catch (Exception)
                        {

                        }
                    }

                    //解析wordsResultList
                    JArray wordsResultList = (JArray)context.SelectNodes(jo, "$..wordsResultList").Single().Value;
                    for (int j = 0; j < wordsResultList.Count; j++)
                    {
                        JObject joResult = (JObject)wordsResultList[j];
                        JsonPathNode pathNode;
                        try
                        {
                            pathNode = context.SelectNodes(joResult, "$..wordBg").Single();
                            txtContent += double.Parse(pathNode.Value.ToString()) * 10 + bgDouble + "\t";
                        }
                        catch (Exception)
                        {
                        }
                        try
                        {
                            pathNode = context.SelectNodes(joResult, "$..wordEd").Single();
                            txtContent += double.Parse(pathNode.Value.ToString()) * 10 + bgDouble + " ";
                        }
                        catch (Exception)
                        {
                        }
                        try
                        {
                            pathNode = context.SelectNodes(joResult, "$..wordsName").Single();
                            txtContent += pathNode.Value.ToString() + "\t";
                        }
                        catch (Exception)
                        {
                        }
                    }
                    txtContent += "\r\n";
                }

                #endregion
            }
            else//只要bg有不等于0 ，就去对应的bg和ed，onebest，写入txt
            {
                #region 处理不等于0
                for (int i = 0; i < ja.Count; i++)
                {
                    JObject jo = (JObject)ja[i];
                    string bd = context.SelectNodes(jo, "$..bg").Single().Value.ToString();
                    if (null != bd && !"0".Equals(bd))
                    {
                        try
                        {
                            wordBdMin = double.Parse(context.SelectNodes(jo, "$..bg").Single().Value.ToString());
                        }
                        catch (Exception)
                        {
                            wordBdMin = 0;
                        }
                        try
                        {
                            edMax = double.Parse(context.SelectNodes(jo, "$..ed").Single().Value.ToString());
                        }
                        catch (Exception)
                        {
                            edMax = 0;
                        }
                        try
                        {
                            wordNameLast = context.SelectNodes(jo, "$..onebest").Single().Value.ToString();
                        }
                        catch (Exception)
                        {
                            wordNameLast = "";
                        }
                        break;
                    }
                }
                #endregion

            }


            //txtContent += wordBdMin + " " + edMax + " " + wordNameLast + "\t";

            writeTargetFile(txtContent, targetDi, file);

        }

        //是否等于0
        private bool equals0(JArray ja, JsonPathContext context)
        {
            foreach (JObject jo in ja)
            {
                string bg = context.SelectNodes(jo, "$..bg").Single().Value.ToString();
                if (!bg.Equals("0"))
                {
                    return false;
                }
            }
            return true;
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
                string path = targetDi.FullName + "\\" + newFileInfo.Name.Replace("json", "txt");
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
