/*
 * What's the goal of this Chapter ? 
 *   - See simple examples of "Policies"
 *   - Understand a simple "Flow" of a "Crawler"
 * 
 * 
 * This chapter will also have basic examples of "HTTP Requests" and "HTML Parsing", but those will be covered  
 * in details on further chapters, so, don't bother trying to hard to understand what's happening right now.
 * 
 *  
 * What this code does ?
 *  
 *  This chapter's code is a rude implementation of a "scrapper" of the "CNN" website (http://edition.cnn.com/), 
 *  which will scrape all the links from the "Front Page" of the site, visit them, and count the number of "Image"
 *  tags if finds. Nothing too fancy or useful, but should be enough to ilustrate a crawler behavior.
 *  
 *  I hope that this fairly simple code is enough to cover most of the "policies" (selection, re-visit and politeness), so as
 *  how an average crawler behaves.
 *  
 *  There's no sofisticated code here, since the goal of this chapter is to allow you to debug and run the code, being able to 
 *  fully grasp what's happening behind the scenes
 * 
 * You can find this chapter's documentation on : https://github.com/MarcelloLins/WebCrawling101/wiki/Chapter-1-:-Anatomy-of-a-Crawler
 * 
 */



using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChapterOne
{
    class CNNCrawler
    {
        // List of "URLS" found on the "Home Page" (Includes Duplicates)
        private static List<String>    _urls         = new List<String> ();
        private static HashSet<String> _visitedUrls  = new HashSet<String> ();
        private static int             _totalLinks   = 0;
        private static int             _totalImgTags = 0;

        // Url of the "Home Page"
        private static string _homePageUrl = "http://edition.cnn.com/";

        static void Main (string[] args)
        {
            // "Step" counter
            int currentStep = 1;

            // Creating "HTTP" Object to issue the "Request" for the HomePage of the site
            HttpWebRequest httpClient    = (HttpWebRequest) HttpWebRequest.Create (_homePageUrl);
            httpClient.Method            = "GET";

            // Console Feedback
            LogStep (currentStep++);

            // HttpWebResponse - Contains the "Response" of this specific Request
            HttpWebResponse httpResponse = (HttpWebResponse) httpClient.GetResponse ();

            // Console Feedback
            LogStep (currentStep++);

            // "HtmlResponse" variable, that contains the "HTML" of the home page, returned by the "GET" request
            string htmlResponse          = DownloadResponse (httpResponse);

            // Checking if the "Request" worked
            if (String.IsNullOrWhiteSpace (htmlResponse))
            {
                // Error.
                Console.WriteLine ("Error Executing Request. Check your internet connection");
                return;
            }

            // Console Feedback
            LogStep (currentStep++);

            // Now that we have the "HTML" response, we have to start parsing the Links from this page
            // Don't bother with "how" this is done, by now, it's better for you to understand the "steps", than how they are performed
            // An in depth analysis of "HTML Parsing" will be on a future chapter
            List<String> urls = ParseLinks (htmlResponse);
            _totalLinks       = urls.Count;

            // Iterating over all the Links, to pick which ones will be visited
            foreach (var url in urls)
            {
                // "Selection Policy" - Is this URL from the same domain ? (CNN)
                // This is a very simple example of a logic that tells which urls are useful for this crawler to visit, and which, aren't
                // In this specific case, the "Selection Policy" is basically : "Pick only the urls from this domain".
                // E.g 1 : "/weather" is a valid URL, because it's a RELATIVE url. Since the full URL is http://edition.cnn.com/weather, it is a valid url, hence, it will be "picked"
                // E.g 2 : "http://commercial.cnn.com" is not a valid URL, because the domain is different, hence, it will lead to a different website and should be "ignored" / "skiped"
                if (SelectionPolicy (url))
                {
                    _urls.Add (url);
                }
            }

            // Console Feedback
            LogStep (currentStep++);

            // Console Feedback
            LogStep (currentStep++);
            
            // Iterating over all the links found on the home page
            // Have in mind that this list might have duplicated Urls
            foreach (var url in _urls)
            {
                // Assembling URL (concatenating the "Domain" to the "Relative" Url)
                string absoluteUrl = "http://edition.cnn.com" + url;

                // "Re-Visit Policy" - Should I revisit the same Link ?
                // This example of "Re-Visit" policy is an implementation of "No-Revisit" policy, meaning that no URL is visited
                // more than once.
                // Since for each "URL" this code makes a decision on whether it should be visited or not, this can be considered a implementation
                // of one "Re-Visit" policy.
                if (!ReVisitPolicy (absoluteUrl))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine ("\t=> [Re-Visit Policy] Duplicated Url. Won't be visited again");
                    Console.ForegroundColor = ConsoleColor.White;
                    continue;
                }

                try
                {
                    // Executing HTTP request for this page, for it's HTML
                    httpClient = (HttpWebRequest)HttpWebRequest.Create (absoluteUrl);
                    httpClient.Method = "GET";
                    httpResponse = (HttpWebResponse)httpClient.GetResponse ();

                    // Reading String Response
                    htmlResponse = DownloadResponse (httpResponse);

                    // Counting Image Tags of this "Page"
                    int imgTags = CountImageTags (htmlResponse);

                    // Summing up
                    _totalImgTags += imgTags;

                    // "Flagging" this url as "visited"
                    _visitedUrls.Add (absoluteUrl);

                    // Console Feedback
                    Console.WriteLine ("\t=> [Re-Visit Policy] Visited Url. Found " + imgTags + " <img> tags");

                    // After a page is visited, our "Politeness-Policy" kicks in
                    // In this specific case, it will be as simple as : 'Waits 0.5 seconds'
                    PolitenessPolicy ();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine ("\t=> [Error] Ops. Error Visiting Url : " + url);
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }

            // Summary of Visits            
            PrintSummary ();

            Console.Read ();
        }

        /// <summary>
        /// Helper method to read the string representation
        /// of a "HTTPWebResponse" object
        /// </summary>
        /// <param name="response">Http Response</param>
        /// <returns>String value of the response</returns>
        private static string DownloadResponse (HttpWebResponse response)
        {
            string htmlResponse;

            // Reading Response Stream
            using (Stream dataStream = response.GetResponseStream ())
            {
                using (StreamReader reader = new StreamReader (dataStream))
                {
                    htmlResponse = reader.ReadToEnd ();
                }
            }

            return htmlResponse;
        }

        /// <summary>
        /// This is just a "dummy" method that will log the current
        /// step of the process to a console, so that you can better understand
        /// what's happening
        /// </summary>
        /// <param name="step">Current step of the process</param>
        private static void LogStep (int step)
        {
            switch (step)
            {
                case 1:
                    Console.WriteLine ("Executing HTTP Request for 'CNN' website");
                break;

                case 2:
                    Console.WriteLine ("Reading Response of the HTTP Request");
                break;

                case 3:
                    Console.WriteLine ("Response 'OK'");
                break;

                case 4:
                    Console.WriteLine ("Extrated Links from the 'HomePage'. Found : " + _urls.Count);
                break;

                case 5:
                    Console.WriteLine ("Visiting Urls found on the 'HomePage'");
                break;
            }
        }

        /// <summary>
        /// Prints a "Summary" of the process
        /// </summary>
        private static void PrintSummary ()
        {
            Console.WriteLine ("\n***************************************************");
            Console.WriteLine ("Total Page Links                               : " + _totalLinks);
            Console.WriteLine ("Valid Links (Selection Policy 'OK')            : " + _urls.Count);
            Console.WriteLine ("Visited Links                                  : " + _visitedUrls.Count);
            Console.WriteLine ("Skipped Valid Links (Re-Visit Policy 'NOT OK') : " + (_urls.Count - _visitedUrls.Count));
            Console.WriteLine ("\nTotal <img> tags found on all pages          : " + _totalImgTags);
            Console.WriteLine ("Average <img> tags per page                    : " + ((double)_totalImgTags / _visitedUrls.Count));
            Console.WriteLine ("\n***************************************************");
        }

        /// <summary>
        /// Helper method to extract all the "href" values of all the
        /// "<a>" tags of this page
        /// </summary>
        /// <param name="htmlResponse">HTML Page</param>
        /// <returns>List of found links</returns>
        private static List<String> ParseLinks (string htmlResponse)
        {
            // Loading the "HTML" response into a map, to allow searching for the nodes
            HtmlDocument map = new HtmlDocument ();
            map.LoadHtml (htmlResponse);
            
            // Returning all the "href" nodes of this Page
            return map.DocumentNode.SelectNodes ("//a").Select (t => t.Attributes["href"].Value).ToList ();
        }

        /// <summary>
        /// Helper method to count all the "<img>" tags
        /// found on the page received as argument
        /// </summary>
        /// <param name="htmlResponse">HTML Page</param>
        /// <returns>Number of <img> tags found</returns>
        private static int CountImageTags (string htmlResponse)
        {
            // Loading the "HTML" response into a map, to allow searching for the nodes
            HtmlDocument map = new HtmlDocument ();
            map.LoadHtml (htmlResponse);

            // Returning all the "href" nodes of this Page
            return map.DocumentNode.SelectNodes ("//img").Count;
        }

        #region ** Implementation of Policies **

        /// <summary>
        /// Simple implemention of a "Selection Policy" that returns "true"
        /// to urls from the same domain as the current one, and "false" to
        /// urls that leads to other domains
        /// </summary>
        /// <param name="url">Url</param>
        /// <returns>true if the url should be used, false otherwise</returns>
        private static bool SelectionPolicy (string url)
        {
            // Check 1 - Does the URL start with "Http" or "https" or "www" ? 
            if (url.StartsWith ("http") || url.StartsWith ("www.") || url.Length == 1
              || url.EndsWith (".com")  || url.StartsWith ("//"))
            {
                return false;
            }

            // Any other "URL" that doesn't starts with either "http" or "www"
            // is a "relative" url, meaning that it belongs to the current domain
            // E.G : "2015/04/13/opinions/williams-ceci-women-in-science/index.html" is actually the following url "http://edition.cnn.com/2015/04/13/opinions/williams-ceci-women-in-science/index.html"
            
            return true;
        }

        /// <summary>
        /// Simple implemention of a "Re-Visit Policy" that returns "true"
        /// to urls that were not visited yet, and "false" to
        /// urls that were visited already.
        /// 
        /// This implementation states that no URL will be visited twice, meaning
        /// that this can be considered a "No-Revisit" policy
        /// 
        /// </summary>
        /// <param name="url">Url</param>
        /// <returns>true if the url can be visited, false otherwise</returns>
        private static bool ReVisitPolicy (string url)
        {
            // Checking for the URL on the hashset of "Visited Urls"
            if (_visitedUrls.Contains (url))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Implementation of a 'Dummy' Politeness-Policy.
        /// 
        /// This simple method only 'Sleeps' for 0.5 seconds, but in a real scenario,
        /// you would have to see whether this time is enough to keep the target server
        /// from being DDOS'd be your processes.
        /// 
        /// Tip: Start from a 2 seconds sleep, and tune your process as you understand
        /// the server's behavior.
        /// 
        /// </summary>
        private static void PolitenessPolicy ()
        {
            // Sleeps for half a second
            Thread.Sleep (500);
        }

        #endregion
    }
}
