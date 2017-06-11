using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AquosRemoteControl
{
    public class AquosConnector
    {
        private readonly IPAddress address;
        private readonly ushort port;
        private readonly string username;
        private readonly string password;

        private AquosStreamReaderWriter readerWriter;

        private readonly ILoggerFactory loggerFactory;

        public AquosConnector(IPAddress address, ushort port, ILoggerFactory loggerFactory = null)
            : this(address, port, null, null, loggerFactory)
        {
        }

        public AquosConnector(IPAddress address, ushort port, string username, string password, ILoggerFactory loggerFactory = null)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            if (port == 0)
                throw new ArgumentOutOfRangeException(nameof(port));

            this.address = address;
            this.port = port;
            this.username = username ?? string.Empty;
            this.password = password ?? string.Empty;

            this.loggerFactory = loggerFactory;
        }

        public async Task Connect()
        {
            var client = new TcpClient();

            await client.ConnectAsync(address, port);

            Stream stream = client.GetStream();

            readerWriter = new AquosStreamReaderWriter(stream, loggerFactory);

            if (username.Length > 0 || password.Length > 0)
                await Login();
        }

        private async Task Login()
        {
            string response = await readerWriter.ReadAsync(true);

            if (response == null)
                throw new Exception("Login timeout (login phase)");

            if (response != "Login:")
                throw new Exception("Expected string 'Login:'");

            await readerWriter.WriteAsync(username);

            response = await readerWriter.ReadAsync(true);

            if (response == null)
                throw new Exception("Login timeout (password phase)");

            if (response != "Password:")
                throw new Exception("Expected string 'Password:'");

            await readerWriter.WriteAsync(password);
        }

        public async Task<string> Send(string command)
        {
            await readerWriter.WriteAsync(command);

            return await readerWriter.ReadAsync();
        }
    }
}
