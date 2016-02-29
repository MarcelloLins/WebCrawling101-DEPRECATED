/* 
 *  [2012_01_10] [ Marcello Lins ]
 * 
 *   This Class is responsible for executing Web Requests and hold possible error messages.
 *   In order to make use of this Class, after instantiating it, the user have to set
 *   up the URL used for the request by using the Public Property : "URL". This URL
 *   will be used to create an HttpWebRequest Object.
 *   
 *   There are also public properties that can be used to setup the request parameters 
 *   such as timeout,referer,host and so on. Any other parameter that is not included here
 *   can be added without any future mess.
 *   
 *   If the user needs the CookieContainer used on the requests to be reseted, he must implicitly 
 *   call the ClearCookies method, that will create a new CookieContainer overwriting the old one.
 *   
 *   After creating a request, and setting its parameters, the user can make use of methods such as
 *   "Get" and "Post", both returning the response of their requests.
 *   
 *   There are Default Values for some Attributes of the request:
 *     . Timeout           = 8000 Miliseconds
 *     . Encoding          = "ISO-8859-1"
 *     . Connections Limit = 500
 *     . UserAgent         = Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.1 (KHTML, like Gecko) Chrome/13.0.782.107 Safari/535.1
 *     
 * 
 *   <Meta> Tags Samples:
 *   
 *   . Content Type Tag: <meta http-equiv="Content-Type" content="text/html; charset=UTF-8;text/html">
 *   . Simple Meta Tag : <meta charset="utf-8">
 *   
 * 
 *   Documentation for HTTPStatusCode Enumeration : http://msdn.microsoft.com/en-us/library/system.net.httpstatuscode.aspx
 *     
 */

using System;
using System.Linq;
using System.Net;
using System.IO;
using System.Drawing;
using System.Net.Security;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;


namespace WebUtilsLib
{
    public class WebRequests : IDisposable
    {
        /// <summary>
        /// 
        /// The top priority for charset detection always goes to Header["Content-Type"]
        /// 
        /// [MozillaCharsetDetection]
        /// This detection is complete, in case of error it will run HtmlCharsetTag, and if still has errors, we use default
        /// 
        /// [HtmlCharsetTag]
        /// Search for charset tags in HTML, in case of error, we use default encoding
        /// 
        /// [ProviderCharset]
        /// Use the charset provider by user
        /// 
        /// [ForceMozillaCharsetDetection]
        /// Use this for testing only, it will ignore Header["Content-Type"] and try to detect with MozillaCharsetDetection
        /// </summary>
        public enum CharsetDetection
        {
            /// <summary>
            /// Try to detect the page encoding using a character based heuristic. <para/>
            /// If not found, fallback to HtmlCharsetTag, and then to DefaultCharset.
            /// </summary>
            MozillaCharsetDetection = 0,
            /// <summary>
            /// Try to find the html charset meta tag.<para/>
            /// If not found, fallback to DefaultCharset.
            /// </summary>
            HtmlCharsetTag,
            /// <summary>
            /// Use the provided charset encoding to decode the page
            /// </summary>
            DefaultCharset,
            /// <summary>
            /// Use this for testing only, it will ignore Header["Content-Type"] and try to detect with MozillaCharsetDetection
            /// </summary>
            ForceMozillaCharsetDetection
        };

        /// <summary>
        /// Delegate used to decide whether to use the ReadResponse until the end method,
        /// or the limited ReadResponse one.
        /// </summary>
        private delegate StringBuilder ReadResponseDelegate (HttpWebRequest request, StringBuilder htmlBuilder, Encoding defaultEncoding);

        #region ** Private Attributes **

        // Private Attributes
        private CharsetDetection m_charsetDetection = CharsetDetection.MozillaCharsetDetection;
        private WebProxy m_proxy = null;
        private CookieContainer m_cookieJar = null;
        private HttpWebRequest m_HttpWebRequest = null;
        private NetworkCredential m_credentials = null;
        private string m_userAgent = String.Empty;
        private string m_contentType = String.Empty;
        private string m_host = String.Empty;
        private string m_encoding = String.Empty;
        private string m_referer = String.Empty;
        private string m_error = String.Empty;
        private string m_origin = String.Empty;
        private string m_accept = String.Empty;
        private int m_timeout = 0;
        private int m_bufferSize = 0;
        private int m_readWriteTimeout = 0;
        private int m_operationTimeout = 0;
        private int m_maxResponseSize;
        private bool m_allowAutoRedirect = false;
        private bool m_keepAlive = false;
        private WebHeaderCollection m_requestHeaders = null;
        private DecompressionMethods m_automaticDecompression;
        private Encoding m_pageEncoding;
        private HttpStatusCode m_statusCode;
        private string m_redirectLocation;
        private string m_fullRedirectLocation;
        private StringBuilder m_htmlBuilder = new StringBuilder ();
        private char[] m_charBuf = null;
        private byte[] m_buffer = null;

        // Redirect Status Codes
        private static HashSet<HttpStatusCode> m_redirectStatusCodes;

        #endregion

        #region ** Public Properties **
        public CookieContainer CookieJar
        {
            get { return m_cookieJar; }
        }

        // Public Properties
        public HttpWebRequest InternalWebRequest
        {
            get { return m_HttpWebRequest; }
            set { m_HttpWebRequest = value; }
        }

        public CharsetDetection EncodingDetection
        {
            get { return m_charsetDetection; }
            set { m_charsetDetection = value; }
        }

        /// <summary>
        /// Size of the buffer used on the
        /// memory reader class in bytes
        /// </summary>
        public int BufferSize
        {
            get { return m_bufferSize; }
            set { m_bufferSize = value; }
        }

        /// <summary>
        /// UserAgent attribute
        /// Of the HttpWebRequest
        /// </summary>
        public string UserAgent
        {
            get { return m_userAgent; }
            set { m_userAgent = value; }
        }

        /// <summary>
        /// Accept attribute
        /// Of the HttpWebRequest
        /// </summary>
        public string Accept
        {
            get { return m_accept; }
            set { m_accept = value; }
        }

        /// <summary>
        /// Gets or Sets the Maximum size of a response.
        /// Default value is Int32.MaxValue
        /// </summary>
        public int MaxResponseSize
        {
            get { return m_maxResponseSize; }
            set { m_maxResponseSize = value; }
        }

        /// <summary>
        /// Connection KeepAlive attribute
        /// Of the HttpWebRequest
        /// </summary>
        public bool KeepAlive
        {
            get { return m_keepAlive; }
            set { m_keepAlive = value; }
        }

        /// <summary>
        /// WebProxy attribute
        /// </summary>
        public WebProxy Proxy
        {
            get { return m_proxy; }
            set { m_proxy = value; }
        }

        /// <summary>
        /// Network credentials attribute for 
        /// proxy/network authentication
        /// </summary>
        public NetworkCredential Credentials
        {
            get { return m_credentials; }
            set { m_credentials = value; }
        }

        /// <summary>
        /// Host attribute
        /// Of the HttpWebRequest
        /// </summary>
        public string Host
        {
            get { return m_host; }
            set { m_host = value; }
        }

        /// <summary>
        /// ContentType attribute
        /// Of the HttpWebRequest
        /// </summary>
        public string ContentType
        {
            get { return m_contentType; }
            set { m_contentType = value; }
        }

        /// <summary>
        /// Encoding parameter
        /// Of the HttpWebRequest
        /// </summary>
        public string Encoding
        {
            get { return m_encoding; }
            set { m_encoding = value; }
        }

        /// <summary>
        /// Encoding used on the page of the last
        /// request executed
        /// </summary>
        public Encoding LastPageEncoding
        {
            get { return m_pageEncoding; }
        }

        /// <summary>
        /// Referer attribute
        /// Of the HttpWebRequest
        /// </summary>
        public string Referer
        {
            get { return m_referer; }
            set { m_referer = value; }
        }

        /// <summary>
        /// Timeout attribute
        /// Of the HttpWebRequest
        /// </summary>
        public int Timeout
        {
            get { return m_timeout; }
            set { m_timeout = value; }
        }

        /// <summary>
        /// Read Write Timeout attribute
        /// Of the HttpWebRequest
        /// </summary>
        public int ReadWriteTimeout
        {
            get { return m_readWriteTimeout; }
            set { m_readWriteTimeout = value; }
        }

        /// <summary>
        /// Timeout of the request as a whole. If any Http Verb
        /// takes longer than this timeout, the methods will abort
        /// </summary>
        public int OperationTimeout
        {
            get { return m_operationTimeout; }
            set { m_operationTimeout = value; }
        }

        /// <summary>
        /// AllowAutoRedirect attribute
        /// Of the HttpWebRequest
        /// </summary>
        public bool AllowAutoRedirect
        {
            get { return m_allowAutoRedirect; }
            set { m_allowAutoRedirect = value; }
        }

        /// <summary>
        /// Gets or Sets the Automatic Decompression
        /// attribute
        /// </summary>
        public DecompressionMethods AutomaticDecompression
        {
            get { return m_automaticDecompression; }
            set { m_automaticDecompression = value; }
        }

        /// <summary>
        /// Origin attribute
        /// Of the HttpWebRequest
        /// </summary>
        public string Origin
        {
            get { return m_origin; }
            set { m_origin = value; }
        }

        /// <summary>
        /// Headers attribute
        /// of the HttpWebRequest
        /// </summary>
        public WebHeaderCollection Headers
        {
            get { return m_requestHeaders; }
            set { m_requestHeaders = value; }
        }

        /// <summary>
        /// Message containing the
        /// last error that ocurred.
        /// Can be reseted by using ClearError
        /// Method
        /// </summary>
        public string Error
        {
            get { return m_error; }
        }

        /// <summary>
        /// Check documentation for status codes
        /// </summary>
        public HttpStatusCode StatusCode
        {
            get { return m_statusCode; }
        }

        /// <summary>
        /// Getter for redirect location if there was any
        /// </summary>
        public string RedirectLocation
        {
            get { return m_redirectLocation; }
        }

        /// <summary>
        /// Getter for the m_fullredirectlocation attribute
        /// </summary>
        public string FullRedirectLocation
        {
            get { return m_fullRedirectLocation; }
        }

        public StringBuilder InternalResponseBuffer
        {
            get { return m_htmlBuilder; }
        }

        #endregion

        #region ** Class Constructor **

        static WebRequests ()
        {
            // Enable useUnsafeHeaderParsing for CRLF incomplete endings
            SetUseUnsafeHeaderParsing (true);

            m_redirectStatusCodes = new HashSet<HttpStatusCode>{HttpStatusCode.Moved, HttpStatusCode.MovedPermanently, HttpStatusCode.Redirect, 
                                                                HttpStatusCode.RedirectKeepVerb, HttpStatusCode.RedirectMethod,HttpStatusCode.TemporaryRedirect};

        }

        /// <summary>
        /// Class Constructor
        /// </summary>        
        public WebRequests ()
        {
            m_cookieJar = new CookieContainer ();

            // Setting Default Values for some Attributes
            m_contentType = "application/x-www-form-urlencoded";
            m_userAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.1 (KHTML, like Gecko) Chrome/13.0.782.107 Safari/535.1";
            m_encoding = "ISO-8859-1";
            m_automaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.None;
            m_timeout = 8000;
            m_readWriteTimeout = 8000;
            m_operationTimeout = Int32.MaxValue;
            m_allowAutoRedirect = true;
            m_requestHeaders = new WebHeaderCollection ();

            // Default BufferSize
            m_bufferSize = 8 * 1024;

            // Setting up default max size for a request
            m_maxResponseSize = Int32.MaxValue;
        }

        #endregion

        #region ** Clearing and Configuration Methods Methods **

        public void Dispose ()
        {
            Reset ();
        }

        /// <summary>
        /// Clear the content of the 
        /// Cookie Container class used in 
        /// the requests
        /// </summary>
        public void ClearCookies ()
        {
            m_cookieJar = new CookieContainer ();
        }

        /// <summary>
        /// Clears the last error variable
        /// </summary>
        public void ClearError ()
        {
            m_error = String.Empty;
        }

        /// <summary>
        /// Ignored any certificate validation issued by the request.
        /// This usually solves Validation/Authentication errors
        /// such as "access denied" or "Forbidden"
        /// </summary>
        public void TurnOffCertificateValidator ()
        {
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback (delegate { return true; });
        }

        private void Reset ()
        {
            m_HttpWebRequest = null;
            if (m_htmlBuilder != null)
                m_htmlBuilder.Clear ();
            // Reseting PageEncoding Value
            m_pageEncoding = null;
            ClearError ();
        }

        #endregion

        #region ** HttpWebRequests Methods **

        private HttpWebRequest CreateHttpWebRequest (string url, string method)
        {
            var request = WebRequest.Create (url) as HttpWebRequest;

            // Proxy
            if (m_proxy != null)
                request.Proxy = m_proxy;

            // Credentials
            if (m_credentials != null)
                request.Credentials = m_credentials;

            // Headers
            if (m_requestHeaders != null)
                request.Headers = m_requestHeaders;

            request.AutomaticDecompression = m_automaticDecompression;
            request.CookieContainer = m_cookieJar;
            request.Method = method;
            request.UserAgent = m_userAgent;
            request.Timeout = m_timeout;
            request.ReadWriteTimeout = m_readWriteTimeout;
            request.ContentType = m_contentType;
            request.Referer = m_referer;
            request.AllowAutoRedirect = m_allowAutoRedirect;
            request.Accept = m_accept;
            request.KeepAlive = m_keepAlive;

            if (!String.IsNullOrEmpty (m_host))
            {
                request.Host = m_host;
            }

            return request;
        }

        /// <summary>
        /// Executes a Get
        /// creating an HttpWebRequest 
        /// object based on previously 
        /// set attributes
        /// 
        /// OBS: Caso esta implementação dê algum problema, fazer o seguinte:
        ///  1. Ler todos os bytes do stream, bloco a bloco, mas sem fazer encoding, só ler os bytes
        ///  2. Guardar os bytes e fazer encoding deles para string, usando o m_encoding
        ///  3. Procurar a tag de encoding, e , se o encoding for diferente do m_encoding, fazer encoding dos bytes
        ///  guardados, para o encoding da página
        ///  
        /// </summary>
        /// <returns>Response of the Request. Empty string if any error ocurred.</returns>
        public string Get (string url, bool throwOnError = false)
        {
            return Query ("GET", url, null, null, throwOnError);
        }

        /// <summary>
        /// Executes a post request and returns the response
        /// instead of the text/html/json/xml
        /// of the request
        /// </summary>
        /// <param name="url">Url of the request</param>
        /// <param name="throwOnError"></param>
        /// <returns>WebResponse of the post</returns>
        public WebResponse ResponsePost (string url, string postData, bool throwOnError = false)
        {
            return QueryResponse ("POST", url, postData, null, throwOnError);
        }

        public WebResponse ResponsePut (string url, string data, bool throwOnError = false)
        {
            return QueryResponse ("PUT", url, data, null, throwOnError);
        }

        public WebResponse ResponseDelete (string url, string data, bool throwOnError = false)
        {
            return QueryResponse ("DELETE", url, data, null, throwOnError);
        }

        /// <summary>
        /// Executes a get request and returns the response
        /// instead of the text/html/json/xml
        /// of the request
        /// </summary>
        /// <param name="url">Url for the request</param>
        /// <param name="throwOnError"></param>
        /// <returns>Web response of the get</returns>
        public WebResponse ResponseGet (string url, bool throwOnError = false)
        {
            return QueryResponse ("GET", url, null, null, throwOnError);
        }

        /// <summary>
        /// Gets the stream        
        /// </summary>
        /// <param name="url">The URL.</param>        
        /// <returns>True if no error ocurred</returns>
        public byte[] GetBytes (string url, bool throwOnError = false)
        {
            Reset ();
            // Checking for empty url
            if (String.IsNullOrEmpty (url))
            {
                if (throwOnError)
                    throw new Exception ("URL para o Request não foi configurada ou é nula.");

                m_error = "URL para o Request não foi configurada ou é nula.";
                return null;
            }

            try
            {
                // Re-Creating Request Object to avoid exceptions
                m_HttpWebRequest = CreateHttpWebRequest (url, "GET");

                // Execute web request and wait for response
                using (HttpWebResponse resp = (HttpWebResponse)m_HttpWebRequest.GetResponse ())
                {
                    // Reading response
                    using (MemoryStream ms = new MemoryStream ())
                    {
                        using (var stream = resp.GetResponseStream ())
                        {
                            stream.CopyTo (ms);
                            return ms.ToArray ();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                m_error = ex.Message;
                if (throwOnError)
                    throw ex;
            }

            return null;
        }
        
        /// <summary>
        /// Executes a POST
        /// creating an HttpWebRequest 
        /// object based on previously 
        /// set attributes.
        /// </summary>
        /// <param name="postData">Parameters the Post Request</param>
        /// <returns>Response of the Request. Empty string if any error ocurred.</returns>
        public string Post (string url, string postData, bool throwOnError = false)
        {
            return Query ("POST", url, postData, null, throwOnError);
        }

        public string Put (string url, string postData, bool throwOnError = false)
        {
            return Query ("PUT", url, postData, null, throwOnError);
        }

        public string Delete (string url, string postData, bool throwOnError = false)
        {
            return Query ("DELETE", url, postData, null, throwOnError);
        }

        public WebResponse QueryResponse (string verb, string url, string contentData, string contentType, bool throwOnError)
        {
            Reset ();

            // Checking for empty url
            if (String.IsNullOrEmpty (url))
            {
                if (throwOnError)
                    throw new Exception ("URL para o Request não foi configurada ou é nula.");

                m_error = "URL para o Request não foi configurada ou é nula.";
                return null;
            }

            try
            {
                // Re-Creating Request Object to avoid exceptions
                m_HttpWebRequest = CreateHttpWebRequest (url, verb);

                // Setting default encoding to the one previously configured
                Encoding currentEncode = TryGetEncoding (m_encoding);
                if (currentEncode == null)
                    currentEncode = System.Text.Encoding.UTF8;

                // Counting the bytes to send
                if (!String.IsNullOrEmpty (contentData))
                {
                    byte[] bytes = currentEncode.GetBytes (contentData);
                    int sz = bytes.Length;
                    m_HttpWebRequest.ContentLength = bytes.Length;

                    // TODO: implement contentType to send the correnct encoding used to send the byte stream
                    //if (contentType != null)
                    //    m_HttpWebRequest.Headers["ContentType"] = (m_HttpWebRequest.Headers["ContentType"] ?? "") + "charset=" +currentEncode.HeaderName;

                    // write content
                    using (var requestStream = m_HttpWebRequest.GetRequestStream ())
                    {
                        requestStream.Write (bytes, 0, sz);
                    }
                }

                // Send Request
                return m_HttpWebRequest.GetResponse ();
            }
            catch (Exception ex)
            {
                m_error = ex.Message;
                if (throwOnError)
                    throw ex;
            }

            return null;
        }

        public string Query (string verb, string url, string contentData, string contentType, bool throwOnError)
        {
            Reset ();

            // Checking for empty url
            if (String.IsNullOrEmpty (url))
            {
                if (throwOnError)
                    throw new Exception ("URL para o Request não foi configurada ou é nula.");

                m_error = "URL para o Request não foi configurada ou é nula.";
                return String.Empty;
            }

            try
            {
                // Re-Creating Request Object to avoid exceptions
                m_HttpWebRequest = CreateHttpWebRequest (url, verb);

                // Setting default encoding to the one previously configured
                Encoding currentEncode = TryGetEncoding (m_encoding);
                if (currentEncode == null)
                    currentEncode = System.Text.Encoding.UTF8;

                // Counting the bytes to send
                if (!String.IsNullOrEmpty (contentData))
                {
                    byte[] bytes = currentEncode.GetBytes (contentData);
                    int sz = bytes.Length;
                    m_HttpWebRequest.ContentLength = bytes.Length;

                    // TODO: implement contentType to send the correnct encoding used to send the byte stream
                    //if (contentType != null)
                    //    m_HttpWebRequest.Headers["ContentType"] = (m_HttpWebRequest.Headers["ContentType"] ?? "") + "charset=" +currentEncode.HeaderName;

                    // write content
                    using (var requestStream = m_HttpWebRequest.GetRequestStream ())
                    {
                        requestStream.Write (bytes, 0, sz);
                    }
                }

                // Gets the Response
                var htmlBuilder = ReadRequestResponseUntilMaxSize (m_HttpWebRequest, m_htmlBuilder, currentEncode);

                // Checking for error message and for the need to throw an exception
                if (!String.IsNullOrWhiteSpace (m_error) && throwOnError)
                {
                    throw new Exception (m_error);
                }

                if (htmlBuilder != null)
                {
                    return htmlBuilder.ToString ();
                }
            }
            catch (Exception ex)
            {
                m_error = ex.Message;
                if (throwOnError)
                    throw;
            }

            return String.Empty;
        }

        public string ResolveURL (string url)
        {
            HttpWebRequest request;
            string resultUrl;
            HashSet<string> visitedPages;
            Uri redirectUrl;
            const int loopMax = 1000000;
            int loopCount = 0;

            // Initializing Variables
            visitedPages = new HashSet<string> (StringComparer.Ordinal);

            // First Value for Redirect
            resultUrl = url;
            redirectUrl = new Uri (resultUrl);

            // Loops until the same page got visited twice or no redirect happened on the request
            while (loopCount++ < loopMax)
            {
                // Formats the Request object
                request = (HttpWebRequest)WebRequest.Create (redirectUrl);
                request.AllowAutoRedirect = false;
                request.Method = "HEAD";

                // Reading Response
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse ())
                {
                    // Checking for Redirect Status Codes
                    if (m_redirectStatusCodes.Contains (response.StatusCode))
                    {
                        // Reading the Redirect Location
                        resultUrl = AssembleFullRedirectUrl (resultUrl, response.Headers["Location"]);

                        // Checking for already visited redirect
                        if (visitedPages.Contains (resultUrl))
                        {
                            resultUrl = string.Empty;
                            break;
                        }

                        // Adding visited url to collection
                        visitedPages.Add (resultUrl);

                        // Creating new URI with redirect location
                        redirectUrl = new Uri (resultUrl);
                    }
                    else
                    {
                        resultUrl = response.ResponseUri.ToString ();
                        break;
                    }
                }
            };

            return resultUrl;
        }

        /// <summary>
        /// Reads the request response, until the max size
        /// is reached or until the end of the stream.
        /// </summary>
        /// <param name="request">The request</param>
        /// <param name="htmlBuilder">The HTML builder</param>
        /// <param name="defaultEncoding">The default encoding</param>
        /// <returns>StringBuilder with the encoded stream</returns>
        public StringBuilder ReadRequestResponseUntilMaxSize (HttpWebRequest request, StringBuilder htmlBuilder = null, Encoding defaultEncoding = null)
        {
            return ReadRequestResponseUntilMaxSize (request, (HttpWebResponse)request.GetResponse (), htmlBuilder, defaultEncoding);
        }

        /// <summary>
        /// Reads the request response, until the max size
        /// is reached or until the end of the stream.
        /// </summary>
        /// <param name="request">The request</param>
        /// <param name="htmlBuilder">The HTML builder</param>
        /// <param name="defaultEncoding">The default encoding</param>
        /// <returns>StringBuilder with the encoded stream</returns>
        public StringBuilder ReadRequestResponseUntilMaxSize (HttpWebRequest request, HttpWebResponse response, StringBuilder htmlBuilder = null, Encoding defaultEncoding = null)
        {
            // Clearing Last Redirect Location
            m_redirectLocation = String.Empty;

            using (HttpWebResponse webResponse = response)
            {
                if (webResponse != null)
                {
                    // Creating operation timeout "timer"
                    Stopwatch timeoutTimer = new Stopwatch ();
                    timeoutTimer.Start ();

                    // Setting public httpstatus code
                    m_statusCode = webResponse.StatusCode;

                    // Checking redirect request codes (301, 302, 303, 307)
                    if (m_redirectStatusCodes.Contains (m_statusCode))
                    {
                        m_redirectLocation = webResponse.Headers["Location"];
                        m_fullRedirectLocation = AssembleFullRedirectUrl (request.RequestUri.ToString (), m_redirectLocation);
                    }
                    else if (!request.Address.ToString ().Equals (request.RequestUri.ToString (), StringComparison.OrdinalIgnoreCase) ||
                        !webResponse.ResponseUri.ToString ().Equals (request.RequestUri.ToString (), StringComparison.OrdinalIgnoreCase))
                    {
                        m_redirectLocation = webResponse.ResponseUri.ToString ();
                        m_fullRedirectLocation = AssembleFullRedirectUrl (request.RequestUri.ToString (), m_redirectLocation);
                    }

                    int contentLength = 0;
                    Int32.TryParse (webResponse.Headers["Content-Length"], out contentLength);
                    contentLength = (int)(contentLength * 1.1);
                    if (contentLength < m_bufferSize)
                        contentLength = m_bufferSize;
                    else if (contentLength > 2 * 1024 * 1024)
                        contentLength = 2 * 1024 * 1024;

                    string contentTypeCharset = null;

                    // ensure default/fallback encoding is set
                    if (defaultEncoding == null)
                        defaultEncoding = TryGetEncoding ("ISO-8859-1");

                    // enconding detection mode
                    if (m_charsetDetection == CharsetDetection.DefaultCharset && m_pageEncoding == null)
                    {
                        m_pageEncoding = defaultEncoding;
                    }
                    // get enconding from header
                    // http://stackoverflow.com/questions/5436452/detecting-character-encoding-in-html
                    else
                    {
                        contentTypeCharset = GetEncodingValue (webResponse.Headers["Content-Type"], "charset=");
                    }

                    // http://www.w3.org/TR/html4/charset.html#h-5.2.2
                    // w3 defines the priority as:
                    // To sum up, conforming user agents must observe the following priorities when determining a document's character encoding (from highest priority to lowest):
                    // 1. An HTTP "charset" parameter in a "Content-Type" field.
                    // 2. A META declaration with "http-equiv" set to "Content-Type" and a value set for "charset".
                    // 3. The charset attribute set on an element that designates an external resource.
                    if (m_pageEncoding == null && !String.IsNullOrEmpty (contentTypeCharset) && m_charsetDetection != CharsetDetection.ForceMozillaCharsetDetection)
                    {
                        m_pageEncoding = TryGetEncoding (contentTypeCharset);
                    }

                    // Read the Content of the response
                    using (var responseStream = webResponse.GetResponseStream ())
                    {
                        // Creating fixed size buffer for bytes                        
                        int bytesRead = 0;
                        int charBufSz = 0;
                        if (m_charBuf == null || m_charBuf.Length != m_bufferSize)
                            m_charBuf = new char[m_bufferSize];
                        if (m_buffer == null || m_buffer.Length != m_bufferSize)
                            m_buffer = new byte[m_bufferSize];
                        List<byte[]> buffers = new List<byte[]> ();
                        List<int> buffersSz = new List<int> ();

                        // Mozilla Universal Charset Detector
                        // Google Code: https://code.google.com/p/ude/
                        // SVN        : https://bigdata.svn.beanstalkapp.com/rd/MozillaUniversalCharsetDetector
                        Ude.CharsetDetector mozillaCharsetDetector = null;
                        if (m_pageEncoding == null && m_charsetDetection == CharsetDetection.MozillaCharsetDetection || m_charsetDetection == CharsetDetection.ForceMozillaCharsetDetection)
                        {
                            mozillaCharsetDetector = new Ude.CharsetDetector ();
                        }
                        Encoding metaTagEncoding = null;

                        // ** Encoding detection code **
                        // Iterates While no encoding was found
                        int totalBytesRead = 0; // Used to Control the ammount of bytes read so that it wont exceed the maximum allowed by the m_MaxResponseSize
                        bool maxSizeReached = false;
                        byte[] tempBuffer = m_buffer;
                        do
                        {
                            // Reading block of bytes                            
                            bytesRead = responseStream.Read (tempBuffer, 0, m_bufferSize);

                            // Checking for no bytes read
                            if (bytesRead <= 0)
                            {
                                break;
                            }

                            // If any byte was read, adds it to the list of buffers
                            buffers.Add (tempBuffer);
                            buffersSz.Add (bytesRead);

                            // Updating total bytes read
                            totalBytesRead += bytesRead;

                            // Checking for maximum size of request reached
                            if (totalBytesRead >= m_maxResponseSize)
                            {
                                maxSizeReached = true;
                                break;
                            }

                            // charset detection
                            if (m_pageEncoding == null)
                            {
                                // MozillaCharsetDetection
                                if (m_charsetDetection == CharsetDetection.MozillaCharsetDetection || m_charsetDetection == CharsetDetection.ForceMozillaCharsetDetection)
                                {
                                    // try to detect
                                    mozillaCharsetDetector.Reset ();
                                    mozillaCharsetDetector.Feed (tempBuffer, 0, bytesRead);
                                    mozillaCharsetDetector.DataEnd ();

                                    // Try to use mozilla charset detection before identify by HTML charset tag
                                    if (mozillaCharsetDetector.Charset != null && mozillaCharsetDetector.Charset != "ASCII" && (mozillaCharsetDetector.IsDone () || mozillaCharsetDetector.Confidence > 0.49))
                                    {
                                        m_pageEncoding = TryGetEncoding (mozillaCharsetDetector.Charset);
                                        break;
                                    }
                                }

                                // HtmlCharsetTag (MozillaCharsetDetection fallback mode)
                                if ((metaTagEncoding == null) && (m_charsetDetection == CharsetDetection.MozillaCharsetDetection || m_charsetDetection == CharsetDetection.HtmlCharsetTag || (m_charsetDetection == CharsetDetection.ForceMozillaCharsetDetection && contentTypeCharset != null)))
                                {
                                    charBufSz = GetChars (tempBuffer, bytesRead, defaultEncoding, ref m_charBuf);
                                    metaTagEncoding = IdentifyEncodingByMetaTag (new string (m_charBuf, 0, charBufSz));
                                    if (m_charsetDetection == CharsetDetection.HtmlCharsetTag)
                                        m_pageEncoding = metaTagEncoding;
                                }

                                // detection limit                                
                                if ((buffers.Count >= 3 && metaTagEncoding != null) ||
                                    (buffers.Count >= 10))
                                {
                                    break;
                                }
                            }

                            // create temp buffer for next pass
                            if (m_pageEncoding == null)
                                tempBuffer = new byte[m_bufferSize];
                        }
                        while (m_pageEncoding == null);

                        // Encoding not found - fallback mode
                        if (m_pageEncoding == null)
                        {
                            // set tag if not detected by MozillaCharsetDetection but detected in MetaTag (fallback mode)
                            if (metaTagEncoding != null)
                            {
                                m_pageEncoding = metaTagEncoding;
                            }
                            else if (mozillaCharsetDetector != null && !String.IsNullOrEmpty (mozillaCharsetDetector.Charset))
                            {
                                // if we detected an encoding, but wasn't used because of low confidence in it, now is the time to use it!
                                m_pageEncoding = TryGetEncoding (mozillaCharsetDetector.Charset);
                            }
                            else if (!String.IsNullOrEmpty (contentTypeCharset))
                            {
                                m_pageEncoding = TryGetEncoding (contentTypeCharset);
                            }
                        }

                        // use Default
                        if (m_pageEncoding == null)
                        {
                            m_pageEncoding = defaultEncoding;
                        }

                        // ** load remaining info and build html string **
                        // prepares StringBuilder
                        if (htmlBuilder == null)
                            htmlBuilder = new StringBuilder (contentLength);
                        else
                            htmlBuilder.Clear ();

                        // Checking for empty list of buffers and no byte read. This means the request failed
                        if (bytesRead <= 0 && buffers.Count == 0)
                        {
                            m_error = "nenhum byte foi lido na requisição";
                            return htmlBuilder;
                        }

                        // Checking for the need to ensure the minimum stringbuilder Size
                        htmlBuilder.EnsureCapacity (contentLength);

                        // Parses All the byte buffers but the last one, which is parsed after the loop - Converts bytes to string using the Encoding found on the page                        
                        for (var i = 0; i < buffers.Count; i++)
                        {
                            // Appending to string builder, the bytes decoded using the M_PageEncoding 
                            charBufSz = GetChars (buffers[i], buffersSz[i], m_pageEncoding, ref m_charBuf);
                            htmlBuilder.Append (m_charBuf, 0, charBufSz);
                        }

                        // Checking if the maximum response size was already reached, to avoid reading the rest of the stream
                        if (maxSizeReached || bytesRead <= 0)
                        {
                            return htmlBuilder;
                        }

                        // Reading the rest of the stream bytes, and parsing them into
                        // string using the m_PageEncoding value
                        do
                        {
                            bytesRead = responseStream.Read (m_buffer, 0, m_bufferSize);

                            // Adding the rest of the bytes into the string builder if any byte was found
                            if (bytesRead > 0)
                            {
                                charBufSz = GetChars (m_buffer, bytesRead, m_pageEncoding, ref m_charBuf);
                                htmlBuilder.Append (m_charBuf, 0, charBufSz);
                            }

                            // Incrementing Total bytes read
                            totalBytesRead += bytesRead;

                            // Breaks Loop in case of operation taking longer than expected
                            if (timeoutTimer.ElapsedMilliseconds >= m_operationTimeout)
                            {
                                m_error = "Operation Timeout : " + timeoutTimer.ElapsedMilliseconds + " ms";
                                return null;
                            }

                        } while (bytesRead > 0 && totalBytesRead <= m_maxResponseSize);
                    }
                }
            }

            return htmlBuilder;
        }

        private static Encoding TryGetEncoding (string encodingName)
        {
            if (encodingName == null)
                return null;
            try
            {
                if (encodingName.Equals ("ASCII", StringComparison.OrdinalIgnoreCase))
                    encodingName = "ISO-8859-1";
                return System.Text.Encoding.GetEncoding (encodingName);
            }
            catch
            {
                // ignore
            }
            return null;
        }

        #endregion

        #region ** Encoding Identification and Auxiliar Methods **
        /// <summary>
        /// Tries to find values
        /// from <meta> tags that refer to the encoding used on the page
        /// </summary>
        /// <param name="response">Html Page</param>
        /// <returns>Encoding found on the page, null if none found</returns>
        private Encoding IdentifyEncodingByMetaTag (string response)
        {
            string encodingName = null;

            foreach (string tag in GetMetaTags (response))
            {
                // <meta charset="utf-8">
                // <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />

                // Checking if the <meta> tag is a charset definition one                  
                encodingName = GetEncodingValue (tag, "charset=");
                if (!String.IsNullOrEmpty (encodingName))
                    break;
            }

            // Setting default encoding in case none is found
            if (String.IsNullOrEmpty (encodingName))
                return null;

            return TryGetEncoding (encodingName);
        }

        static HashSet<string> m_validEncodings = null;

        /// <summary>
        /// Identifies the encoding used on the page
        /// looking after the <meta> tags.
        /// 
        /// In case no meta tag was found, the returned
        /// encoding is the default one
        /// </summary>
        /// <param name="response">Html of the page received</param>
        /// <returns>Found encoding of the page, if no encoding was found, returns the default (attribute) one</returns>        
        private IEnumerable<String> GetMetaTags (string response)
        {
            int ix = response.Length;

            // Iterating over <meta> tags
            while ((ix = response.LastIndexOf ("<meta", ix, ix + 1, StringComparison.Ordinal)) >= 0)
            {
                int last = response.IndexOf ('>', ix + 1);
                if (last++ < 0)
                    continue;

                yield return response.Substring (ix, last - ix);
            }
        }

        /// <summary>
        /// Extracts from the tagText the value 
        /// of the attribute received as argument
        /// </summary>
        /// <param name="tagText">Complete text of the tag</param>
        /// <param name="attributeName">Name of the attribute whose value will be returned</param>
        /// <returns>Value of the attribute maped by the name received</returns>
        private string GetEncodingValue (string tagText, string attributeName)
        {
            if (tagText == null)
                return null;
            int sz = tagText.Length;
            //attributeName = attributeName + "=";
            int startPos = System.Globalization.CultureInfo.InvariantCulture.CompareInfo.IndexOf (tagText, attributeName, 0, sz, System.Globalization.CompareOptions.IgnoreCase);
            if (startPos < 0)
                return String.Empty;
            startPos = startPos + attributeName.Length;

            if (tagText[startPos] == '\"' || tagText[startPos] == '\'')
                ++startPos;
            char c;
            int lastPos = -1;
            for (var i = startPos; i < sz; i++)
            {
                c = tagText[i];
                if (c == '\"' || c == '\'' || c == ';' || c == ',' || c == ' ')
                {
                    lastPos = i;
                    break;
                }
            }

            // range check
            if (lastPos < 0)
                lastPos = sz;

            // create our valid encodings table            
            if (m_validEncodings == null)
            {
                var tmpHash = new HashSet<string> (System.Text.Encoding.GetEncodings ().Select (e => e.Name), StringComparer.OrdinalIgnoreCase);
                m_validEncodings = tmpHash;
            }
            // check for valid encoding
            var encoding = tagText.Substring (startPos, lastPos - startPos).Trim ();
            if (!m_validEncodings.Contains (encoding))
                encoding = null;

            // get value
            return encoding;
        }

        private int GetChars (byte[] buffer, int bytesRead, Encoding encoding, ref char[] charBuf)
        {
            // Trying to identify the encoding on the current read bytes
            // get the total size
            int sz = encoding.GetCharCount (buffer, 0, bytesRead);
            // check array size
            if (charBuf == null || charBuf.Length < sz)
            {
                charBuf = new char[sz];
            }
            // get data
            return encoding.GetChars (buffer, 0, bytesRead, charBuf, 0);
        }

        private static bool SetUseUnsafeHeaderParsing (bool b)
        {
            try
            {
                var a = System.Reflection.Assembly.GetAssembly (typeof (System.Net.Configuration.SettingsSection));
                if (a == null)
                    return false;

                Type t = a.GetType ("System.Net.Configuration.SettingsSectionInternal");
                if (t == null)
                    return false;

                object o = t.InvokeMember ("Section",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.NonPublic, null, null, new object[] { });
                if (o == null)
                    return false;

                var f = t.GetField ("useUnsafeHeaderParsing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (f == null)
                    return false;

                f.SetValue (o, b);
            }
            catch { }
            return true;
        }

        /// <summary>
        /// Checks if the location url is a relative or absolute one.
        /// If its a relative url, the method will join it with the url
        /// parameter in order to build a absolute one.
        /// </summary>
        /// <param name="url">URL used on the request</param>
        /// <param name="responseLocation">Location header of the response</param>
        /// <returns>Absolute url for the redirect</returns>
        private string AssembleFullRedirectUrl (string url, string responseLocation)
        {
            // Normalizing Location
            responseLocation = responseLocation.ToLowerInvariant ();

            // Checking if the response contains full url symbols
            if (responseLocation.Contains ("http://") || responseLocation.Contains ("https://") || responseLocation.Contains ("www."))
            {
                return responseLocation;
            }
            else
            {
                // Checking malformed url
                if (!url.EndsWith ("/"))
                {
                    url = url + "/";
                }

                return url + responseLocation;
            }
        }

        #endregion
    }
}
