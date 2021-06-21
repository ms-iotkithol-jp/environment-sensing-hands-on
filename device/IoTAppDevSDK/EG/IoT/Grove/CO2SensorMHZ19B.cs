using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;

namespace EG.IoT.Grove
{
    public class CO2SensorMHZ19B
    {
        private SerialPort serialPort = null;
        public string Port { get; set; }
        public bool Initialize()
        {
            bool succeeded = true;
            if (string.IsNullOrEmpty(Port))
            {
                succeeded = false;
            }
            else
            {
                serialPort = new SerialPort(Port, 9600, Parity.None, 8, StopBits.One);
                try
                {
                    serialPort.Open();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    succeeded = false;
                }
            }
            return succeeded;
        }

        public int SensorValue()
        {
            int co2 = -1;
            if (serialPort != null)
            {
                var command = new byte[] { 0xff, 0x01, 0x86, 0x00, 0x00, 0x00, 0x00, 0x00, 0x79 };
                var readBuf = new byte[9];
                serialPort.Write(command, 0, command.Length);
                var readLen = serialPort.Read(readBuf, 0, readBuf.Length);
                while (readLen < readBuf.Length)
                {
                    if (readLen > 0)
                    {
                        if (readBuf[0] == command[0])
                        {
                            readLen += serialPort.Read(readBuf, readLen, readBuf.Length - readLen);
                        }
                    }
                }
                if (readLen == readBuf.Length)
                {
                    if (readBuf[0] == command[0] && readBuf[1] == command[2])
                        co2 = readBuf[2] * 256 + readBuf[3];
                }
            }
            return co2;
        }
    }
}
