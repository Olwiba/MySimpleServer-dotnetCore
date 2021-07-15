using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace MySimpleServer
{
    public class Server
    {
        private int _threadId = 0;
        public void Start()
        {
            Console.WriteLine("Server start!");

            TcpListener listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 3360);
                listener.Start();
                while (true)
                {
                    var client = listener.AcceptTcpClient();
                    var thread = new Thread(() => RunServer(client, _threadId));
                    thread.Start();
                    _threadId++;
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine($"Socket exception: {e}");
            }
            finally
            {
                listener?.Stop();
            }
        }

        private void RunServer(TcpClient client, int id)
        {
            Console.WriteLine($"Client {id} accepted!");

            var networkStream = client.GetStream();
            var reader = new StreamReader(networkStream);
            var writer = new StreamWriter(networkStream) {AutoFlush = true};

            writer.WriteLine($"====================================\r\n");
            writer.WriteLine($"Welcome to MySimpServer\r\n");
            writer.WriteLine($"====================================\r\n");
            var requestBuilder = new List<string>();

            do
            {
                // Accept messages until newline is found
                var receivedMessage = reader.ReadLine();
                requestBuilder.Add(receivedMessage);
                if (!string.IsNullOrEmpty(receivedMessage)) continue;

                // Request submitted
                RequestBase requestBase = ParseRequestBase(requestBuilder[0], out var errorCode);

                // Handle bad requests
                if (requestBase == null || errorCode >= 0)
                {
                    writer.WriteLine(GetErrorResponse(errorCode.ToString(), "Bad Request"));
                    requestBuilder.Clear();
                    continue;
                }
                requestBuilder.Clear();

                // Handle request types
                if (requestBase.RequestType == "GET")
                {
                    // Set headers and perform logic
                    var requestedFile = File.ReadAllLines(requestBase.Path);
                    var mimeType = GetMimeType(requestBase.Path);
                    writer.WriteLine($"{requestBase.Protocol} 200 OK");
                    writer.WriteLine($"Date: {DateTime.UtcNow}");
                    writer.WriteLine("Expires: -1");
                    writer.WriteLine("Cache-Control: private, max-age=0");
                    writer.WriteLine($"Content-Type: {mimeType}");
                    writer.WriteLine(String.Join("\r\n", requestedFile));
                }
                else
                {
                    writer.WriteLine("Please use a GET request!!1");
                }

                // Generic exit strategy
                if (receivedMessage is "exit" || receivedMessage is "^C")
                {
                    client.Close();
                    break;
                }
            }
            while (client.Connected);

            Console.WriteLine($"Client {id} disconnected!");
            reader.Close();
            writer.Close();
            networkStream.Close();
        }

        private RequestBase ParseRequestBase(string request, out int error)
        {
            var requestBase = request.Split(" ");

            error = -1;
            if (requestBase.Length < 2)
            {
                error = 400;
                return null;
            }

            var requestType = requestBase[0];
            var requestPath = $"content/{requestBase[1]}";
            var protocol = requestBase.Length == 3 ? requestBase[2] : "HTTP/1.1";

            return new RequestBase(requestType, requestPath, protocol);
        }

        private static string GetErrorResponse(string code, string message)
        {
            return @$"HTTP/1.1 {code} {message}
Content-Length: {37 + code.Length + message.Length}
Content-Type: text/html; charset=UTF-8
Date: {DateTime.UtcNow}

<html><title>Error {code} ({message})</title></html>";
        }

        private static string GetMimeType(string path)
        {
            var provider = new FileExtensionContentTypeProvider();

            if(!provider.TryGetContentType(path, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return contentType;
        }
    }
}