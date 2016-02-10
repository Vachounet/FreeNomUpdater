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
        /// <summary>
        /// BackOffice URL
        /// </summary>
        private const string _tokenURL = "https://my.freenom.com/clientarea.php";

        /// <summary>
        /// Random URL to get token from
        /// </summary>
        private const string _updateToken = "https://my.freenom.com/clientarea.php?action=domaindetails&id=1012676620";

        /// <summary>
        /// Auth URL
        /// </summary>
        private const string _doLogin = "https://my.freenom.com/dologin.php";

        /// <summary>
        /// Update URL - Where to send updated date
        /// </summary>
        private const string _updateURL = "https://my.freenom.com/clientarea.php?managedns=vache.ml&domainid=1012676620";

        /// <summary>
        /// Get IP from IPIFY service
        /// </summary>
        private const string _ipURL = "https://api.ipify.org/";

        /// <summary>
        /// Current user token (connected or net)
        /// Used for queries
        /// </summary>
        public string CurrentToken;

        /// <summary>
        /// Current Cookies
        /// USerd for queries
        /// </summary>
        public CookieContainer CookieCont;

        /// <summary>
        /// Where to get domains from
        /// </summary>
        public string HTMLToParse;

        /// <summary>
        /// Populated found subdomain for current
        /// domain
        /// </summary>
        public List<SubDomain> SubDomains;

        /// <summary>
        /// Current real IP
        /// </summary>
        public string CurrentIP;

        /// <summary>
        /// Get username from config
        /// </summary>
        public string UserName
        {
            get
            {
                return ConfigurationManager.AppSettings["username"];
            }
        }

        /// <summary>
        /// Get password from config
        /// </summary>
        public string Password
        {
            get
            {
                return ConfigurationManager.AppSettings["password"];
            }
        }

        /// <summary>
        /// Get domains ids from config
        /// </summary>
        public List<string> DomainIDs
        {
            get
            {
                return ConfigurationManager.AppSettings["domainIDs"].Split(';').ToList();
            }
        }

        /// <summary>
        /// Get domains names from config
        /// </summary>
        public List<string> DomainNames
        {
            get
            {
                return ConfigurationManager.AppSettings["domainNames"].Split(';').ToList();
            }
        }

        /// <summary>
        /// Launch process
        /// </summary>
        public FreeNom()
        {
            //Let's populate current ip
            GetCurrentIP();

            //Stop process if IP not found
            if (string.IsNullOrEmpty(CurrentIP))
                return;

            SubDomains = new List<SubDomain>();

            //Get a new token to wonnect with
            CurrentToken = GetToken(_tokenURL, null, "/html/body/section/div/div/input");

            if (!string.IsNullOrEmpty(CurrentToken))
            {
                //Connect to FreeNom with provided credentials
                DoLogin();

                //Get subdomains
                GetDomains(HTMLToParse);

                //Update subdomains
                if (SubDomains.Count > 1)
                {
                    UpdateData();
                    //AddDomains();
                }

                //Logout from FreeNom
                Logout();
            }


        }

        /// <summary>
        /// End session
        /// </summary>
        private void Logout()
        {
            var request = (HttpWebRequest)WebRequest.Create(_ipURL);
            request.CookieContainer = CookieCont;
            request.Method = "GET";

            var response = (HttpWebResponse)request.GetResponse();

            response.Close();
        }

        /// <summary>
        /// Get current IP from choosen service
        /// </summary>
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

        /// <summary>
        /// Get a new token
        /// </summary>
        /// <param name="url">URL to get token from (doesn't work with all pages)</param>
        /// <param name="cookie">Use a cookie if needed (url is an private page)</param>
        /// <param name="path">XPath string to search token input value in HTML response</param>
        /// <returns></returns>
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

        /// <summary>
        /// Get a new session and populate cookie
        /// </summary>
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
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="html">HTML string</param>
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

        }

        private void AddDomains()
        {
            var postData = "&token=" + CurrentToken;
            postData += "&dnsaction=add";

            string tmp = string.Empty;
            foreach (SubDomain dom in SubDomains)
            {
                Console.WriteLine("Adding " + dom.Name + " to " + CurrentIP);
                tmp += "&addrecord[" + dom.ID + "][type]=" + dom.Type;
                tmp += "&addrecord[" + dom.ID + "][name]=" + dom.Name;
                tmp += "&addrecord[" + dom.ID + "][ttl]=" + dom.TTL;
                tmp += "&addrecord[" + dom.ID + "][value]=" + CurrentIP;//+ dom.Value;
                tmp += "&addrecord[" + dom.ID + "][priority]=";
                tmp += "&addrecord[" + dom.ID + "][port]=";
                tmp += "&addrecord[" + dom.ID + "][weight]=";
                tmp += "&addrecord[" + dom.ID + "][forward_type]=1";

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

        private void UpdateData()
        {
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

    /// <summary>
    /// Store subdomains
    /// </summary>
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
