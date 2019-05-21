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

            w = new Worker("COM7", "COM6");

            w.eventMsgSent += msgSent;

            w.recevedMesg += msgReceved;
            
            t = new Thread(w.Work);

            t.Start();
            
        }

        
        void msgSent(object sender, MessageEventArgs e)
        {
            //string textNow;

            //richTextBox1.Invoke(new Action(() => textNow = richTextBox1.Text));

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

            /*
            string textNow;

            richTextBox1.Invoke(new Action(() => textNow = richTextBox1.Text));

            textNow += "\r\n " + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz") + " " + e.Message;

            richTextBox1.Invoke(new Action(() => richTextBox1.Text = textNow));
            */

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

            serial_conn1.Open();

            serial_conn2 = new SerialPort(com2, 9600, Parity.None, 8, StopBits.One);

            serial_conn2.Open();
            
        }

        public void disconn()
        {
            if (serial_conn1 != null)
                serial_conn1.Close();

            if (serial_conn2 != null)
                serial_conn2.Close();

        }

        public void Work()
        {
            try
            {
                while (!shouldStopped)
                {
                    
                    // original command
                    string response = ReadCmd("", serial_conn1);

                    System.Threading.Thread.Sleep(3000);

                    //  test only
                    response = "PC send msg\r\n";

                    if (response.Length > 0)
                    {
                        SendCmd(response, serial_conn2);

                        System.Threading.Thread.Sleep(3000);

                        string response2 = ReadCmd("", serial_conn2);

                        //  test only 
                        response2 = "PC send msg2\r\n";

                        System.Threading.Thread.Sleep(3000);

                        SendCmd(response2, serial_conn1);

                    }
                    else
                    {
                        continue;
                        //System.Threading.Thread.Sleep(1000);

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
                handler(this, e);
            }
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
            SerialPort serial_conn = null;

            string msg = "";

            try
            {
                string[] allPortNames = System.IO.Ports.SerialPort.GetPortNames();


                //serial_conn = new SerialPort(strComport, 9600, Parity.None, 8, StopBits.One);

                //serial_conn.Open();

                string cmd_to_send = cmd;// + "\r\n";

                port.Write(cmd_to_send); //weight

                if (cmd_to_send.Length > 0)
                {
                    MessageEventArgs e = new MessageEventArgs();

                    e.Message = "sending message " + cmd_to_send + "\r\n";

                    OnSentMsg(e);

                }


                int count = 0;

                //S S     0.0000 g\r
                //S S     1.1752 g\r
                Console.WriteLine(msg);

                //MessageBox.Show(msg);
                //textBox2.Text = msg;
                //textBox2.Enabled = true;
                //serial_conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                MessageEventArgs e2 = new MessageEventArgs();

                e2.Message = e.Message;

                OnSentMsg(e2);

                //throw new Exception(e.Message);

            }

            //if (serial_conn != null)
                //serial_conn.Close();

            return "";
        }


        private string ReadCmd(string cmd, SerialPort port)
        {
            //SerialPort serial_conn = null;

            string msg = "";

            try
            {
                string[] allPortNames = System.IO.Ports.SerialPort.GetPortNames();


                //serial_conn = new SerialPort(strComport, 9600, Parity.None, 8, StopBits.One);

                //serial_conn.Open();

                int count = 0;

                do
                {
                    System.Threading.Thread.Sleep(1000);

                    msg = port.ReadExisting();

                    if (msg.Length > 0)
                    {
                        MessageEventArgs e = new MessageEventArgs();

                        e.Message = msg;

                        OnRecevedMsg(e);
                        
                        break;
                    }
                    
                    /*
                    if (msg.Length > 0)
                        break;
                    */    
                    count = count + 1;

                    if (count > 3)
                        break;
                        
                }
                while (true);


                //S S     0.0000 g\r
                //S S     1.1752 g\r
                Console.WriteLine(msg);

                //MessageBox.Show(msg);
                //textBox2.Text = msg;
                //textBox2.Enabled = true;
                //serial_conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                //throw new Exception(e.Message);
                MessageEventArgs e2 = new MessageEventArgs();

                e2.Message = e.Message;

                OnSentMsg(e2);
            }

            //if (serial_conn != null)
            //    serial_conn.Close();
            
                return msg;
        }
        
    }

    public class MessageEventArgs : EventArgs
    {
        public string Message { get; set; }
    }
}
