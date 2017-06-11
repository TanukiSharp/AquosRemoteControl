using System;
using System.Net;
using System.Threading.Tasks;
using AquosRemoteControl;
using Microsoft.Extensions.Logging;

namespace TestConsoleApp
{
    class Program
    {
        private static readonly IPAddress Address = IPAddress.Parse("<ip-address-here>");
        private const ushort Port = 10000;
        private const string Username = "<username>";
        private const string Password = "<password>";

        static void Main(string[] args)
        {
            new Program().Run().Wait();
        }

        private async Task Run()
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole(LogLevel.Trace);

            var connector = new AquosConnector(Address, Port, Username, Password, loggerFactory);

            await connector.Connect();

            await connector.Send("POWR1   ");

            for (int i = 0; i < 10; i++)
            {
                await connector.Send("MUTE0   ");
                await Task.Delay(1000);
            }

            await connector.Send("POWR0   ");

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
        }
    }
}
