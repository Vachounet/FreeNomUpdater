using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace FreeNom
{
    public class FreeNom
    {
        private const string _tokenURL = "https://my.freenom.com/clientarea.php";
        private const string _updateToken = "https://my.freenom.com/clientarea.php?action=domaindetails&id=";
        private const string _doLogin = "https://my.freenom.com/dologin.php";
        private const string _updateURL = "https://my.freenom.com/clientarea.php?managedns=&domainid=";

        private const string _ipURL = "https://api.ipify.org/";

        public string CurrentToken;

        public CookieContainer CookieCont;

        public string HTMLToParse;

        public List<SubDomain> SubDomains;

        public string CurrentIP;

        public string UserName
        {
            get
            {
                return ConfigurationManager.AppSettings["username"];
            }
        }

        public string Password
        {
            get
            {
                return ConfigurationManager.AppSettings["password"];
            }
        }

        public List<string> DomainIDs
        {
            get
            {
                return ConfigurationManager.AppSettings["domainIDs"].Split(';').ToList();
            }
        }

        public List<string> DomainNames
        {
            get
            {
                return ConfigurationManager.AppSettings["domainNames"].Split(';').ToList();
            }
        }



        public FreeNom()
        {
            GetCurrentIP();

            if (string.IsNullOrEmpty(CurrentIP))
                return;

            SubDomains = new List<SubDomain>();

            CurrentToken = GetToken(_tokenURL, null, "/html/body/section/div/div/input");

            DoLogin();

            Logout();
        }

        private void Logout()
        {
            var request = (HttpWebRequest)WebRequest.Create(_ipURL);
            request.CookieContainer = CookieCont;
            request.Method = "GET";

            var response = (HttpWebResponse)request.GetResponse();

            response.Close();
        }

        private void GetCurrentIP()
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(_ipURL);

                request.Method = "GET";

                var response = (HttpWebResponse)request.GetResponse();

                CurrentIP = new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to get current IP !");
                throw;
            }
        }

        private string GetToken(string url, CookieContainer cookie, string path)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);

            if (cookie != null)
                request.CookieContainer = cookie;
            else
                request.CookieContainer = new CookieContainer();

            request.Method = "GET";

            request.AllowAutoRedirect = true;
            var response = (HttpWebResponse)request.GetResponse();

            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            HTMLToParse = responseString;

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(responseString);

            string token = string.Empty;

            foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//*[@name=\"token\"]"))
            {
                token = link.Attributes["value"].Value;
                break;

            }

            response.Close();

            return token;
        }



        private void DoLogin()
        {
            var requestLogin = (HttpWebRequest)WebRequest.Create(_doLogin);

            var postData = "username=" + UserName;
            postData += "&password=" + Password;
            postData += "&token=" + CurrentToken;
            var data = Encoding.ASCII.GetBytes(postData);

            requestLogin.Method = "POST";
            requestLogin.ContentType = "application/x-www-form-urlencoded";
            CookieCont = new CookieContainer();
            requestLogin.CookieContainer = CookieCont;
            requestLogin.ContentLength = data.Length;

            using (var stream = requestLogin.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var responseLogin = (HttpWebResponse)requestLogin.GetResponse();

            var responseStringLogin = new StreamReader(responseLogin.GetResponseStream()).ReadToEnd();

            CurrentToken = GetToken(_updateURL, CookieCont, "/html/body/section/section/section/section/section/input");

            GetDomains(HTMLToParse);
        }

        private void GetDomains(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            string token = string.Empty;

            foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//input[contains(@name,\"records\")]"))
            {

                string[] splitted = link.Attributes["name"].Value.Split('[');

                string id = splitted[1].Replace("]", "");
                SubDomain currentDomain = SubDomains.Where(d => d.ID.Equals(id)).FirstOrDefault();

                if (currentDomain == null)
                {
                    currentDomain = new SubDomain();
                    currentDomain.ID = id;
                    SubDomains.Add(currentDomain);
                }

                string filter = splitted[2].Replace("]", "");

                switch (filter)
                {
                    case "line":
                        currentDomain.Line = link.Attributes["value"].Value;
                        break;
                    case "ttl":
                        currentDomain.TTL = link.Attributes["value"].Value;
                        break;
                    case "value":
                        currentDomain.Value = link.Attributes["value"].Value;
                        break;
                    case "name":
                        currentDomain.Name = link.Attributes["value"].Value;
                        break;
                    case "type":
                        currentDomain.Type = link.Attributes["value"].Value;
                        break;
                }

            }


            var postData = "&token=" + CurrentToken;
            postData += "&dnsaction=modify";

            string tmp = string.Empty;
            foreach (SubDomain dom in SubDomains)
            {
                Console.WriteLine("Updating " + dom.Name + " to " + CurrentIP);
                tmp += "&records[" + dom.ID + "][line]=" + dom.Line;
                tmp += "&records[" + dom.ID + "][type]=" + dom.Type;
                tmp += "&records[" + dom.ID + "][name]=" + dom.Name;
                tmp += "&records[" + dom.ID + "][ttl]=" + dom.TTL;
                tmp += "&records[" + dom.ID + "][value]=" + CurrentIP;//+ dom.Value;

            }

            postData = postData + tmp;

            var requestLogin = (HttpWebRequest)WebRequest.Create(_updateURL);


            var data = Encoding.ASCII.GetBytes(postData);

            requestLogin.Method = "POST";
            requestLogin.ContentType = "application/x-www-form-urlencoded";
            // Cookie = new CookieContainer();
            requestLogin.CookieContainer = CookieCont;
            requestLogin.ContentLength = data.Length;

            using (var stream = requestLogin.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var responseLogin = (HttpWebResponse)requestLogin.GetResponse();

            var responseStringLogin = new StreamReader(responseLogin.GetResponseStream()).ReadToEnd();
        }

    }

    public class SubDomain
    {
        public string Name;

        public string ID;

        public string TTL;

        public string Line;

        public string Value;

        public string Type;


        public SubDomain() { }



    }
}
