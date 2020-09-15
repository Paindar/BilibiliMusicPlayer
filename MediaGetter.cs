using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BilibiliMusicPlayer
{
    // av, bv转化算法由知乎大佬 mcfx 提供(https://www.zhihu.com/question/381784377)
    // 由 @我叫以赏 优化代码（指整理）(https://zhuanlan.zhihu.com/p/117358823)
    class PageContentException : Exception
    {
        public string page;
        public string msg;
        public PageContentException(string page, string msg)
        {
            this.page = page;
            this.msg = msg;
        }
    }
    class MediaGetter
    {
		private static HttpClientHandler handler = new HttpClientHandler
		{
			ClientCertificateOptions = ClientCertificateOption.Automatic,
            AutomaticDecompression = DecompressionMethods.All
        };
		HttpClient client = new HttpClient(handler);
        private static string HEADER_TAG = "window.__playinfo__=";
        private static string FEET_TAG = "</script>";

        public MediaGetter()
        {
            setHeaderValue("user-agent", "Ginn no Kagi/1.1.3");
            setHeaderValue("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            setHeaderValue("accept-encoding", "gzip, deflate, br");
            setHeaderValue("accept-language", "zh-CN,zh;q=0.9,en;q=0.8,ja;q=0.7");
        }
        private void setHeaderValue(string key, string value)
        {
            if (client.DefaultRequestHeaders.Contains(key))
                client.DefaultRequestHeaders.Remove(key);
            client.DefaultRequestHeaders.Add(key, value);
        }
        public async Task<string> GetAudioTitle(string id)
        {
            string HEAD = "<title data-vue-meta=\"true\">";
            string FEET = "_哔哩哔哩";
            string url = "https://www.bilibili.com/" + id;
            client.DefaultRequestHeaders.Remove("referer");
            var res = await client.GetAsync(url);
            byte[] bytes = await res.Content.ReadAsByteArrayAsync();
            string text = Encoding.UTF8.GetString(bytes); 
            int head = text.IndexOf(HEAD);
            if (head == -1)
            {
                return "";
            }
            int feet = text.IndexOf(FEET, head+HEAD.Length);
            if (feet == -1)
            {
                return text.Substring(head+HEAD.Length);
            }
            return text.Substring(head + HEAD.Length, feet-head-HEAD.Length);
        }
        public string getAudioURL(string url)
        {
            client.DefaultRequestHeaders.Remove("referer");
            var res = client.GetAsync(url).Result;
            byte[] bytes = res.Content.ReadAsByteArrayAsync().Result;
            string text = Encoding.UTF8.GetString(bytes);
            int head = text.IndexOf(HEADER_TAG);
            if(head==-1)
            {
                throw new PageContentException(text, "Cannot find HEADER_TAG");
            }
            int feet = text.IndexOf(FEET_TAG, head);
            if (feet == -1)
            {
                throw new PageContentException(text, "Cannot find FEET_TAG");
            }
            string json_raw = text.Substring(head+HEADER_TAG.Length, feet-head- HEADER_TAG.Length);
            JObject json = null;
            try
            {
                json = JObject.Parse(json_raw);
            }
            catch(JsonReaderException)
            {
                using(FileStream fs = new FileStream("json_error.json",System.IO.FileMode.Create))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.Write(json_raw);
                    }
                }
                Console.WriteLine("Failed to parse json file!");
                Environment.Exit(1);
            }
            return json["data"]["dash"]["audio"][0]["baseUrl"].Value<string>();
        }

        public async Task downloadFile(string homeURL, string url, string name)
        {
            int blockSize = 1024 * 512;
            int begin = 0;
            int end = blockSize - 1;
            int flag = 0;
            new FileStream(name, FileMode.Create).Close();
            setHeaderValue("referer", homeURL);
            FileStream fs = new FileStream(name, FileMode.Create);
            while (true)
            {
                setHeaderValue("range", string.Format("bytes={0}-{1}", begin, end));
                HttpResponseMessage res = await client.GetAsync(url);
                  
                if (res.StatusCode == HttpStatusCode.Forbidden)
                {
                    fs.Close();
                    throw new HttpRequestException("403 Forbidden");
                }
                else if (res.StatusCode != HttpStatusCode.RequestedRangeNotSatisfiable)
                {
                    begin = end + 1;
                    end = end + blockSize;
                }
                else
                {
                    setHeaderValue("range", string.Format("bytes={0}-", end+1));
                    res = await client.GetAsync(url);
                    flag = 1;
                }
                Console.WriteLine(string.Format("download: {0} - {1}", begin, end));
                fs.Write(await res.Content.ReadAsByteArrayAsync());
                if(flag==1)
                {
                    fs.Close();
                    break;
                }
            }
        }
        public async Task DownloadAV(string avid, string cachePath)
        {
            string url = "https://www.bilibili.com/" + avid;
            string audioURL = getAudioURL(url);
            await downloadFile(url, audioURL, cachePath);
        }
        public async Task DownloadBV(string bvid, string cachePath)
        {
            string url = "https://www.bilibili.com/" + bvid;
            string audioURL = getAudioURL(url);
            await downloadFile(url, audioURL, cachePath);
        }

        internal void SetUserAgent(string v)
        {
            setHeaderValue("user-agent", v);
        }
    }
}
