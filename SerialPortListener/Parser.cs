using System;
using System.Threading;
using System.Diagnostics;
using SerialPortListener.Serial;

namespace SerialPortListener
{
    public class SerialPortDataParser
    {
        //Thread worker;
        //private bool _quit = false;
        private const int bufferSize = 200;
        private const int payloadLength = 16;
        private const int headerLength = 5;
        private byte[] buffer;
        //private int position;
        private byte[] responsePrefix;
        private short numberOfChannels = 4;
        private Stopwatch stopwatch;
        private long numberOfPackets = 0;
        private long badPackets = 0;

        public short NumberOfChannels
        {
            get { return numberOfChannels; }
            set { numberOfChannels = value; }
        }

        public event EventHandler<PositionDataEventArgs> NewPositionDataRecieved;

        public SerialPortDataParser()
        {
            buffer = new byte[bufferSize];
            responsePrefix = new byte[] { (byte)'$', (byte)'M', (byte)'>', 16, 105 };
            //position = 0;
            stopwatch = new Stopwatch();
            stopwatch.Start();
        }
        
        public void NewSerialDataReceived(object sender, SerialDataEventArgs e)
        {
            //numberOfPackets++;
            //Console.WriteLine("Elapsed {0}", stopwatch.ElapsedMilliseconds);
            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();
            
            if (e.Length == 0)
            {
                //badPackets++;
                return;
            }

            var data = e.Data;

            var startIndex = Array.LastIndexOf(data, responsePrefix[0]);
            if (startIndex < 0)
            {
                return;
            }

            var indexBefore = startIndex;
            var totalLength = numberOfChannels * 2 + headerLength;
            if (startIndex + totalLength > e.Length)
            {
                startIndex = Array.IndexOf(data, responsePrefix[0]);
                //startIndex = startIndex - (payloadLength + headerLength);
                //Console.WriteLine("before {0}, now {1}, length {2}", indexBefore, startIndex, e.Length);
                if (indexBefore == startIndex)
                {
                    //badPackets++;
                    //Console.WriteLine("badPackets: {0} %", ((float)badPackets / numberOfPackets) * 100);
                    return;
                }
            }
            //if (startIndex + 1 < e.Length && data[startIndex + 1] != responsePrefix[1])
            //{
            //    Console.WriteLine("not complete {0}", DateTime.Now);
            //    return;
            //}
            //if (startIndex != indexBefore)
            //    Console.WriteLine("good, index now: {0}, index before: {1}, length: {2}, left: {3}", startIndex, indexBefore, e.Length, e.Length - startIndex + 1);
            //Console.WriteLine("handle, length: {0}, firstIndex {1}, charAtFirst {2}", e.Length, startIndex, data[startIndex]);

            if (data.Length >= totalLength + startIndex &&
                data[startIndex] == responsePrefix[0] &&
                data[startIndex + 1] == responsePrefix[1] &&
                data[startIndex + 2] == responsePrefix[2])
            {
                var roll = GetPositionInteger(data, startIndex + 5);
                var pitch = GetPositionInteger(data, startIndex + 5 + 2);
                var yaw = GetPositionInteger(data, startIndex + 5 + 4);
                var throttle = GetPositionInteger(data, startIndex + 5 + 6);
                //var aux1 = GetPositionInteger(data, startIndex + 5 + 8);                
                //Console.WriteLine("{0}, {1}, {2}, {3}", roll, pitch, yaw, throttle);

                if (NewPositionDataRecieved != null)
                    NewPositionDataRecieved(this, new PositionDataEventArgs(roll, pitch, yaw, throttle, 0, elapsedMilliseconds));
            }
        }

        private short GetPositionInteger(byte[] data, int firstIndex)
        {
            return (short)(data[firstIndex + 1] << 8 | data[firstIndex]);
        }

        public class PositionDataEventArgs : EventArgs
        {
            public PositionDataEventArgs(short roll, short pitch, short yaw, short throttle, short aux1, long timeSinceLast)
            {
                Roll = roll;
                Pitch = pitch;
                Yaw = yaw;
                Throttle = throttle;
                Aux1 = aux1;
                TimeSinceLast = timeSinceLast;
            }

            public short Roll { get; set; }
            public short Pitch { get; set; }
            public short Yaw { get; set; }
            public short Throttle { get; set; }
            public short Aux1 { get; set; }
            public long TimeSinceLast { get; set; }
        }
    }    
}
