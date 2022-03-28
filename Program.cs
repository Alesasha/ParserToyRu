using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
//using System.Data.Linq;
using AngleSharp;
using AngleSharp.Html.Parser;
//using Newtonsoft.Json;
using System.Threading;
using AngleSharp.Text;
using System.Runtime.InteropServices;
//using System.Data.Linq.Mapping;
using System.Globalization;

namespace ParserToyRu
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var LinkToStartPage = "https://www.toy.ru/catalog/boy_transport/";
            var request = (HttpWebRequest)WebRequest.Create(LinkToStartPage);
            request.Timeout = 10000;
            request.ReadWriteTimeout = 10000;
            request.ContinueTimeout = 10000;
            request.Method = "GET";
            //request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; rv:78.0) Gecko/20100101 Firefox/78.0"; //GetUserAgent(ish);// 
            request.AutomaticDecompression = DecompressionMethods.GZip;
            var str = ""; 
            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                var reader = new System.IO.StreamReader(response.GetResponseStream(), Portable.Text.Encoding.GetEncoding(1251));
                str = reader.ReadToEnd();
                reader.Close();
            }
            catch (Exception ex)
            {
                Thread.Sleep(3000);
            }


            var parser = new HtmlParser();
            var doc = parser.ParseDocument(str);
            var docs = doc.QuerySelectorAll("[itemprop=\"url\"]");
            var ListOfLinks = new List<string>();
            foreach(var l in docs) {
               var lnn = l.GetAttribute("Content");
               if(lnn!=null)
                    ListOfLinks.Add(lnn);
            }

            var div1 = 10; //parser.ParseFragment("meta itemprop=<meta itemprop=\"url\" content=");
        }
    }
}