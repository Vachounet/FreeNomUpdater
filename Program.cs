using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace FreeNom
{
    class Program
    {
        static void Main(string[] args)
        {
            //SSL
            ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;

            //Start process
            FreeNom updater = new FreeNom();
        }

    }

}
