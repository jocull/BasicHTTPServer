using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;

namespace BasicHTTPServer
{
    public class HTTPServer
    {
        public int portNum = 80;
        public TcpListener listener;
        public Thread Thread;
		public Hashtable respStatus;
        public string Name = "BasicHTTPServer/1.0.*";

		public bool IsAlive
		{
			get 
			{
				return this.Thread.IsAlive; 
			}
		}

		public HTTPServer()
		{
			//
			respStatusInit();
		}

		public HTTPServer(int thePort)
		{
			portNum = thePort;
			respStatusInit();
		}

        public void respStatusInit()
		{
			respStatus = new Hashtable();
			
			respStatus.Add(200, "200 Ok");
			respStatus.Add(201, "201 Created");
			respStatus.Add(202, "202 Accepted");
			respStatus.Add(204, "204 No Content");

			respStatus.Add(301, "301 Moved Permanently");
			respStatus.Add(302, "302 Redirection");
			respStatus.Add(304, "304 Not Modified");
			
			respStatus.Add(400, "400 Bad Request");
			respStatus.Add(401, "401 Unauthorized");
			respStatus.Add(403, "403 Forbidden");
			respStatus.Add(404, "404 Not Found");

			respStatus.Add(500, "500 Internal Server Error");
			respStatus.Add(501, "501 Not Implemented");
			respStatus.Add(502, "502 Bad Gateway");
			respStatus.Add(503, "503 Service Unavailable");
		}

        public static bool CheckPortAvailability(int port)
        {
            //http://stackoverflow.com/questions/570098/in-c-how-to-check-if-a-tcp-port-is-available

            // Evaluate current system tcp connections. This is the same information provided
            // by the netstat command line application, just in .Net strongly-typed object
            // form.  We will look through the list, and if our port we would like to use
            // in our TcpClient is occupied, we will set isAvailable to false.
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
            {
                if (tcpi.LocalEndPoint.Port == port)
                    return false;
            }

            try
            {
                TcpListener listener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
                listener.Start();
                listener.Stop();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

		public void Listen() 
		{
            try
            {
                bool done = false;

                listener = new TcpListener(new IPEndPoint(IPAddress.Any, portNum));

                listener.Start();

                while (!done)
                {
                    //Waiting for connection...
                    HTTPRequest newRequest = new HTTPRequest(listener.AcceptTcpClient(), this);
                    Thread Thread = new Thread(new ThreadStart(newRequest.Process));
                    Thread.Name = "HTTP Request";
                    Thread.Start();
                }
            }
            catch (ThreadAbortException)
            {
                Console.WriteLine("Aborting listener...");
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
		}

		public void Start()
		{
			// HTTPServer HTTPServer = new HTTPServer(portNum);
			this.Thread = new Thread(new ThreadStart(this.Listen));
			this.Thread.Start();
		}

		public void Stop()
		{
			listener.Stop();
			this.Thread.Abort();
            while (this.Thread.IsAlive)
                Thread.Sleep(250);
		}

		//public abstract void OnResponse(ref HTTPRequestStruct rq, ref HTTPResponseStruct rp);
    }

    public struct HTTPRequestStruct
    {
        public string Method;
        public string URL;
        public string Version;
        public Hashtable Args;
        public bool Execute;
        public Hashtable Headers;
        public int BodySize;
        public byte[] BodyData;
    }

    public struct HTTPResponseStruct
    {
        public int status;
        public string version;
        public Hashtable Headers;
        public int BodySize;
        public byte[] BodyData;
        public System.IO.FileStream fs;
    }
}