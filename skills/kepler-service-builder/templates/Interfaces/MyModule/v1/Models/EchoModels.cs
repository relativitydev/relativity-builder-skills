using System;

namespace MyCompany.MyApp.Interfaces.MyModule.v1.Models
{
    public class EchoRequest
    {
        public string Message { get; set; }
    }

    public class EchoResponse
    {
        public string Message { get; set; }

        public DateTime ReceivedUtc { get; set; }
    }
}
