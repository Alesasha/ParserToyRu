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
            var csvHeader = "'regionName','productName','price','priceOld','inStock','breadCrumbs','productHref','imageHrefs'";
            string csvDump;

            var LinkToStartPage = "https://www.toy.ru/catalog/boy_transport/?count=45&filterseccode%5B0%5D=transport&PAGEN_8=";
            var CookieRostov = "Cookie:BITRIX_SM_city=61000001000";
            var CookieMoscow = "Cookie:BITRIX_SM_city=77000000000";
            var CityCookie = CookieRostov;

            int pageNum = 1;
            var ListOfLinks = new List<string>();
            while (true)
            {
                var href = LinkToStartPage + pageNum.ToString();
                var pageContent = GetPageFromSite(href, CityCookie);
                var parser = new HtmlParser();
                var doc = parser.ParseDocument(pageContent);
                var docs = doc.QuerySelectorAll("[itemprop=\"url\"]");
                foreach (var l in docs)
                {
                    var lnn = l.GetAttribute("Content");
                    if (lnn != null)
                        ListOfLinks.Add(lnn);
                }
                if (pageContent.Contains("След")) {
                    pageNum++;
                    continue;
                }
                break;
            }
            csvDump = csvHeader + '\n';
            foreach (var r in ListOfLinks)
            {
                var p = GetProductInfo(r, CityCookie);
                csvDump += $"'{p.regionName}','{p.productName}','{p.price}','{p.priceOld}','{p.inStock}','{p.breadCrumbs}','{p.productHref}','{p.imageHrefs}'\n";
            }

            File.WriteAllText(@"d:\Rostov.CSV",csvDump); 

            //var prin = GetProductInfo(ListOfLinks[0], CityCookie);//ListOfLinks[0]
            var div1 = 10; //parser.ParseFragment("meta itemprop=<meta itemprop=\"url\" content=");
        }
        
        static string GetPageFromSite(string href,string Cookie)
        {
            string page="";
            var request = (HttpWebRequest)WebRequest.Create(href);
            request.Timeout = 10000;
            request.ReadWriteTimeout = 10000;
            request.ContinueTimeout = 10000;
            request.Method = "GET";
            request.Headers.Add(Cookie);
            //request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; rv:78.0) Gecko/20100101 Firefox/78.0"; //GetUserAgent(ish);// 
            request.AutomaticDecompression = DecompressionMethods.GZip;
            try
            {
                var response = request.GetResponse(); //HttpWebResponse
                var reader = new System.IO.StreamReader(response.GetResponseStream(), Portable.Text.Encoding.GetEncoding(1251));
                page = reader.ReadToEnd();
                reader.Close();
            }
            catch (Exception ex)
            {
                Thread.Sleep(3000);
            }
            return page;
        }
        class productInfo
        {
            public productInfo(string ph) { this.productHref = ph; }
            public string productHref;
            public string regionName;
            public string breadCrumbs;
            public string productName;
            public string price;
            public string priceOld;
            public string inStock;
            public string imageHrefs;
        }
        static productInfo GetProductInfo(string href, string Cookie)
        {
            var pInfo = new productInfo(href);
            var productPage = GetPageFromSite(href,Cookie);
            var parser = new HtmlParser();
            var document = parser.ParseDocument(productPage);
            pInfo.regionName = document.QuerySelector("[data-src=\"#region\"]").TextContent.Replace("\t","").Trim();
            var bc = document.QuerySelectorAll(".breadcrumb > a,.breadcrumb >span");
            for (var i = 0; i < bc.Count()-1; i++) 
            {
                if(i==0)
                    pInfo.breadCrumbs = bc[i].TextContent;
                else
                    pInfo.breadCrumbs += ">"+bc[i].TextContent;
            }
            pInfo.productName = document.QuerySelector(".detail-name").GetAttribute("Content");
            pInfo.price = document.QuerySelector(".price").TextContent;
            pInfo.priceOld = document.QuerySelector(".old-price")?.TextContent;
            var imgCollection = document.QuerySelectorAll(".card-slider-for a").Select(x=>x.GetAttribute("href").Replace("?_cvc=1647661175","")).ToList();  //[0].GetAttribute("href");

            for (var i = 0; i < imgCollection.Count; i++)
            {
                if (i == 0)
                    pInfo.imageHrefs = imgCollection[i];
                else
                    pInfo.imageHrefs += " " + imgCollection[i];
            }
            if (productPage.Contains("Товар есть в наличии"))
                pInfo.inStock = "В наличии";
            else
                pInfo.inStock = "Отсутствует";

            //var bb = bc;
            return pInfo;
        }


    }
}