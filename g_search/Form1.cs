using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Media;

namespace g_search
{
    public partial class Form1 : Form
    {
        string nextURL = null;
        bool isFirstPage = true;
        int numPagesToProcess = 0;
        string logFile = @"F:\rejeesh\V4U\log.txt";
        public Form1()
        {
            InitializeComponent();

            txtRefFolder.Text = @"F:\rejeesh\V4U\Files";
            txtDestFolder.Text = @"F:\rejeesh\V4U\TodaysFiles";

            Trace.AutoFlush = true;
            Trace.Listeners.Add(new TextWriterTraceListener(logFile));

            this.webBrowser1.Navigate("https://www.google.com");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string query = this.txtCriteria.Text;
            string queryEncoded = HttpUtility.UrlEncode(query);
            nextURL = "https://www.google.com/search?q=" + queryEncoded;
            this.webBrowser1.Navigate(nextURL);
            isFirstPage = true;
        }

        static IEnumerable<HtmlElement> ElementsByClass(HtmlDocument doc, string className)
        {
            foreach (HtmlElement e in doc.All)
                if (e.GetAttribute("className") == className)
                    yield return e;
        }




        bool downloadFile(string url)
        {
            string fileName = Path.GetFileName(url);
            fileName = HttpUtility.UrlDecode(fileName);

            string expectedExt = "." + this.txtFileType.Text;

            try
            {

                if (Path.GetExtension(fileName).ToLower() == expectedExt.ToLower())
                {
                    downloadDirect(url);
                }
                else
                {
                    downloadFromStream(url);
                }
            }
            catch(Exception e)
            {
                Trace.TraceInformation("Error " + fileName);
            }

            return true;
        }

        private bool downloadFromStream(string url)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            String fileName = "";

            try
            {

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    var fn = response.Headers["Content-Disposition"].Split(new string[] { "=" }, StringSplitOptions.None)[1];
                    string basePath = this.txtDestFolder.Text;
                    var responseStream = response.GetResponseStream();

                    fileName = fn.Replace('"', ' ');
                    fileName = fileName.Trim();

                    string expectedExt = "." + this.txtFileType.Text;
                    if (Path.GetExtension(fileName).ToLower() != expectedExt.ToLower())
                    {
                        return true;
                    }

                    //If the file is already downloaded in destination folder, then skip it
                    string path = Path.Combine(basePath, fileName);
                    if (File.Exists(path)) return true;


                    //If the file is already downloaded in reference folder, then skip it
                    string refPath = Path.Combine(this.txtRefFolder.Text, fileName);
                    if (File.Exists(refPath)) return true;



                    using (var fileStream = File.Create(Path.Combine(basePath, fileName)))
                    {
                        responseStream.CopyTo(fileStream);
                    }

                    Trace.TraceInformation("Copied " + fileName);
                }
            }
            catch (Exception e)
            {
                Trace.TraceInformation("Error: " + fileName);
            }

            return true;
        }

        private bool downloadDirect(string url)
        {

            string fileName = Path.GetFileName(url);
            fileName = HttpUtility.UrlDecode(fileName);

            //If the file already exists in the destination folder, then skip it
            string destPath = Path.Combine(this.txtDestFolder.Text, fileName);

            if (File.Exists(destPath)) return true;


            //IF file is already downloaded in reference folder, then skip it
            string refPath = Path.Combine(this.txtRefFolder.Text, fileName);

            if (File.Exists(refPath)) return true;

            try
            {
                WebClient Client = new WebClient();
                Client.DownloadFile(url, destPath);
            }
            catch (Exception e)
            {
                Trace.TraceInformation("Error " + fileName);
                return true;
            }

            Trace.TraceInformation("Copied " + fileName);

            return true;

        }


        string getNextPage()
        {

            HtmlElement elHref = webBrowser1.Document.GetElementById("pnnext");

            if (elHref == null) return "";

            return elHref.GetAttribute("href");

        }

        int getNumberOfResultPages()
        {
            int val = 0;
            HtmlElement elResult = webBrowser1.Document.GetElementById("resultStats");

            if (elResult == null) return 0;

            string[] res = elResult.InnerText.Split(' ');


            int n = Array.IndexOf(res, "results");

            if (n > 0)
            {
                string s = res[n - 1];
                s = s.Replace(",", "");
                val = Int32.Parse(s);
            }

            return val;
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            //if (String.IsNullOrEmpty(getNextPage()))
            //    return;


            if (isFirstPage == true)
            {
                isFirstPage = false;

                if (getNumberOfResultPages() == 0)
                    return;

                if (getNumberOfResultPages() >= 100)
                {
                    numPagesToProcess = 10;
                }

                if (getNumberOfResultPages() < 100)
                {
                    numPagesToProcess = 5;
                }

                Trace.TraceInformation(txtCriteria.Text);
            }


            if (numPagesToProcess <= 0)
            {
                MessageBox.Show("Finished");
                SystemSounds.Beep.Play();
                return;
            }
            nextURL = getNextPage();

            //Get all links to download

            //HtmlElementCollection elc = webBrowser1.Document.GetElementsByTagName("span");
            HtmlElementCollection elc = webBrowser1.Document.GetElementsByTagName("a");

            foreach (HtmlElement elem in elc)
            {
                //string fileType = txtFileType.Text;
                //if (txtFileType.Text.ToLower() == "docx")
                //{
                //    fileType = "doc";
                //}

                //if (txtFileType.Text.ToLower() == "pptx")
                //{
                //    fileType = "ppt";
                //}
                //string pattern = "[" + fileType.ToUpper() + "]";

                //if (pattern == elem.InnerText)
                {
                    //HtmlElement h = elem.NextSibling;
                    string link = elem.GetAttribute("href");

                    if (String.IsNullOrEmpty(link)) continue;

                    Uri u = new Uri(link);
                    String s = u.Host;
                    if (s.Contains("google") || s.Contains("youtube")) continue;

                    if (String.IsNullOrEmpty(link)) continue;

                    downloadFile(link);

                }

            }

            numPagesToProcess--;
            if(String.IsNullOrEmpty(nextURL))
            {
                MessageBox.Show("Finished. No next page.");
                return;
            }
                this.webBrowser1.Navigate(nextURL);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void toolTip1_Popup(object sender, PopupEventArgs e)
        {

        }
    }
}
