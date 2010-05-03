using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Collections;
using System.Web;
using System.Threading;

namespace BasicHTTPServer
{
    public enum RState
    {
        METHOD, URL, URLPARM, URLVALUE, VERSION,
        HEADERKEY, HEADERVALUE, BODY, OK
    };

    public enum RespState
    {
        OK = 200,
        BAD_REQUEST = 400,
        NOT_FOUND = 404
    }

    public class HTTPRequest
    {
        public TcpClient client;
        public RState ParserState;
        public HTTPRequestStruct _HTTPRequest;
        public HTTPResponseStruct _HTTPResponse;
        public byte[] myReadBuffer;
        public HTTPServer Parent;

		public HTTPRequest(TcpClient client, HTTPServer Parent) 
		{
			this.client = client;
			this.Parent = Parent;

			this._HTTPResponse.BodySize = 0;
		}

		public void Process()
		{
			myReadBuffer = new byte[client.ReceiveBufferSize];
			String myCompleteMessage = "";
			int numberOfBytesRead = 0;

			//Connection accepted. Buffering...
			NetworkStream ns = client.GetStream();

			string hValue = "";
			string hKey = "";

			try 
			{
				// binary data buffer index
				int bfndx = 0;

				// Incoming message may be larger than the buffer size.
				do
				{
					numberOfBytesRead = ns.Read(myReadBuffer, 0, myReadBuffer.Length);  
					myCompleteMessage = 
						String.Concat(myCompleteMessage, Encoding.ASCII.GetString(myReadBuffer, 0, numberOfBytesRead));  
					
					// read buffer index
					int ndx = 0;
					do
					{
						switch ( ParserState )
						{
							case RState.METHOD:
								if (myReadBuffer[ndx] != ' ')
									_HTTPRequest.Method += (char)myReadBuffer[ndx++];
								else 
								{
									ndx++;
									ParserState = RState.URL;
								}
								break;
							case RState.URL:
								if (myReadBuffer[ndx] == '?')
								{
									ndx++;
									hKey = "";
									_HTTPRequest.Execute = true;
									_HTTPRequest.Args = new Hashtable();
									ParserState = RState.URLPARM;
								}
								else if (myReadBuffer[ndx] != ' ')
									_HTTPRequest.URL += (char)myReadBuffer[ndx++];
								else
								{
									ndx++;
									_HTTPRequest.URL = HttpUtility.UrlDecode(_HTTPRequest.URL);
									ParserState = RState.VERSION;
								}
								break;
							case RState.URLPARM:
								if (myReadBuffer[ndx] == '=')
								{
									ndx++;
									hValue="";
									ParserState = RState.URLVALUE;
								}
								else if (myReadBuffer[ndx] == ' ')
								{
									ndx++;

									_HTTPRequest.URL = HttpUtility.UrlDecode(_HTTPRequest.URL);
									ParserState = RState.VERSION;
								}
								else
								{
									hKey += (char)myReadBuffer[ndx++];
								}
								break;
							case RState.URLVALUE:
								if (myReadBuffer[ndx] == '&')
								{
									ndx++;
									hKey=HttpUtility.UrlDecode(hKey);
									hValue=HttpUtility.UrlDecode(hValue);
									_HTTPRequest.Args[hKey] =  _HTTPRequest.Args[hKey] != null ? _HTTPRequest.Args[hKey] + ", " + hValue : hValue;
									hKey="";
									ParserState = RState.URLPARM;
								}
								else if (myReadBuffer[ndx] == ' ')
								{
									ndx++;
									hKey=HttpUtility.UrlDecode(hKey);
									hValue=HttpUtility.UrlDecode(hValue);
									_HTTPRequest.Args[hKey] =  _HTTPRequest.Args[hKey] != null ? _HTTPRequest.Args[hKey] + ", " + hValue : hValue;
									
									_HTTPRequest.URL = HttpUtility.UrlDecode(_HTTPRequest.URL);
									ParserState = RState.VERSION;
								}
								else
								{
									hValue += (char)myReadBuffer[ndx++];
								}
								break;
							case RState.VERSION:
								if (myReadBuffer[ndx] == '\r') 
									ndx++;
								else if (myReadBuffer[ndx] != '\n') 
									_HTTPRequest.Version += (char)myReadBuffer[ndx++];
								else 
								{
									ndx++;
									hKey = "";
									_HTTPRequest.Headers = new Hashtable();
									ParserState = RState.HEADERKEY;
								}
								break;
							case RState.HEADERKEY:
								if (myReadBuffer[ndx] == '\r') 
									ndx++;
								else if (myReadBuffer[ndx] == '\n')
								{
									ndx++;
									if (_HTTPRequest.Headers["Content-Length"] != null)
									{
										_HTTPRequest.BodySize = Convert.ToInt32(_HTTPRequest.Headers["Content-Length"]);
										this._HTTPRequest.BodyData = new byte[this._HTTPRequest.BodySize];
										ParserState = RState.BODY;
									}
									else
										ParserState = RState.OK;
									
								}
								else if (myReadBuffer[ndx] == ':')
									ndx++;
								else if (myReadBuffer[ndx] != ' ')
									hKey += (char)myReadBuffer[ndx++];
								else 
								{
									ndx++;
									hValue = "";
									ParserState = RState.HEADERVALUE;
								}
								break;
							case RState.HEADERVALUE:
								if (myReadBuffer[ndx] == '\r') 
									ndx++;
								else if (myReadBuffer[ndx] != '\n')
									hValue += (char)myReadBuffer[ndx++];
								else 
								{
									ndx++;
									_HTTPRequest.Headers.Add(hKey, hValue);
									hKey = "";
									ParserState = RState.HEADERKEY;
								}
								break;
							case RState.BODY:
								// Append to request BodyData
								Array.Copy(myReadBuffer, ndx, this._HTTPRequest.BodyData, bfndx, numberOfBytesRead - ndx);
								bfndx += numberOfBytesRead - ndx;
								ndx = numberOfBytesRead;
								if ( this._HTTPRequest.BodySize <=  bfndx)
								{
									ParserState = RState.OK;
								}
								break;
								//default:
								//	ndx++;
								//	break;

						}
					}
					while(ndx < numberOfBytesRead);

				}
				while(ns.DataAvailable);
				
				_HTTPResponse.version = "HTTP/1.1";

				if (ParserState != RState.OK)
					_HTTPResponse.status = (int)RespState.BAD_REQUEST;
				else
					_HTTPResponse.status = (int)RespState.OK;

				this._HTTPResponse.Headers = new Hashtable();
				this._HTTPResponse.Headers.Add("Server", Parent.Name);
				this._HTTPResponse.Headers.Add("Date", DateTime.Now.ToString("r"));

                HTTPRequestHandler.OnRequest(this, ref ns);
			}
			catch (Exception e)
			{
                //Exception!
                Console.WriteLine(e.Message);
			}
			finally 
			{
				ns.Close();
				client.Close();
				if (this._HTTPResponse.fs != null)
					this._HTTPResponse.fs.Close();
				Thread.CurrentThread.Abort();
			}
		}
    }
}