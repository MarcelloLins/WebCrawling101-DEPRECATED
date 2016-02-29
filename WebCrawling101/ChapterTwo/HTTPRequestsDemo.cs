/*
 * What's the goal of this Chapter ? 
 *   - See simple examples of "HTTP Requests"
 *   - Issue different HTTP Requests
 *   - Understand Status Codes and Responses
 *  
 * What this code does ?
 *  
 *  This chapter's code tried to illustrate the usage of simple HTTP GET requests issued to well-known websites
 *  
 *  I hope that after reading the code, you feel confortable tweaking some requests to your need, and know how to inspect the responses and 
 *  status codes of each request you issue, looking for errors.
 * 
 *  If you are wondering how can you find the actual URL (or address) of a given request the answer is: Look at your browser's Address Bar.
 * 
 *       E.G: Open IMDB on your browser. Search for something (Leo DiCaprio, for instance). Look at the URL bar and you will see something similar to this:
 *          http://www.imdb.com/find?ref_=nv_sr_fn&q=leo+dicaprio&s=all  
 *          
 *       As you can see, the search terms you typed can be found in the URL after the "Q" parameter. So, by changing that, you can search for anything within IMDB,
 *       hence, reach the data you want in an automated way.
 * 
 *  There's no sofisticated code here, since the goal of this chapter is to allow you to debug and run the code, being able to 
 *  fully grasp what's happening behind the scenes
 * 
 * You can find this chapter's documentation on : https://github.com/MarcelloLins/WebCrawling101/wiki/Chapter-2-:-Understanding-HTTP-Requests
 * 
 */

using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebUtilsLib;

namespace ChapterTwo
{
    class HTTPRequestsDemo
    {
        static void Main (string[] args)
        {
            // Creating Disposable Instance of WebRequests Client (our custom class that can be found on the "Shared Library" project)
            using(WebRequests client = new WebRequests ())
            {
                Console.WriteLine ("Setting up Web Requests Client");

                // Example 1 : HTTP GET Request for IMDB Home Page
                GetIMDBHomePage (client);

                Console.WriteLine ("------------------------------------------");

                // Example 2 : HTTP GET Request for searching for something on IMDB
                SearchForContentOnIMDB (client, "Leonardo DiCaprio");
                Console.WriteLine ("------------------------------------------");
                SearchForContentOnIMDB (client, "Kill Bill");

                // Example 3 : Performing Login on Pocket Website
                Console.WriteLine ("------------------------------------------");
                PerformLoginOnPocket (client, "username", "password");

                Console.WriteLine ("------------------------------------------");
                Console.WriteLine ("End of Program. Press enter to halt");
                Console.ReadLine ();
            }
        }

        private static void GetIMDBHomePage(WebRequests client)
        {
            // First thing we have to do is to define the actual "Target" for our demo, in this case, it will be "www.imdb.com"
            // To start Simple, let's try to get the HTML of the Home Page of the site. Since we are RETRIEVING a resource (the home page), the request method needs to be a GET
            // But first, let's setup the headers for this request
            client.Host      = "imdb.com";
            client.UserAgent = "Web Crawling 101 Book - Used for educational purposes only";
            client.Timeout   = 18000; // 18 Seconds

            // Issuing the request itself
            string imdbHomePageUrl = "http://www.imdb.com/";
            Console.WriteLine ("GET Request for Home Page ({0})", imdbHomePageUrl);
            string htmlResponse    = client.Get (imdbHomePageUrl);

            // Sanity Check - Checking if the "Status Code" was "200 - OK", which is what a regular succesful HTTP request should return
            Console.WriteLine (" => Request Status Code : " + client.StatusCode);

            // Printing Length of the Response (which should be the HTML of the home page)
            Console.WriteLine (" => Response Length : " + htmlResponse.Length);

            // Writing Response to a file (TIP: Double click on the file to open it with your browser to see if the content is the same as IMDB's home page)
            string exeDirectory = Path.GetDirectoryName (System.Reflection.Assembly.GetEntryAssembly ().Location);
            Console.WriteLine (" => Writing HTML Page to file in : " + exeDirectory);
            File.WriteAllText ("imdb.html", htmlResponse);
        }

        private static void SearchForContentOnIMDB(WebRequests client, string searchTerm)
        {
            // First thing we have to do is to define the actual "Target" for our demo, in this case, it will be "www.imdb.com"
            // To start Simple, let's try to get the HTML of the Home Page of the site. Since we are RETRIEVING a resource (the home page), the request method needs to be a GET
            // But first, let's setup the headers for this request
            client.Host      = "imdb.com";
            client.UserAgent = "Web Crawling 101 Book - Used for educational purposes only";
            client.Timeout   = 18000; // 18 Seconds

            // Issuing the request itself
            string normalizedSearchTerm = searchTerm.Replace (' ', '+');
            string imdbSearchUrl        = String.Format("http://www.imdb.com/find?ref_=nv_sr_fn&q={0}&s=all", normalizedSearchTerm);
            Console.WriteLine ("GET Request - Searching For '{0}'", searchTerm);
            string htmlResponse         = client.Get (imdbSearchUrl);

            // Sanity Check - Checking if the "Status Code" was "200 - OK", which is what a regular succesful HTTP request should return
            Console.WriteLine (" => Request Status Code : " + client.StatusCode);

            // Printing Length of the Response (which should be the HTML of the home page)
            Console.WriteLine (" => Response Length : " + htmlResponse.Length);

            // Writing Response to a file (TIP: Double click on the file to open it with your browser to see if the content is the same as IMDB's home page)
            string exeDirectory = Path.GetDirectoryName (System.Reflection.Assembly.GetEntryAssembly ().Location);
            Console.WriteLine (" => Writing HTML Page to file in : " + exeDirectory);
            File.WriteAllText (normalizedSearchTerm + ".html", htmlResponse);
        }

        private static void PerformLoginOnPocket(WebRequests client, string username, string password)
        {
            // Explanation of the requests below:
            // In order to perform the LOGIN on Pocket website, we have to issue a POST request with a few parameters in it's BODY.
            // Aside from the obvious "Login" and "Password", there's also a variable parameter called "Form_Check".
            // This "form_check" parameter can be found on the login page, hidden in it's HTML, so what we are going to do is "Extract" this information
            // from there, and use it on the login request to automate the whole login flow.

            // First thing we have to do is to define the actual "Target" for our demo, in this case, it will be "www.imdb.com"
            // To start Simple, let's try to get the HTML of the Home Page of the site. Since we are RETRIEVING a resource (the home page), the request method needs to be a GET
            // But first, let's setup the headers for this request
            client.ClearCookies ();
            client.Host      = "getpocket.com";
            client.UserAgent = "Web Crawling 101 Book - Used for educational purposes only";
            client.Referer   = "https://getpocket.com/login?e=4";
            client.Origin    = "https://getpocket.com";
            client.Timeout   = 18000; // 18 Seconds

            // Reaching Home Page for the "Form Check" parameter
            Console.WriteLine (" => Executing GET Request for Home Page");
            string homePageHTML = client.Get ("https://getpocket.com/login?e=4");

            // Parsing "Form Check" parameter
            string formCheck = ExtractFormCheckParameter (homePageHTML);
            Console.WriteLine (" => Extracted FormCheck parameter hidden within the HTML '{0}'", formCheck);
            
            // Formatting the HTTP POST BODY of the LOGIN Request
            string postData = String.Format("feed_id={0}&password={1}&route=&form_check={2}&src=&source=email&source_page=%2Flogin&is_ajax=1",
                                             username, password, formCheck);

            // HTTP Post URl for "Login" on Pocket
            Console.WriteLine (" => Performing Login");
            string pocketLoginUrl = "https://getpocket.com/login_process.php";
            string loginResponse  = client.Post (pocketLoginUrl, postData);

            Console.WriteLine (" => Login Status Code : {0}", client.StatusCode);
            Console.WriteLine (" => Login Response Text : {0}", loginResponse);
        }

        private static string ExtractFormCheckParameter(string htmlResponse)
        {
            // This Parsing is done by using HTMLAgility Pack, a third party library
            // that relies on XPath Expressions to reach and extract data out of HTML Nodes (or XML)
            // More on this on the next chapter about "Scraping/Parsing data out of HTML Pages"
            HtmlDocument doc = new HtmlDocument ();
            doc.LoadHtml (htmlResponse);

            return doc.DocumentNode.SelectSingleNode ("//*[@class='field-form-check']").Attributes["value"].Value;
        }
    }
}
