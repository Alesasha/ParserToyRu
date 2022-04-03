using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using System.Threading;

namespace ParserToyRu
{
    class Program
    {
        public enum CMD : int
        {
            StandBy = 0, GetProdInfo = 1, Exit = 2
        }
        class productInfo
        {
            public productInfo(string ph) { productHref = ph; }
            public string productHref;
            public string regionName;
            public string breadCrumbs;
            public string productName;
            public string price;
            public string priceOld;
            public string inStock;
            public string imageHrefs;
        }
        class taskInfo
        {
            public taskInfo(int n,string link, string c) { productNumber = n; productLink = link; cookie = c; badAttempts = 0; command = CMD.StandBy; isResultReady = false;}
            public Task task;
            public int productNumber;
            public string productLink;
            public string cookie;
            public int badAttempts;
            public productInfo result;
            public CMD command;
            public bool isResultReady;
        }

        static void Main(string[] args)
        {
            var csvHeader = "'regionName','productName','price','priceOld','inStock','breadCrumbs','productHref','imageHrefs'";
            string csvDump;
            string fileName = @"d:\Moscow_test003.CSV"; // csv - file name and path
            var GoodsPerPage = 45;
            var LinkToStartPage = $"https://www.toy.ru/catalog/boy_transport/?count={GoodsPerPage}&filterseccode%5B0%5D=transport&PAGEN_8=";
            var CookieRostov = "Cookie:BITRIX_SM_city=61000001000";
            var CookieMoscow = "Cookie:BITRIX_SM_city=77000000000";
            var CityCookie = CookieMoscow;
            int CPinLOL = 0;  //Current position in ListOfLinks
            int ppCount = 0;  //Processed products counter
            var taskInfoList = new List<taskInfo>(); // Active task list
            int MaxThreads = 30; // Maximum Threads

            int pageNum = 1;
            int TotalPages = 0;

            var ListOfLinks = new List<string>();

            Stopwatch Timer = new Stopwatch();
            Timer.Start();

            //Starting product list collector
            var PLcollector = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var href = LinkToStartPage + pageNum.ToString();
                    var pageContent = GetPageFromSite(href, CityCookie);
                    var parser = new HtmlParser();
                    var doc = parser.ParseDocument(pageContent);
                    var docs = doc.QuerySelectorAll("[itemprop=\"url\"]");
                    if (pageNum == 1)
                        TotalPages = doc.QuerySelectorAll(".page-link").Select(x => x.InnerHtml).Where(x => Int32.TryParse(x, out int xd)).Select(x => Int32.Parse(x)).Max();

                    foreach (var l in docs)
                    {
                        var lnn = l.GetAttribute("Content");
                        if (lnn != null)
                            ListOfLinks.Add(lnn);
                    }
                    if (pageContent.Contains("След."))
                    {
                        pageNum++;
                        continue;
                    }
                    break;
                }
            },TaskCreationOptions.LongRunning);

            while (ListOfLinks.Count == 0)
                Thread.Sleep(10);

            var Results = new productInfo[TotalPages * GoodsPerPage]; // Results array

            while (true)
            {
                var j = CPinLOL;
                for (var i = 0; i < taskInfoList.Count; i++)
                {
                    if (taskInfoList[i].isResultReady)
                    {
                        ppCount++;
                        Console.WriteLine($"Product №={ppCount} ({taskInfoList[i].result.productName})");
                        Results[taskInfoList[i].productNumber] = taskInfoList[i].result;
                        taskInfoList[i].result = null;
                        taskInfoList[i].isResultReady = false;
                        taskInfoList[i].badAttempts = 0;

                        if (CPinLOL < ListOfLinks.Count)
                        {
                            taskInfoList[i].productNumber = CPinLOL;
                            taskInfoList[i].productLink = ListOfLinks[CPinLOL++];
                            taskInfoList[i].command = CMD.GetProdInfo;
                            break;
                        }
                    }
                }
                if ((j == CPinLOL) && (CPinLOL < ListOfLinks.Count) && (taskInfoList.Count < MaxThreads))
                {
                    var nti = new taskInfo(CPinLOL, ListOfLinks[CPinLOL++], CityCookie);
                    taskInfoList.Add(nti);
                    nti.command = CMD.GetProdInfo;
                    nti.task =  Task.Factory.StartNew(()=>ProductInfoCollector(nti), TaskCreationOptions.LongRunning);
                }
                if (ppCount == ListOfLinks.Count && PLcollector.IsCompleted is true)
                    break;
            }
            foreach (var ti in taskInfoList) ti.command = CMD.Exit;

            Timer.Stop();
            Console.WriteLine($"Elapsed time: {Timer.ElapsedMilliseconds:### ### ##0}ms ({Timer.Elapsed:hh\\:mm\\:ss\\.ffff}), Total products={ppCount}");

            csvDump = csvHeader + '\n';
            for (var i = 0; i < ListOfLinks.Count; i++)
            {
                var p = Results[i];
                csvDump += $"'{p.regionName}','{p.productName}','{p.price}','{p.priceOld}','{p.inStock}','{p.breadCrumbs}','{p.productHref}','{p.imageHrefs}'\n";
            }
            File.WriteAllText($"{fileName}", csvDump);
        }

        static void ProductInfoCollector(taskInfo IO_Block)
        {
            while (true)
            {
                while (IO_Block.command == CMD.StandBy)
                    Thread.Sleep(10);

                if (IO_Block.command == CMD.Exit)
                {
                    IO_Block.command = CMD.StandBy;
                    return;
                }
                if (IO_Block.command == CMD.GetProdInfo)
                {
                    IO_Block.isResultReady = false;
                    IO_Block.result = GetProductInfo(IO_Block.productLink, IO_Block.cookie);
                    if (IO_Block.result is null)
                        if (IO_Block.badAttempts++ < 2)
                            continue;
                    IO_Block.command = CMD.StandBy;
                    IO_Block.isResultReady = true;
                }
            }
        }

        static string GetPageFromSite(string href,string Cookie)
        {
            int attempts = 0;
            string page = "";
            while (true)
            {
                page = "";
                var request = (HttpWebRequest)WebRequest.Create(href);
                request.Timeout = 10000;
                request.ReadWriteTimeout = 10000;
                request.ContinueTimeout = 10000;
                request.Method = "GET";
                request.Headers.Add(Cookie);
                request.AutomaticDecompression = DecompressionMethods.GZip;
                try
                {
                    var response = request.GetResponse(); //HttpWebResponse
                    var reader = new System.IO.StreamReader(response.GetResponseStream(), Portable.Text.Encoding.GetEncoding(1251));
                    page = reader.ReadToEnd();
                    reader.Close();
                }
                catch (Exception)
                {
                    if(attempts>=3)
                        throw new TimeoutException();
                    Task.Delay(2000);
                    attempts++;
                }
                break;
            }
            return page;
        }

        static productInfo GetProductInfo(string href, string Cookie)
        {
            var pInfo = new productInfo(href);
            try
            {
                var productPage = GetPageFromSite(href, Cookie);
                var parser = new HtmlParser();
                var document = parser.ParseDocument(productPage);
                pInfo.regionName = document.QuerySelector("[data-src=\"#region\"]").TextContent.Replace("\t", "").Trim();
                var bc = document.QuerySelectorAll(".breadcrumb > a,.breadcrumb >span");
                for (var i = 0; i < bc.Count() - 1; i++)
                {
                    if (i == 0)
                        pInfo.breadCrumbs = bc[i].TextContent;
                    else
                        pInfo.breadCrumbs += ">" + bc[i].TextContent;
                }
                pInfo.productName = document.QuerySelector(".detail-name").GetAttribute("Content");
                pInfo.price = document.QuerySelector(".price").TextContent;
                pInfo.priceOld = document.QuerySelector(".old-price")?.TextContent;
                var imgCollection = document.QuerySelectorAll(".card-slider-for a").Select(x => x.GetAttribute("href")).Select(x=>x.Substring(0,x.Contains("?_cvc=") ? x.Length-16 : x.Length)).ToList();  //[0].GetAttribute("href"); //Replace("?_cvc=1647661175", "")

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
            }
            catch (Exception) { return null; }

            return pInfo;
        }
    }
}