using System;
using System.Collections.Generic;
using System.Text;

using System.Windows.Forms;
using System.IO;
using System.Net;

namespace slrClub_Capture
{
    public class clsSrchContents
    {
        System.Threading.Thread t1;

        private string itemBaseURL;
        private string ctNum;
        private string title;
        private string savePath;
        private bool saveHTML;

        public clsSrchContents(string ContentsBaseURL, string ContentsNum, string Title, string ContentsSavePath, bool SaveHTML)
        {
            this.itemBaseURL = ContentsBaseURL;
            this.ctNum = ContentsNum;
            this.title = Title;
            this.savePath = ContentsSavePath;
            this.saveHTML = SaveHTML;

            t1 = new System.Threading.Thread(new System.Threading.ThreadStart(srchContents));
        }

        public void DoIt()
        {
            t1.Start();
        }

        //컨텐츠 검사
        private void srchContents()
        {
            StringBuilder strBuilder = new StringBuilder();
            WebRequest request = null;
            WebResponse response = null;

            string imgURL;
            string finalPath;

            HtmlAgilityPack.HtmlDocument doc_ct = new HtmlAgilityPack.HtmlDocument();
            HtmlAgilityPack.HtmlNode obj_ct;

            try
            {
                request = WebRequest.Create(itemBaseURL + ctNum);
                response = request.GetResponse();
                doc_ct.Load(response.GetResponseStream());
            }
            catch (Exception ex)
            {
                libMyUtil.clsFile.writeLog(ex.ToString());
            }

            if (response != null)
                response.Close();

            try
            {
                if (doc_ct.DocumentNode.HasChildNodes)
                {
                    if (doc_ct.GetElementbyId("userct").SelectNodes("table")[0] != null)
                    {
                        obj_ct = doc_ct.GetElementbyId("userct").SelectNodes("table")[0];
                        foreach (HtmlAgilityPack.HtmlNode tr in obj_ct.SelectNodes("tr"))
                        {
                            foreach (HtmlAgilityPack.HtmlNode td in tr.SelectNodes("td"))
                            {
                                if (td.SelectNodes("img") != null && td.SelectNodes("img").Count > 0)
                                {
                                    //이미지
                                    foreach (HtmlAgilityPack.HtmlNode img in td.SelectNodes("img"))
                                    {
                                        imgURL = string.Empty;

                                        if (img.Attributes["href"] != null)
                                            imgURL = img.Attributes["href"].Value;
                                        if (img.Attributes["src"] != null)
                                            imgURL = img.Attributes["src"].Value;

                                        if (imgURL.Length > 0 && Uri.CheckSchemeName(new Uri(imgURL).Scheme))//URL유효성 검사
                                        {
                                            finalPath = libMyUtil.clsFile.GetUniqueFileName(savePath, ctNum + "_" + libMyUtil.clsFile.GetFileNameFromUrl(imgURL), false);
                                            libMyUtil.clsFile.DownloadFromUrl(imgURL, "http://www.slrclub.com", finalPath);
                                        }
                                    }
                                }

                                //동영상
                                if (td.SelectNodes("embed") != null && td.SelectNodes("embed").Count > 0)
                                {
                                    strBuilder.Append("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">");
                                    strBuilder.Append("<table>");
                                    strBuilder.Append("<tr><td>" + this.title + "</td></tr>");

                                    strBuilder.Append(obj_ct.InnerHtml);
                                    strBuilder.Append("</table>");
                                    finalPath = libMyUtil.clsFile.GetUniqueFileName(savePath, ctNum + ".html", false);
                                    writeContents(finalPath, strBuilder.ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                libMyUtil.clsFile.writeLog(ex.ToString());
            }
        }

        //텍스트 기록
        private void writeContents(string savePath, string contents)
        {
            using (StreamWriter srWriter = new StreamWriter(savePath))
            {
                srWriter.Write(contents);
                srWriter.Close();
            }
        }
    }
}
