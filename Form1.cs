using System;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Runtime.InteropServices;  


namespace piano_pdf_tool
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll")]
        static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags); 

        [FlagsAttribute]
        public enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
        }

        static SerialPort Arduino1;     // serial connection
        private bool ArduinoReady;      // true if arduinoconnection is established
        string filename;

        int LastPage;
        int Page1;
        int Page2;

        public Form1()
        {
            InitializeComponent();
            axAcroPDF1.setShowScrollbars(false);
            axAcroPDF2.setShowScrollbars(false);
            SetSize();

            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_DISPLAY_REQUIRED); // prevents screen from locking

            button1.KeyDown += gotoNext;
            numericUpDown1.KeyDown += gotoNext;
            button4.KeyDown += gotoNext;
            comboBox1.KeyDown += gotoNext;
        }

        #region Clickables

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();

            if(fd.ShowDialog() == DialogResult.OK)
            {
                filename = fd.FileName;
                axAcroPDF1.src = filename;
                axAcroPDF2.src = filename;
                
                axAcroPDF2.gotoNextPage();
                Page1 = 1;
                Page2 = 2;
                LastPage = getNrOfPages(fd.FileName);

                SetSize();
            }

            else
            {
                MessageBox.Show("select PDF file");
            }
            
        }

        private void comboBox1_Click(object sender, EventArgs e) // if ports combobox clicked, get all ports
        {
            setInputPorts();
        }

        private void button4_Click(object sender, EventArgs e) // connect to Arduino and begin listening for serial commands.
        {
            setupArduino();
            Arduino1.DataReceived += new SerialDataReceivedEventHandler(TurnPage);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            axAcroPDF1.setView("Fit");
        }

        #endregion
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            axAcroPDF1.setZoom((float)numericUpDown1.Value);
            axAcroPDF2.setZoom((float)numericUpDown1.Value);
        }

        private void gotoNext(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D2)
            {
                Scroll2Forward();
            }

            if (e.KeyCode == Keys.D1)
            {
                Scroll2Backward();
            }
        }

        public void TurnPage(object sender, System.IO.Ports.SerialDataReceivedEventArgs a)
        {
            //int c = axAcroPDF1.AccessibilityObject.Bounds.Bottom;
            int b = Arduino1.ReadByte();

            if (b == 1)
            {
                this.Invoke((MethodInvoker)delegate ()
                {
                    //axAcroPDF1.gotoNextPage();
                    Scroll2Forward();
                });

            }
            if (b == 0)
            {

                this.Invoke((MethodInvoker)delegate ()
                {
                    //axAcroPDF1.gotoPreviousPage();
                    Scroll2Backward();
                });
            }
            b = 2;
        }

        private void setupArduino() // setup connection with Arduino
        {
            if (ArduinoReady == false)
            {
                try
                {
                    Arduino1 = new SerialPort();
                    Arduino1.PortName = comboBox1.Text;
                    Arduino1.BaudRate = 9600;
                    Arduino1.ReadTimeout = 10000;
                    Arduino1.Open();
                    ArduinoReady = true;
                    button4.Enabled = false; // grey out 'connect' button.
                }
                catch (ArgumentException)
                {
                    MessageBox.Show("Error, check port or physical connection with Arduino");
                }
                catch (System.IO.IOException)
                {
                    MessageBox.Show("Error, time-out period expired. Try a different port");
                }
            }

            else
            {
                MessageBox.Show("Error, connection already exists");
            }
        }
        private void setInputPorts() // find all occupied USB ports add them to combobox
        {
            string[] allPortNames;
            allPortNames = SerialPort.GetPortNames();
            int nrPorts = allPortNames.Length;

            comboBox1.Items.Clear();
            for (int i = 0; i < nrPorts; i++)
            {
                comboBox1.Items.Add(allPortNames[i]);
            }
        }

        public void Scroll2Forward()
        {
            if (Page1 < LastPage-2)
            {
                Page1 += 2;
                Page2 += 2;
                axAcroPDF1.setCurrentPage(Page1);
                axAcroPDF2.setCurrentPage(Page2);
            }
            else if (Page1 == LastPage - 2)
            {
                Page1 += 2;
                Page2 += 2;
                axAcroPDF1.setCurrentPage(Page1);
                ShowEmpty();
            }
        }

        public void Scroll2Backward()
        {
            if (Page1 > 2 && Page2 <= LastPage)
            {
                Page1 -= 2;
                Page2 -= 2;
                axAcroPDF1.setCurrentPage(Page1);
                axAcroPDF2.setCurrentPage(Page2);
            }
            else if (Page1 > 2 && Page2 > LastPage)
            {
                Page1 -= 2;
                Page2 -= 2;
                axAcroPDF2.src = filename;
                axAcroPDF2.setCurrentPage(Page2);
                axAcroPDF1.setCurrentPage(Page1);
            }
        }

        public void ShowEmpty()
        {
            axAcroPDF2.LoadFile("Empty");
        }

        public int getNrOfPages(string filename)
        {
            int pagecount;
            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            StreamReader r = new StreamReader(fs);
            string pdfText = r.ReadToEnd();

            System.Text.RegularExpressions.Regex regx = new System.Text.RegularExpressions.Regex(@"/Type\s*/Page[^s]");
            System.Text.RegularExpressions.MatchCollection matches = regx.Matches(pdfText);
            pagecount = matches.Count;

            return pagecount;
        }

        public void FormResize(object sender, EventArgs e)
        {
            SetSize();   
        }

        public void SetSize()
        {
            axAcroPDF1.Height = Height - 85;
            axAcroPDF2.Height = Height - 85;
            axAcroPDF1.Width = (int)(Width * 0.5) - 20;
            axAcroPDF2.Width = (int)(Width * 0.5) - 20;

            axAcroPDF1.setViewScroll("FitB", (float)numericUpDown1.Value);
            axAcroPDF2.setViewScroll("FitB", (float)numericUpDown1.Value);

            axAcroPDF1.setZoom((float)numericUpDown1.Value);
            axAcroPDF2.setZoom((float)numericUpDown1.Value);

            panel1.Height = Height - 85;
            panel2.Height = Height - 85;
            panel1.Width = (int)(Width * 0.5) - 20;
            panel2.Width = (int)(Width * 0.5) - 20;
            panel2.Location = new System.Drawing.Point((int)(0.5 * this.Width)-5, 38);
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS); // turn on possibility to lock the screen again.
        }
    }
}
