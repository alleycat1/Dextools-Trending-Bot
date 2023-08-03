using CefSharp;
using CefSharp.WinForms;
using Knapcode.TorSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows.Forms;

namespace DextoolsTrending
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            var browserList = new List<string> { "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/114.0", "Mozilla/5.0 (Macintosh; Intel Mac OS X 13_4) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.5 Safari/605.1.15", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36 Edg/114.0.1823.51" };
            Random rnd = new Random();
            int index = rnd.Next(browserList.Count);
            var settings = new CefSettings();
            //settings.CefCommandLineArgs.Add("headless");
            settings.CefCommandLineArgs.Add("disable-gpu");
            settings.CefCommandLineArgs.Add("no-sandbox");
            settings.CefCommandLineArgs.Add("disable-dev-shm-usage");
            settings.CefCommandLineArgs.Add("disable-software-rasterizer");
            settings.CefCommandLineArgs.Add("disable-extensions");
            settings.CefCommandLineArgs.Add("mute-audio");
            settings.CefCommandLineArgs.Add("disable-setuid-sandbox");
            settings.CefCommandLineArgs.Add("disable-application-cache");
            settings.CefCommandLineArgs.Add("media-cache-size=1");
            settings.CefCommandLineArgs.Add("disk-cache-size=1");
            settings.CefCommandLineArgs.Add("aggressive-cache-discard");
            settings.CefCommandLineArgs.Add("start-maximized");
            settings.CefCommandLineArgs.Add("disable-infobars");
            settings.CefCommandLineArgs.Add("disable-notifications");
            settings.CefCommandLineArgs.Add("disable-offline-auto-reload");
            settings.CefCommandLineArgs.Add("disable-offline-auto-reload-visible-only");
            settings.CefCommandLineArgs.Add("blink-settings=imagesEnabled=false");
            //settings.CefCommandLineArgs.Add("disable-image-loading");
            settings.UserAgent = browserList[index];
            Cef.Initialize(settings);
            InitializeComponent();

        }


        bool isTorRunning = false;
        private async void button3_Click(object sender, EventArgs e)
        {
            if (button3.Text == "Stop...")
            {
                checkBox1.Checked = false;
                checkBox1.Enabled = false;
                button3.Text = "Start Trending Loop";
                button3.Enabled = true;
            }
            else
            {
                button3.Enabled = false;
                // configure
                var settings = new TorSharpSettings
                {
                    ZippedToolsDirectory = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "TorZipped"),
                    ExtractedToolsDirectory = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "TorExtracted"),
                    PrivoxySettings = { Port = 1337 },
                    TorSettings =
                    {
                      SocksPort = 1338,
                      ControlPort = 1339,
                      ControlPassword = "",
                    },
                };

                // download tools
                await new TorSharpToolFetcher(settings, new HttpClient()).FetchAsync();
                // execute
                var proxy = new TorSharpProxy(settings);
                var handler = new HttpClientHandler
                {
                    Proxy = new WebProxy(new Uri("http://localhost:" + settings.PrivoxySettings.Port))
                };
                var httpClient = new HttpClient(handler);

                if (isTorRunning == false)
                {
                    await proxy.ConfigureAndStartAsync();

                    isTorRunning = true;
                    button3.Text = "Tor Proxy Started";
                }


                await Cef.UIThreadTaskFactory.StartNew(delegate
                    {
                        string ip = "127.0.0.1";
                        string port = "1337";
                        var rc = chromiumWebBrowser1.GetBrowser().GetHost().RequestContext;
                        var dict = new Dictionary<string, object>
                    {
                    { "mode", "fixed_servers" },
                    { "server", "" + ip + ":" + port + "" }
                    };
                        string error;
                        bool success = rc.SetPreference("proxy", dict, out error);

                    });
                await chromiumWebBrowser1.LoadUrlAsync(textBox1.Text);
                checkBox1.Enabled = true;
                button3.Enabled = false;

                while (checkBox1.Checked == false)
                {
                    Application.DoEvents();
                }
                button3.Text = "Stop...";
                button3.Enabled = true;
                checkBox1.Enabled = false;

                while (checkBox1.Checked == true && button3.Text == "Stop...")
                {

                    await chromiumWebBrowser1.LoadUrlAsync(textBox1.Text);
                    var scriptFavorite = @"document.querySelector('.favorite-button button').click();";
                    chromiumWebBrowser1.ExecuteScriptAsync(scriptFavorite);
                    var scriptContractCopy = @"document.querySelector('a.text-muted').click();";
                    chromiumWebBrowser1.ExecuteScriptAsync(scriptContractCopy);
                    //var scriptScanner = @"document.querySelector('a img[alt=""scanner logo""]').closest('a').click();";
                    //chromiumWebBrowser1.ExecuteScriptAsync(scriptScanner);
                    var scriptShare = @"document.querySelectorAll('a[href=""javascript:""][placement=""top""]')[0].click();";
                    chromiumWebBrowser1.ExecuteScriptAsync(scriptShare);
                    await proxy.GetNewIdentityAsync();
                    //chromiumWebBrowser1.Reload(true);

                }
            }

        }

    }
}
