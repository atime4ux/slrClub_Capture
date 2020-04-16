using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Data;
using System.Drawing;
using System.Text;
using System.Net;
using System.Windows.Forms;

namespace slrClub_Capture
{
    public partial class Form1 : Form
    {
        libCommon.clsUtil objUtil = new libCommon.clsUtil();

        System.Threading.Thread mainThread;//게시판 검색
        Queue history = new Queue(50);
        NotifyIcon myNotify;

        string srchWrd;
        string captureWrd;
        string exceptWrd;
        string[] srchWrd_arr;
        string[] captureWrd_arr;
        string[] exceptWrd_arr;
        
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Application.ApplicationExit += new EventHandler(Application_ApplicationExit);
            
            srchWrd = objUtil.getAppCfg("srchWrd");
            captureWrd = objUtil.getAppCfg("captureWrd");
            exceptWrd = objUtil.getAppCfg("exceptWrd");
            
            if (srchWrd == null || captureWrd == null || exceptWrd == null)
            {
                MessageBox.Show("검색어 오류");
                this.Close();
            }
            else
            {
                srchWrd_arr = objUtil.Split(srchWrd, "|");
                captureWrd_arr = objUtil.Split(captureWrd, "|");
                exceptWrd_arr = objUtil.Split(exceptWrd, "|");
            }

            myNotify = new NotifyIcon();
            myNotify.BalloonTipText = "최소화...";
            myNotify.BalloonTipTitle = "자게캡쳐";
            myNotify.BalloonTipIcon = ToolTipIcon.Info;
            myNotify.Icon = this.Icon;
            myNotify.MouseClick += new MouseEventHandler(myNotify_MouseClick);

            radioButton1.Checked = true;
        }

        void Application_ApplicationExit(object sender, EventArgs e)
        {
            if (mainThread != null)
                mainThread.Abort();
        }

        //트레이에서 복귀
        void myNotify_MouseClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        //자유게시판 파싱
        private void parsingSLRfree(HtmlAgilityPack.HtmlDocument doc)
        {
            string itemBaseURL = "http://www.slrclub.com/bbs/vx2.php?id=free&no=";
            string num = "";
            string title;
            string filepath = textBox1.Text + System.DateTime.Now.ToString("yyMMdd") + "\\";
            bool saveHTML;
            int i;
            int j;

            bool isSkip = false;

            HtmlAgilityPack.HtmlNode obj = doc.GetElementbyId("slrct");
            if (obj == null)
            {
                //사이트 접속 불가
            }
            else
            {
                for(i=0; i<obj.SelectNodes("table").Count; i++)
                {
                    if (isSkip)
                        break;

                    HtmlAgilityPack.HtmlNode table = obj.SelectNodes("table")[i];
                    
                    for(j=0; j<table.SelectNodes("tr").Count; j++)
                    {
                        HtmlAgilityPack.HtmlNode tr = table.SelectNodes("tr")[j];

                        if (tr.SelectNodes("td").Count == 6)
                        {
                            if (!tr.SelectNodes("td")[0].InnerText.Equals("공지"))
                            {
                                num = tr.SelectNodes("td")[0].InnerText;
                                title = tr.SelectNodes("td")[1].InnerText;
                                title = title.Substring(title.IndexOf("-->") + 3).Replace("&nbsp;", "");

                                //기존 게시물인지 검사
                                if (chkQueue(num))
                                {
                                    //검색어 검사
                                    if (chkSrchWrd(title, srchWrd_arr).Length > 0)
                                    {
                                        //제외단어 검사
                                        if (chkSrchWrd(title, exceptWrd_arr).Length == 0)
                                        {
                                            //날짜와 토픽으로 폴더 생성
                                            libMyUtil.clsFile.MakeDir(filepath + chkSrchWrd(title, srchWrd_arr) + "\\");
                                            saveHTML = false;

                                            if (radioButton1.Checked)
                                            {
                                                if (chkSrchWrd(title, captureWrd_arr).Length > 0)
                                                {
                                                    string captureServerAddress = objUtil.getAppCfg("captureServerAddress");
                                                    if (captureServerAddress == null)
                                                        captureServerAddress = "";
                                                    if (captureServerAddress.Length > 0)
                                                    {

                                                        //웹 호출로 캡쳐
                                                        using (WebClient forCapture = new WebClient())
                                                        {
                                                            string urlParam1 = "url=" + libMyUtil.clsWeb.encURL(itemBaseURL + num, "UTF-8");
                                                            string urlParam2 = "downloadPath=" + libMyUtil.clsWeb.encURL(filepath + chkSrchWrd(title, srchWrd_arr) + "\\" + num + ".png", "UTF-8");
                                                            string captureURL = "http://" + objUtil.getAppCfg("captureServerAddress") + "/capture/Capture.aspx?" + urlParam1 + "&" + urlParam2;

                                                            forCapture.DownloadStringAsync(new Uri(captureURL));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        //libMyUtil.clsWeb.WebpageCapture_noReturn(itemBaseURL + num, filepath + chkSrchWrd(title, srchWrd_arr) + "\\" + num + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                                                    }
                                                    saveHTML = true;
                                                }

                                                clsSrchContents objSrchContents = new clsSrchContents(itemBaseURL, num, title, filepath + chkSrchWrd(title, srchWrd_arr) + "\\", saveHTML);
                                                objSrchContents.DoIt();
                                            }
                                            else if (radioButton2.Checked)
                                            {
                                                //캡쳐
                                                libMyUtil.clsWeb.WebpageCapture_noReturn(itemBaseURL + num, filepath + chkSrchWrd(title, srchWrd_arr) + "\\" + num + ".png", System.Drawing.Imaging.ImageFormat.Png);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    isSkip = true;
                                    break;//이전에 처리한 게시물이므로 다음 과정은 스킵
                                }
                            }
                        }
                    }

                    if (num.Length > 0)
                        break;//게시물 검색 완료 후 루프 탈출
                }
            }
        }
        
        /// <summary>
        /// 검색어가 있으면 해당 검색어, 없으면 "" 리턴
        /// </summary>
        /// <param name="str">검사할 문자열</param>
        /// <param name="srchStr">검색어가 들어간 배열</param>
        private string chkSrchWrd(string str, string[] srchStr)
        {
            int i;

            if (srchStr != null)
            {
                for (i = 0; i < srchStr.Length; i++)
                {
                    if (srchStr[i].Length > 0)
                    {
                        if (str.IndexOf(srchStr[i]) > -1)
                        {
                            return srchStr[i];
                        }
                    }
                }
            }

            return "";
        }

        //html 다운로드
        private void startCapture()
        {
            WebRequest request = null;
            WebResponse response = null;
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();

            string siteURL = "http://www.slrclub.com/bbs/zboard.php?id=free";
            int interval = (int)(numericUpDown1.Value);

            while (true)
            {
                try
                {
                    request = WebRequest.Create(siteURL);
                    response = request.GetResponse();
                    doc.Load(response.GetResponseStream());
                }
                catch (Exception ex)
                {
                    libMyUtil.clsFile.writeLog(ex.ToString());
                }

                if (response != null)
                {
                    response.Close();
                    response = null;
                }

                try
                {
                    if (doc.DocumentNode.HasChildNodes)
                        parsingSLRfree(doc);
                }
                catch (Exception ex)
                {
                    libMyUtil.clsFile.writeLog(ex.ToString());
                }

                System.Threading.Thread.Sleep(interval * 1000);
            }
        }

        //캡쳐 시작
        private void button1_Click_1(object sender, EventArgs e)
        {
            if (textBox1.Text.Length == 0)
                return;

            if (button1.Text.Equals("시작"))
            {
                button1.Text = "정지";
                button2.Enabled = false;
                numericUpDown1.Enabled = false;

                mainThread = new System.Threading.Thread(new System.Threading.ThreadStart(startCapture));
                mainThread.Start();
            }
            else
            {
                try
                {
                    mainThread.Abort();
                }
                catch { }

                button1.Text = "시작";
                button2.Enabled = true;
                numericUpDown1.Enabled = true;
            }
        }
        
        /// <summary>
        /// 중복 값이 없으면 큐에 저장 후 true, 있으면 false
        /// </summary>
        private bool chkQueue(string num)
        {
            foreach (string obj in history)
            {
                if (obj.Equals(num))
                {
                    return false;
                }
            }

            history.Enqueue(num);
            if (history.Count > 50)
            {
                history.Dequeue();
            }

            return true;
        }
        
        //폴더 지정
        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog objFBD = new FolderBrowserDialog();
            string tmp;

            objFBD.SelectedPath = Application.StartupPath;

            if (objFBD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                tmp = objFBD.SelectedPath + "\\";
                textBox1.Text = tmp.Replace("\\\\", "\\");
            }
        }

        //크기 변경시 이벤트
        private void Form1_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == this.WindowState)
            {
                myNotify.Visible = true;
                this.Hide();
            }
            else if (FormWindowState.Normal == this.WindowState)
            {
                myNotify.Visible = false;
            }
        }

        //폴더로 이동
        private void button3_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Length > 0)
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                p = System.Diagnostics.Process.Start("explorer.exe", textBox1.Text);
            }
        }
    }
}
