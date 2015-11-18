using System;
using System.Windows.Forms;
using SerialPortListener.Serial;

namespace SerialPortListener
{
    public partial class MainForm : Form
    {
        SerialPortManager _serialPortManager;
        SerialPortDataParser _parser;

        public MainForm()
        {
            InitializeComponent();
            UserInitialization();
        }
      
        private void UserInitialization()
        {
            _parser = new SerialPortDataParser();
            _serialPortManager = new SerialPortManager();           
            var mySerialSettings = _serialPortManager.CurrentSerialSettings;
            serialSettingsBindingSource.DataSource = mySerialSettings;
            portNameComboBox.DataSource = mySerialSettings.PortNameCollection;
            baudRateComboBox.DataSource = mySerialSettings.BaudRateCollection;
            dataBitsComboBox.DataSource = mySerialSettings.DataBitsCollection;
            parityComboBox.DataSource = Enum.GetValues(typeof(System.IO.Ports.Parity));
            stopBitsComboBox.DataSource = Enum.GetValues(typeof(System.IO.Ports.StopBits));

            _serialPortManager.NewSerialDataRecieved += NewSerialDataRecieved;
            _serialPortManager.NewSerialDataRecieved += _parser.NewSerialDataRecieved;
            _parser.NewPositionDataRecieved += Parser_NewPositionDataRecieved;
            this.FormClosing += MainForm_FormClosing;            
        }

        private void Parser_NewPositionDataRecieved(object sender, SerialPortDataParser.PositionDataEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler<SerialPortDataParser.PositionDataEventArgs>(Parser_NewPositionDataRecieved), new object[] { sender, e });
                return;
            }

            int maxTextLength = 1000;
            if (tbData.TextLength > maxTextLength)
                tbData.Text = tbData.Text.Remove(0, tbData.TextLength - maxTextLength);
            
            rollTextBox.Text = e.Roll.ToString();
            pitchTextBox.Text = e.Pitch.ToString();
            yawTextBox.Text = e.Yaw.ToString();
            throttleTextBox.Text = e.Throttle.ToString();
            aux1TextBox.Text = e.Aux1.ToString();

            elapsedLabel.Text = e.TimeSinceLast.ToString() + " ms";
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _serialPortManager.Dispose();   
        }

        void NewSerialDataRecieved(object sender, SerialDataEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler<SerialDataEventArgs>(NewSerialDataRecieved), new object[] { sender, e });
                return;
            }            
        }
        
        private void btnStart_Click(object sender, EventArgs e)
        {
            _serialPortManager.StartListening();
        }
        
        private void btnStop_Click(object sender, EventArgs e)
        {
            _serialPortManager.StopListening();
        }
    }
}
