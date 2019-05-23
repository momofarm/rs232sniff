using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace rs232sniff
{
    
    public partial class Form1 : Form
    {
        private static Thread t;

        private Worker w; 
        
        public Form1()
        {
            InitializeComponent();

            w = new Worker("COM8", "COM6");

            w.eventMsgSent += msgSent;

            w.recevedMesg += msgReceved;

            var config = new NLog.Config.LoggingConfiguration();

            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "c:\\rs232sniff\\log.txt" };

            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
            
            
            config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logconsole);

            config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, logfile);

            NLog.LogManager.Configuration = config;

            t = new Thread(w.Work);

            //t.Start();
            
        }

        private void writeLog(string log)
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Info(log);
        }

        void msgSent(object sender, MessageEventArgs e)
        {
            
            richTextBox1.Invoke(new Action(() =>
            {
                string textNow = richTextBox1.Text;

                textNow += "\r\n " + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz") + " " + e.Message;

                richTextBox1.Invoke(new Action(() => richTextBox1.Text = textNow));

                richTextBox1.SelectionStart = richTextBox1.SelectedText.Length;

                richTextBox1.ScrollToCaret();
            }));

            
        }


        void msgReceved(object sender, MessageEventArgs e)
        {
            
            richTextBox1.Invoke(new Action(() =>
            {
                string textNow = richTextBox1.Text;

                textNow += "\r\n " + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz") + " " + e.Message;

                richTextBox1.Invoke(new Action(() => richTextBox1.Text = textNow));

                richTextBox1.SelectionStart = richTextBox1.SelectedText.Length;

                richTextBox1.ScrollToCaret();
            }));
            
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
          
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            
            w.StopThread();
            w.disconn();
            t.Join();
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //  let's save current text in textbox
            string text = richTextBox1.Text;

            opensaveFileDialog(text);

            writeLog("[File Save] Save logging");
        }

        private void opensaveFileDialog(string text)
        {
            // Displays a SaveFileDialog so the user can save the Image  
            // assigned to Button2.  
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "Text File|*.txt";
            saveFileDialog1.Title = "Save an Text File";
            saveFileDialog1.ShowDialog();

            // If the file name is not an empty string open it for saving.  
            if (saveFileDialog1.FileName != "")
            {
                // Saves the Image via a FileStream created by the OpenFile method.  
                System.IO.FileStream fs =
                   (System.IO.FileStream)saveFileDialog1.OpenFile();
                // Saves the Image in the appropriate ImageFormat based upon the  
                // File type selected in the dialog box.  
                // NOTE that the FilterIndex property is one-based.  
                switch (saveFileDialog1.FilterIndex)
                {
                    case 1:
                    default:
                        byte[] info = new UTF8Encoding(true).GetBytes(text);
                        fs.Write(info, 0, info.Length);
                        break;
                }

                fs.Close();
            }
        }
    }

    public class Worker
    {
        public event EventHandler<MessageEventArgs> recevedMesg;
        public event EventHandler<MessageEventArgs> eventMsgSent;

        private static bool shouldStopped = false;
        private static SerialPort serial_conn1 = null;
        private static SerialPort serial_conn2 = null;


        public Worker(string com1, string com2)
        {
            serial_conn1 = new SerialPort(com1, 9600, Parity.None, 8, StopBits.One);

            serial_conn1.DataReceived += new SerialDataReceivedEventHandler(OnSerialRecvMsg_serial1);

            serial_conn1.Open();

            serial_conn2 = new SerialPort(com2, 9600, Parity.None, 8, StopBits.One);
            
            serial_conn2.DataReceived += new SerialDataReceivedEventHandler(OnSerialRecvMsg_serial2);

            serial_conn2.Open();
            
        }

        private void writeLog(string log)
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();

            logger.Info(log);
        }

        public void disconn()
        {
            writeLog("Worker: disconnecting serial");

            try
            {
                if (serial_conn1 != null)
                {
                    serial_conn1.Close();
                    writeLog("Worker: disconnecting serial conn1");

                }

                if (serial_conn2 != null)
                {
                    serial_conn2.Close();
                    writeLog("Worker: disconnecting serial conn2");

                }
            }
            catch(Exception e)
            {
                writeLog("Worker: disconnecting serial excepton catched " + e.Message);

            }


        }

        public void OnSerialRecvMsg_serial1(object sender, SerialDataReceivedEventArgs e)
        {
            string s = serial_conn1.ReadExisting();

            string msg = "serial1  getting message " + s + "\r\n";

            popMsg(s);

            //write to serial 2
            SendCmd(s, serial_conn2);

            // read from serial 2
            string r2 = ReadCmd("", serial_conn2);

            //send serial 1
            SendCmd(r2, serial_conn1);

        }
        
        public void OnSerialRecvMsg_serial2(object sender, SerialDataReceivedEventArgs e)
        {
            string s = serial_conn2.ReadExisting();

            string msg = "serial2  getting message " + s + "\r\n";

            popMsg(msg);

            //write to serial 1
            SendCmd(s, serial_conn1);

            // read from serial 1
            ReadCmd("", serial_conn1);

            //send serial 2
            SendCmd("", serial_conn2);
        }

        private void popMsg(string s)
        {
            if (s.Length > 0)
            {
                MessageEventArgs e2 = new MessageEventArgs();

                e2.Message = "serial1 gettiing message " + s + "\r\n";

                OnSentMsg(e2);

            }
        }

        public void Work()
        {
            try
            {
                while (!shouldStopped)
                {
                    // original command
                    string response = ReadCmd("", serial_conn1);
                    writeLog("Worker: Getting message from machine1 " + response);

                    //100 => partical working
                    System.Threading.Thread.Sleep(100);

                    //  test only
                    //response = "PC send msg\r\n";

                    if (response.Length > 0)
                    {
                        writeLog("Worker: sending message to machine2 " + response);

                        SendCmd(response, serial_conn2);

                        //System.Threading.Thread.Sleep(100);

                        string response2 = ReadCmd("", serial_conn2);

                        writeLog("Worker: Getting reply message from machine2 " + response2);

                        //  test only 
                        //response2 = "PC send msg2\r\n";

                        //System.Threading.Thread.Sleep(1000);

                        SendCmd(response2, serial_conn1);
                        writeLog("Worker: Sending reply message to machine1 " + response2);

                        System.Threading.Thread.Sleep(100);

                    }
                    else
                    {
                        System.Threading.Thread.Sleep(100);

                        continue;         
                    }

                }
            }
            catch (Exception e)
            {
                string exceptTxt = e.Message;

            }

        }

        protected virtual void OnRecevedMsg(MessageEventArgs e)
        {
            // Do something useful here.
            EventHandler<MessageEventArgs> handler = recevedMesg;

            if (handler != null)
            {
                handler(this, e);            }
        }

        protected virtual void OnSentMsg(MessageEventArgs e)
        {
            // Do something useful here.  
            EventHandler< MessageEventArgs> handler = eventMsgSent;

            if (handler != null)
            {
                handler(this, e);
            }
            
        }

        public void StopThread()
        {
            shouldStopped = true;
        }

        private string SendCmd(string cmd, SerialPort port)
        {
            string msg = "";

            try
            {
                
                string cmd_to_send = cmd;// + "\r\n";

                port.Write(cmd_to_send); //weight

                if (cmd_to_send.Length > 0)
                {
                    MessageEventArgs e = new MessageEventArgs();

                    e.Message = "sending message " + cmd_to_send + "\r\n";

                    OnSentMsg(e);

                }

                Console.WriteLine(msg);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                MessageEventArgs e2 = new MessageEventArgs();

                e2.Message = e.Message;

                OnSentMsg(e2);


            }

            return "";
        }


        private string ReadCmd(string cmd, SerialPort port)
        {
            string msg = "";

            try
            {
                int count = 0;
            
                do
                {
                    //100 => only one sampling process work
                    //50  ==> not working
                    //  80 ==> not working
                    //90 = partical working 
                    // 95
                    System.Threading.Thread.Sleep(100);

                    msg = port.ReadExisting();

                    if (msg.Length > 0)
                    {
                        MessageEventArgs e = new MessageEventArgs();

                        e.Message = msg;

                        OnRecevedMsg(e);
                        
                        break;
                    }
                    
                    count = count + 1;

                    if (count > 3)
                        break;
                        
                }
                while (true);
                
                Console.WriteLine(msg);

               
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                MessageEventArgs e2 = new MessageEventArgs();

                e2.Message = e.Message;

                OnSentMsg(e2);
            }
            
                return msg;
        }
        
    }

    public class MessageEventArgs : EventArgs
    {
        public string Message { get; set; }
    }
}
