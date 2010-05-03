using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Collections;

namespace BasicHTTPServer
{
    public static class HTTPRequestHandler
    {
        public static string _DefaultResponse = "alive";

        public static void OnRequest(HTTPRequest Request, ref NetworkStream ns)
        {
            string HeadersString = Request._HTTPResponse.version + " " + Request.Parent.respStatus[Request._HTTPResponse.status] + "\r\n";

            //***Put Custom Handling Here!
            Request._HTTPResponse.Headers.Add("Content-Type", "text/plain");

            SendHeaders(HeadersString, Request._HTTPResponse.Headers, ref ns);

			// Send body
            Request._HTTPResponse.BodyData = new ASCIIEncoding().GetBytes(_DefaultResponse);
			if (Request._HTTPResponse.BodyData != null)
			ns.Write(Request._HTTPResponse.BodyData, 0, Request._HTTPResponse.BodyData.Length);
        }

        public static void SendHeaders(string initial, Hashtable Headers, ref NetworkStream ns)
        {
            string HeadersString = initial;

            foreach (DictionaryEntry Header in Headers) 
			{
				HeadersString += Header.Key + ": " + Header.Value + "\r\n";
			}

			HeadersString += "\r\n";
			byte[] bHeadersString = Encoding.ASCII.GetBytes(HeadersString);

			// Send headers
			ns.Write(bHeadersString, 0, bHeadersString.Length);
        }
    }
}
