namespace MySimpleServer
{
    public class RequestBase
    {
        public string RequestType { get; private set; }
        public string Path { get; private set; }
        public string Protocol { get; private set; }

        public RequestBase(string requestType, string requestPath, string protocol)
        {
            RequestType = requestType;
            Protocol = protocol;
            Path = requestPath;
        }
    }
}