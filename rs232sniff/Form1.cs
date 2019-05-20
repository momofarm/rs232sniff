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

            w = new Worker();

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
            t.Join();

        }
    }

    public class Worker
    {
        public event EventHandler<MessageEventArgs> recevedMesg;
        public event EventHandler<MessageEventArgs> eventMsgSent;

        private static bool shouldStopped = false;

        

        public void Work()
        {
            try
            {
                while (!shouldStopped)
                {
                    //string txt = "test read"; 
                        //ReadCmd("", "COM1");

                    //System.Threading.Thread.Sleep(1000);

                    //SendCmd(txt, "COM3");

                    //System.Threading.Thread.Sleep(1000);

                    // read response from com3

                    string response = ReadCmd("", "COM5");

                    //System.Threading.Thread.Sleep(1000);

                    //if (response.Length > 0)
                    SendCmd(response, "COM8");

                    string response2 = ReadCmd("", "COM8");

                    SendCmd(response2, "COM5");

                    //System.Threading.Thread.Sleep(1000);

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

        private string SendCmd(string cmd, string strComport)
        {
            SerialPort serial_conn = null;

            string msg = "";

            try
            {
                string[] allPortNames = System.IO.Ports.SerialPort.GetPortNames();


                serial_conn = new SerialPort(strComport, 9600, Parity.None, 8, StopBits.One);

                serial_conn.Open();

                string cmd_to_send = cmd;// + "\r\n";

                serial_conn.Write(cmd_to_send); //weight

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
                serial_conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                MessageEventArgs e2 = new MessageEventArgs();

                e2.Message = e.Message;

                OnSentMsg(e2);

                //throw new Exception(e.Message);

            }

            if (serial_conn != null)
                serial_conn.Close();

            return "";
        }


        private string ReadCmd(string cmd, string strComport)
        {
            SerialPort serial_conn = null;

            string msg = "";

            try
            {
                string[] allPortNames = System.IO.Ports.SerialPort.GetPortNames();


                serial_conn = new SerialPort(strComport, 9600, Parity.None, 8, StopBits.One);

                serial_conn.Open();

                int count = 0;

                do
                {
                    System.Threading.Thread.Sleep(1000);

                    msg = serial_conn.ReadExisting();

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
                serial_conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                //throw new Exception(e.Message);
                MessageEventArgs e2 = new MessageEventArgs();

                e2.Message = e.Message;

                OnSentMsg(e2);
            }

            if (serial_conn != null)
                serial_conn.Close();
            
                return msg;
        }
        
    }

    public class MessageEventArgs : EventArgs
    {
        public string Message { get; set; }
    }
}
