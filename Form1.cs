/*

    5_modem signal analysis software.    2016, Version 0.1

        This C# software is developed for testing 5_modem_board voltage signals(include: 
     Vbus, 5V and 3.3V power, 7_port_USB chip enable_signals, and modem enable_signals). 
     To test those signals, connect signals pins to the AD convert terminals (channel 0 to 7) and 
     press 'start' button, the siganls will show up on the strip chart. 
     To save the measured voltage data to SQL server database, press 'Save to SQL' button, 
     or press 'Save to text' button to save as text file on local PC.
     From the strip chart, you can compare those siganl's waveform to analysis the timing issues of 
     the 5_modem boards and debug the potential failure causes .  

*/ 



using System;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.ComponentModel;

namespace AnalogIn_toStripchart_SW_Timed
{
    public partial class Form1 : Form
    {
        //testing hardware head files
        public MccDaq.MccBoard DaqBoard;
        public MccDaq.ErrorInfo ULStat;
        public MccDaq.Range Range;
        public System.Int32 numchannels = 0;
        public BERGtools.LED LE = new BERGtools.LED();
        //public BERGtools.StripChart Form1.strip2();

        public Form1()
        {
            InitializeComponent();
        }

        
        private void Form1_Load(object sender, EventArgs e)
        {

            //create file for write and read;
            FileStream F = new FileStream("test.txt", FileMode.Create, FileAccess.Write, FileShare.Read);
            // F.Write 


            //First Lets make sure there's a USB-1408FS headware plugged in,
            System.Int16 BoardNum;
            System.Boolean Boardfound = false;
            for (BoardNum = 0; BoardNum < 99; BoardNum++)
            {

                DaqBoard = new MccDaq.MccBoard(BoardNum);
                if (DaqBoard.BoardName.Contains("1408FS"))
                {
                    Boardfound = true;
                    DaqBoard.FlashLED();
                    break;
                }
            }

            if (Boardfound == false)
            {
                System.Windows.Forms.MessageBox.Show("No USB-1408FS found in system.  Please run InstaCal.", "No Board detected");
                this.Close();
            }
            else
            {

                System.String mystring = DaqBoard.BoardName.Substring(0, DaqBoard.BoardName.Trim().Length - 1) +
                " found as board number: " + BoardNum.ToString();
                this.Text = mystring;

                //Initialize objects on the form needing attention
                LoadComboBox(cmboAInRange);

                //Determine if the device is set for single ended or differential by the number of channels.
                //use the value returned to set the NumericUpDown Control
                DaqBoard.BoardConfig.GetNumAdChans(out numchannels);
                nudAInChannel.Maximum = numchannels - 1;

                //set up sample timing
                for (int i = 1; i < 11; i++)
                    cbRate.Items.Add(i);
                cbRate.SelectedIndex = 9;
            }

        }
        
        //setting the testing voltage range
        public void LoadComboBox(ComboBox sender)  {
            sender.Items.Add("BIP20VOLTS");
            sender.Items.Add("BIP10VOLTS");
            sender.Items.Add("BIP5VOLTS");
            sender.Items.Add("BIP4VOLTS");
            sender.Items.Add("BIP2PT5VOLTS");
            sender.Items.Add("BIP2VOLTS");
            sender.Items.Add("BIP1PT25VOLTS");
            sender.Items.Add("BIP1VOLTS");
            sender.SelectedIndex = 1;
        }

        public void SelectRange(Int32 ComboBoxIndex)
        {
            switch (ComboBoxIndex)
            {
                case 0:
                    Range = MccDaq.Range.Bip20Volts;
                    break;
                case 1:
                    Range = MccDaq.Range.Bip10Volts;
                    break;
                case 2:
                    Range = MccDaq.Range.Bip5Volts;
                    break;
                case 3:
                    Range = MccDaq.Range.Bip4Volts;
                    break;
                case 4:
                    Range = MccDaq.Range.Bip2Pt5Volts;
                    break;
                case 5:
                    Range = MccDaq.Range.Bip2Volts;
                    break;
                case 6:
                    Range = MccDaq.Range.Bip1Pt25Volts;
                    break;
                case 7:
                    Range = MccDaq.Range.Bip1Volts;
                    break;
            }
        }

        //error handle
        public void errhandler(MccDaq.ErrorInfo ULStat)
        {
            //Generic UL error handler
            tmrAnalogIn.Enabled = false;
            lblAIMode.Text = "Idle";
            btnStartStop.Text = "Start";
            System.Windows.Forms.MessageBox.Show(ULStat.Message, "Universal Library Error ");
        }


        //starting the testing
        private void btnStartStop_Click(object sender, EventArgs e)
        {
            if (btnStartStop.Text == "Start")
            {
                btnStartStop.Text = "Stop";
                lblAIMode.Text = "Running";
                btnStartStop.Refresh();
                tmrAnalogIn.Interval = 1000 / Convert.ToInt32(cbRate.SelectedItem); // setting the time interval
                stripChart1.TimeInterval = tmrAnalogIn.Interval;
                stripChart1.Reset();                         //reset/clear the stripchart
                tmrAnalogIn.Enabled = true;
                list.Clear();


                //show the start time Tick

                //show time_tick
                L1 = times.UtcNowTicks;

               // label9.Text = L.ToString(); //show the tick of system
                //DateTime date = default(DateTime);
                DateTime date = new DateTime(); // default(DateTime);
                date = DateTime.Now;
                //timne stamp setting
                date.Millisecond.ToString();
                
                string Tms; //time ms
                Tms = date.Millisecond.ToString(); // / date.ToString();
              //  label9.Text = Tms;
                
            }
            else
            {
                btnStartStop.Text = "Start";
                lblAIMode.Text = "Idle";
                tmrAnalogIn.Enabled = false;

            }
        }

        private void Button6_Click(object sender, EventArgs e) //close testing process
        {
            this.Close();
        }

        /// <summary>
        /// timestample class
        /// </summary>
        public class HiResDateTime
        {
            private static long lastTimeStamp = DateTime.UtcNow.Ticks;
            // public static long UtcNowTicks
            public long UtcNowTicks
            {
                get
                {
                    long orig, newval;
                    do
                    {
                        orig = lastTimeStamp;
                        long now = DateTime.UtcNow.Ticks;
                        newval = Math.Max(now, orig + 1);
                    } while (Interlocked.CompareExchange
                                 (ref lastTimeStamp, newval, orig) != orig);

                    return newval;
                }
            }
        }

        long L = 0; //time_tick
        long L1 = 0;
        HiResDateTime times = new HiResDateTime();
                
        //tested voltage data array list
        System.Collections.ArrayList list = new System.Collections.ArrayList();  //double array list
        System.Collections.ArrayList list_String = new System.Collections.ArrayList();  //string array list
        double k = 0.0;
        string k_string = "";

        //system time tick, every tick create one voltage data
        private void tmrAnalogIn_Tick(object sender, EventArgs e)
        {

            //show relative time_tick
            L = times.UtcNowTicks;
            L = L - L1;
            label11.Text = L.ToString(); //show the tick of system

            //show current time
            DateTime date = new DateTime(); // default(DateTime);
            date = DateTime.Now;

            date.Millisecond.ToString();
            string s1;
            s1 = date.ToString();// date.Millisecond.ToString();   // date.ToString();
            label8.Text = s1;
                      
            //here is how to implement the VIn() Method.
            System.Single VInVolts; System.Single VInVolts2;

            SelectRange(cmboAInRange.SelectedIndex);
            
            //error handle
            // MccDaq.ErrorInfo.ErrorCode.Unavailable
            ULStat = DaqBoard.VIn(Convert.ToInt16(nudAInChannel.Value), Range, out VInVolts, MccDaq.VInOptions.Default);
            if (ULStat.Value != MccDaq.ErrorInfo.ErrorCode.NoErrors)
            {
                errhandler(ULStat);
                return;
            }

            ULStat = DaqBoard.VIn(Convert.ToInt16(nudAInChannel.Value + 1), Range, out VInVolts2, MccDaq.VInOptions.Default);
            if (ULStat.Value != MccDaq.ErrorInfo.ErrorCode.NoErrors)
            {
                errhandler(ULStat);
                return;
            }


            txtADValue.Text = VInVolts.ToString("#0.###");
            //texADValue2.Text = VInVolts2.ToString("#0.###");
            stripChart1.AddValue(VInVolts);
            //stripChart1.AddValue(VInVolts2 + 3);
            
            k = VInVolts;
            k_string = VInVolts.ToString();
            list.Add(k); //this list is double list
            list_String.Add(k_string);// this array list is string list

            //array list count
            int vv = list.Count;
            label6.Text = vv.ToString();            
        }

        private void stripChart1_Load(object sender, EventArgs e)
        {

        }

      //save measured data into SQL server database
        private void butt_save(object sender, EventArgs e)
        {
           

           //SQL server table name: Table_Jun2
            string insert_ = "INSERT INTO Table_Jun2 ( time, voltage) VALUES (  'ffLname',   @list )";

           
            String str = "Data Source=MININT-KRDE270;Initial Catalog=JunPeng-Oct15;Integrated Security=True";

            SqlConnection conn = new SqlConnection(str);

            SqlCommand cmd2 = new SqlCommand(insert_, conn);
            cmd2.Parameters.Add("@list", System.Data.SqlDbType.Float);
            // set values to parameters from textboxes
            
            conn.Open();
           
           // measured voltage data array list[1] = 3.2;


            for (int i = 0; i < list.Count; i++)
            {
                cmd2.Parameters["@list"].Value = Convert.ToDouble(list[i]);

                cmd2.ExecuteNonQuery();
            }

            conn.Close();            
        }           
        
        private void button2_Click(object sender, EventArgs e)
        {
            /*        
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                label14.Text = folderBrowserDialog1.SelectedPath;
            }
        
            FileStream F = new FileStream("C:\\Users\\manufacturing-dell1\\Desktop\\diloger_for Winform\\test1.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            

            string path = folderBrowserDialog1.SelectedPath.ToString();
            //byte[] b = new byte[] { (byte)("ajkdalf ") };
            //string s = Encoding.UTF8.GetString(b);

           // string s = "jdsakfajkfjak jakjfdk";
            string string2 = "The voltage Data:  ";

            foreach (string var in list_String)
            {
              string2 = string2 + "   " + var.ToString();
            }
            //textBox1.Text = temporary;

            //File.WriteAllText("myFile.txt", s);

            string ss = "C:\\Users\\manufacturing-dell1\\Desktop\\diloger_for Winform\\test88.txt";

            File.WriteAllText((label14.Text + "\\tt.txt"), string2);
           
            F.Close();
           */

        }       

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {

        }

        //save measured data into Text file in local PC
        private void button3_Click(object sender, EventArgs e)
        {
            string string2 = "The voltage Data:  ";

            foreach (string var in list_String)
            {
                string2 = string2 + "   " + var.ToString();
            }
                  
           
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;
            
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                     string str2 = saveFileDialog1.FileName.ToString();
                   
                     File.WriteAllText(str2, string2);
                                  
            }
           
        }       
    }
}

    

