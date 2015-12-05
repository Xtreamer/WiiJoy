using System;
using System.Linq;
using System.IO.Ports;
using System.Reflection;
using System.Threading;
using System.Diagnostics;

namespace SerialPortListener.Serial
{
    public class SerialPortManager : IDisposable
    {
        private SerialPort _serialPort;
        private SerialSettings _currentSerialSettings = new SerialSettings();
        private string _latestRecieved = string.Empty;
        public event EventHandler<SerialDataEventArgs> NewSerialDataRecieved;
        Thread senderThread;
        private byte[] request;
        private Stopwatch stopwatch;

        public SerialPortManager()
        {
            //stopwatch = new Stopwatch();
            //stopwatch.Start();

            request = new byte[] { (byte)'$', (byte)'M', (byte)'<', 0, 105, 105 };

            _currentSerialSettings.PortNameCollection = SerialPort.GetPortNames();
            _currentSerialSettings.PropertyChanged += CurrentSerialSettingsPropertyChanged;

            if (_currentSerialSettings.PortNameCollection.Length > 0)
                _currentSerialSettings.PortName = _currentSerialSettings.PortNameCollection.First();
        }

        public SerialSettings CurrentSerialSettings
        {
            get { return _currentSerialSettings; }
            set { _currentSerialSettings = value; }
        }

        void CurrentSerialSettingsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("PortName"))
                UpdateBaudRateCollection();
        }

        void OnSerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //Console.WriteLine("Elapsed {0}", stopwatch.ElapsedMilliseconds);
            //stopwatch.Restart();

            int dataLength = _serialPort.BytesToRead;
            byte[] data = new byte[dataLength];
            int bytesRead = _serialPort.Read(data, 0, dataLength);

            //Console.WriteLine(bytesRead);

            if (bytesRead == 0)
                return;

            if (NewSerialDataRecieved != null)
                NewSerialDataRecieved(this, new SerialDataEventArgs(data, bytesRead));
        }

        public void StartListening()
        {
            if (_serialPort != null && _serialPort.IsOpen)
                _serialPort.Close();

            _serialPort = new SerialPort(
                _currentSerialSettings.PortName,
                _currentSerialSettings.BaudRate,
                _currentSerialSettings.Parity,
                _currentSerialSettings.DataBits,
                _currentSerialSettings.StopBits);

            _serialPort.BaudRate = 115200;
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;

            _serialPort.DataReceived += OnSerialPortDataReceived;
            _serialPort.Open();

            StartSending();
        }

        private void StartSending()
        {
            var threadStart = new ThreadStart(Run);
            senderThread = new Thread(threadStart);
            senderThread.SetApartmentState(ApartmentState.STA);
            senderThread.Start();
        }

        private void Run()
        {
            while (true)
            {
                Thread.Sleep(10);
                if (!_serialPort.IsOpen)
                    break;

                _serialPort.Write(request, 0, request.Length);               
            }
        }

        public void StopListening()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        private void UpdateBaudRateCollection()
        {
            _serialPort = new SerialPort(_currentSerialSettings.PortName);
            _serialPort.Open();
            object p = _serialPort.BaseStream.GetType().GetField("commProp", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_serialPort.BaseStream);
            var dwSettableBaud = (int)p.GetType().GetField("dwSettableBaud", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(p);

            _serialPort.Close();
            _currentSerialSettings.UpdateBaudRateCollection(dwSettableBaud);
        }

        ~SerialPortManager()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_serialPort != null)
            {
                if (disposing)
                {
                    _serialPort.DataReceived -= OnSerialPortDataReceived;
                }
                if (_serialPort.IsOpen)
                    _serialPort.Close();

                _serialPort.Dispose();
            }
        }
    }

    public class SerialDataEventArgs : EventArgs
    {
        public SerialDataEventArgs(byte[] dataInByteArray, int length)
        {
            Data = dataInByteArray;
            Length = length;
        }

        public byte[] Data;
        public int Length { get; set; }
    }
}
