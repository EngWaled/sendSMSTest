using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
namespace WindowsFormsApplication1
{
    public partial class SMSForm : Form
    {
        public AutoResetEvent receiveNow;
        public SerialPort smsPort = new SerialPort();

        public SMSForm()
        {
            InitializeComponent();
        }

        private void SMSForm_Load(object sender, EventArgs e)
        {
            foreach(var p in SerialPort.GetPortNames())
            {
                cbPortnames.Items.Add(p.ToString());
            }
        }

        public SerialPort OpenPort(string portName, int baudRate, int dataBits, int readTimeout, int writeTimeout)
        {
            receiveNow = new AutoResetEvent(false);
            SerialPort port = new SerialPort();
            try
            {
                port.PortName = portName;                 //COM1
                port.BaudRate = baudRate;                 //9600
                port.DataBits = dataBits;                 //8
                port.StopBits = StopBits.One;             //1
                port.Parity = Parity.None;                //None
                port.ReadTimeout = readTimeout;           //300
                port.WriteTimeout = writeTimeout;         //300
                port.Encoding = Encoding.GetEncoding("iso-8859-1");
                port.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
                port.Open();
                port.DtrEnable = true;
                port.RtsEnable = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return port;
        }

        public void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (e.EventType == SerialData.Chars)
                {
                    receiveNow.Set();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void ClosePort(SerialPort port)
        {
            try
            {
                port.Close();
                port.DataReceived -= new SerialDataReceivedEventHandler(port_DataReceived);
                port = null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string SendATCommand(SerialPort port, string command, int responseTimeout, string errorMessage)
        {
            try
            {
                port.DiscardOutBuffer();
                port.DiscardInBuffer();
                receiveNow.Reset();
                port.Write(command + "\r");

                string input = ReadResponse(port, responseTimeout);
                if ((input.Length == 0) || ((!input.EndsWith("\r\n> "))
                            && (!input.EndsWith("\r\nOK\r\n"))))
                    throw new ApplicationException("No success message was received.");
                return input;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string ReadResponse(SerialPort port, int timeout)
        {
            string serialPortData = string.Empty;
            try
            {
                do
                {
                    if (receiveNow.WaitOne(timeout, false))
                    {
                        string data = port.ReadExisting();
                        serialPortData += data;
                    }
                    else
                    {
                        if (serialPortData.Length > 0)
                            throw new ApplicationException("Response received is incomplete.");
                        else
                            throw new ApplicationException("No data received from phone.");
                    }
                }
                while (!serialPortData.EndsWith("\r\nOK\r\n") &&
                 !serialPortData.EndsWith("\r\n> ") && !serialPortData.EndsWith("\r\nERROR\r\n"));
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return serialPortData;
        }

        public bool SendMessage(SerialPort port, string phoneNo, string message)
        {
            bool isSend = false;
            try
            {
                string recievedData = SendATCommand(port, "AT", 300, "No phone connected");
                string command = "AT+CMGF=1" + char.ConvertFromUtf32(13);
                recievedData = SendATCommand(port, command, 300, "Failed to set message format.");

                command = "AT+CMGS=\"" + phoneNo + "\"" + char.ConvertFromUtf32(13);
                recievedData = SendATCommand(port, command, 300,
                    "Failed to accept phoneNo");

                command = message + char.ConvertFromUtf32(26);
                recievedData = SendATCommand(port, command, 3000,
                    "Failed to send message"); //3 seconds

                if (recievedData.EndsWith("\r\nOK\r\n"))
                    isSend = true;
                else if (recievedData.Contains("ERROR"))
                    isSend = false;

                return isSend;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /*
        public ShortMessageCollection ReadSMS(SerialPort port, string atCommand)
        {
            // Set up the phone and read the messages
            ShortMessageCollection messages = null;
            try
            {

                #region Execute Command
                // Check connection
                SendATCommand(port, "AT", 300, "No phone connected");
                // Use message format "Text mode"
                SendATCommand(port, "AT+CMGF=1", 300, "Failed to set message format.");
                // Read the messages
                string input = SendATCommand(port, atCommand, 5000, "Failed to read the messages.");
                #endregion

                #region Parse messages
                messages = ParseMessages(input);
                #endregion

            }
            catch (Exception ex)
            {
                throw ex;
            }

            if (messages != null)
                return messages;
            else
                return null;
        }
        public ShortMessageCollection ParseMessages(string input)
        {
            ShortMessageCollection messages = new ShortMessageCollection();
            try
            {
                Regex r = new Regex(@"\+CMGL: (\d+),""(.+)"",""(.+)"",(.*),""(.+)""\r\n(.+)\r\n");
                Match m = r.Match(input);
                while (m.Success)
                {
                    ShortMessage msg = new ShortMessage();
                    msg.Index = m.Groups[1].Value;
                    msg.Status = m.Groups[2].Value;
                    msg.Sender = m.Groups[3].Value;
                    msg.Alphabet = m.Groups[4].Value;
                    msg.Sent = m.Groups[5].Value;
                    msg.Message = m.Groups[6].Value;
                    messages.Add(msg);
                    m = m.NextMatch();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return messages;
        }
        */

        public bool DeleteMessage(SerialPort port, string atCommand)
        {
            bool isDeleted = false;
            try
            {
                #region Execute Command
                string recievedData = SendATCommand(port, "AT", 300, "No phone connected");
                recievedData = SendATCommand(port, "AT+CMGF=1", 300, "Failed to set message format.");
                String command = atCommand;
                recievedData = SendATCommand(port, command, 300, "Failed to delete message");
                #endregion

                if (recievedData.EndsWith("\r\nOK\r\n"))
                {
                    isDeleted = true;
                }
                if (recievedData.Contains("ERROR"))
                {
                    isDeleted = false;
                }
                return isDeleted;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if(cbPortnames.Text != "")
            {
                if (smsPort.IsOpen == false)
                {
                    smsPort = OpenPort(cbPortnames.Text, 9600, 8, 300, 300);
                }
                //var sss = SendATCommand(smsPort, txtMessage.Text, 300, "happyMe");
                //txtLog.Text = sss;
                SendMessage(smsPort, txtNumber.Text, txtMessage.Text);
            }
            else
            {
                MessageBox.Show("Set your GSM PORT");
            }
        }
    }
}
