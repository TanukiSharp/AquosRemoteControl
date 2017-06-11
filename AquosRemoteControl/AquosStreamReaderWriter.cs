using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AquosRemoteControl
{
    public class AquosStreamReaderWriter
    {
        private readonly ILogger logger;
        private readonly Stream stream;

        private byte[] readBuffer = new byte[512];
        private byte[] writeBuffer = new byte[512];
        private int readOffset;
        private int readCount;

        public AquosStreamReaderWriter(Stream stream, ILoggerFactory loggerFactory = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            this.stream = stream;

            if (loggerFactory != null)
                logger = loggerFactory.CreateLogger(nameof(AquosStreamReaderWriter));
        }

        private static string EscapeString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return input.Replace(" ", "[SP]").Replace("\n", "[LF]").Replace("\r", "[CR]");
        }

        public Task WriteAsync(string data)
        {
            int bytesWritten = Encoding.ASCII.GetBytes(data, 0, data.Length, writeBuffer, 0);
            if (bytesWritten >= writeBuffer.Length)
                throw new InvalidOperationException("Too large data to write");

            writeBuffer[bytesWritten] = (byte)'\r';

            logger?.LogTrace($"PC -> TV: '{EscapeString(data)}'");

            return stream.WriteAsync(writeBuffer, 0, bytesWritten + 1);
        }

        public async Task<string> ReadAsync(bool skipLineFeedCheck = false)
        {
            while (true)
            {
                while (readBuffer[readOffset] == 10 || readBuffer[readOffset] == 13)
                {
                    readOffset++;
                    readCount--;
                }

                int dataEndIndex = Array.IndexOf(readBuffer, (byte)'\r', readOffset, readCount);

                if (dataEndIndex > -1 || (skipLineFeedCheck && readCount > 0))
                {
                    int resultLength;

                    if (dataEndIndex > -1)
                        resultLength = dataEndIndex - readOffset;
                    else
                        resultLength = readCount;

                    string result = Encoding.ASCII.GetString(readBuffer, readOffset, resultLength);

                    if (readOffset > 0)
                    {
                        Array.Copy(readBuffer, readOffset, readBuffer, 0, resultLength);
                        readOffset = 0;
                    }

                    readCount -= resultLength;

                    logger?.LogTrace($"TV -> PC: '{EscapeString(result)}'");

                    return result;
                }
                else
                {
                    int bytesRead;
                    var cancellationTokenSource = new CancellationTokenSource(3000);

                    try
                    {
                        bytesRead = await stream.ReadAsync(readBuffer, readOffset, readBuffer.Length - readOffset, cancellationTokenSource.Token);
                        readCount += bytesRead;
                    }
                    catch (OperationCanceledException)
                    {
                        return null;
                    }
                }
            }
        }
    }
}
