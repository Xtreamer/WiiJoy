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
        private byte[] buffer;
        //private int position;
        private byte[] responsePrefix;
        private Stopwatch stopwatch;
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
            //Console.WriteLine("Elapsed {0}", stopwatch.ElapsedMilliseconds);
            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();
            
            if (e.Length == 0)
            {
                return;
            }

            var data = e.Data;

            var startIndex = Array.LastIndexOf(data, responsePrefix[0]);
            if (startIndex < 0)
            {
                return;
            }
            if (startIndex + (16 + 6) < e.Length)
            {
                startIndex = startIndex - (16 + 6);
            }          

            //Console.WriteLine("handle, length: {0}, firstIndex {1}, charAtFirst {2}", e.Length, startIndex, data[startIndex]);
            
            if (data.Length > 15 &&
                data[0] == responsePrefix[0] &&
                data[1] == responsePrefix[1] &&
                data[2] == responsePrefix[2])
            {
                var roll = GetPositionInteger(data, 5);
                var pitch = GetPositionInteger(data, 5 + 2);
                var yaw = GetPositionInteger(data, 5 + 4);
                var throttle = GetPositionInteger(data, 5 + 6);
                var aux1 = GetPositionInteger(data, 5 + 8);
                //Console.WriteLine("{0}, {1}, {2}, {3}", roll, pitch, yaw, throttle);

                if (NewPositionDataRecieved != null)
                    NewPositionDataRecieved(this, new PositionDataEventArgs(roll, pitch, yaw, throttle, aux1, elapsedMilliseconds));
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
