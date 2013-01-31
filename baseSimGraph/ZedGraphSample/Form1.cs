using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ZedGraph;
using System.IO;
using System.Collections;

namespace ZedGraphSample
{
	public partial class Form1 : Form
	{
        public struct packetEntry
        {
            public string id;
            public double time;
            public string protocol;
            public int clientID;
            public string clientRole;
        }
        public struct client
        {
            public int clientID;
            public string clientRole;
            public double lastAuth;
            public double totalInternetUsageTime;
            public double totalInternetUsageDelay;
        }       
        client[] clientArray = new client[300];
        public string folderName = "retake/300/";
        int userCount = 0;       
        double totalDelay = 0;
		public Form1()
		{
			InitializeComponent();
		}
        bool isValid(double time, double delay)
        {
            return true;
            if (delay >= 1 && time >= 00)
                return false;
            else
                return true;
        }

        public List<packetEntry> MergeSort(List<packetEntry> arrIntegers)
        {
            if (arrIntegers.Count == 1)
            {
                return arrIntegers;
            }
            List<packetEntry> arrSortedInt = new List<packetEntry>();
            int middle = (int)arrIntegers.Count / 2;
            List<packetEntry> leftArray = arrIntegers.GetRange(0, middle);
            List<packetEntry> rightArray = arrIntegers.GetRange(middle, arrIntegers.Count - middle);
            leftArray = MergeSort(leftArray);
            rightArray = MergeSort(rightArray);
            int leftptr = 0;
            int rightptr = 0;
            for (int i = 0; i < leftArray.Count + rightArray.Count; i++)
            {
                if (leftptr == leftArray.Count)
                {
                    arrSortedInt.Add(rightArray[rightptr]);
                    rightptr++;
                }
                else if (rightptr == rightArray.Count)
                {
                    arrSortedInt.Add(leftArray[leftptr]);
                    leftptr++;
                }
                else if (leftArray[leftptr].time < rightArray[rightptr].time)
                {
                    //need the cast above since arraylist returns Type object
                    arrSortedInt.Add(leftArray[leftptr]);
                    leftptr++;
                }
                else
                {
                    arrSortedInt.Add(rightArray[rightptr]);
                    rightptr++;
                }
            }
            return arrSortedInt;
        }

		private void Form1_Load( object sender, EventArgs e )
		{

		}

        private void CreateGraph(ZedGraphControl zgc)
		{
			GraphPane myPane = zgc.GraphPane;
            button2.Visible = false;
			// Set the titles and axis labels
			myPane.Title.Text = "";
			myPane.XAxis.Title.Text = "Time (m?nutes)";
			myPane.YAxis.Title.Text = "Average Delay (seconds)";

            List<packetEntry> srcList = new List<packetEntry>();
            string[] fileArray = Directory.GetFiles(folderName+"1srcWifi/");
            for (int i = 0; i < fileArray.Length; i++)
            {
                if (fileArray[i].Contains("txt"))
                {
                    StreamReader sr = new StreamReader(fileArray[i]);

                    client c = new client();
                    c.totalInternetUsageTime = 0;
                    c.totalInternetUsageDelay = 0;
                    c.lastAuth = -1;
                    c.clientRole = "";
                    string lastline;
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (line != "")
                        {
                            lastline = line;
                            string[] srcLine = line.Split('$');
                            string packetId = srcLine[4];
                            double packetTime = Convert.ToDouble(srcLine[5].Replace('.', ','));
                            string networkProtocol = srcLine[6];

                            if (c.clientRole == "")
                            {
                                c.clientRole = srcLine[7];
                                c.clientID = Convert.ToInt32(srcLine[8]);
                            }
                            if ((networkProtocol == "InitialAuth" || networkProtocol == "Reuse") && c.lastAuth == -1)
                            {
                                c.lastAuth = packetTime;
                            }
                            else if (networkProtocol == "Disconnection")
                            {
                                if(packetTime > c.lastAuth)
                                {
                                    double diff = packetTime - c.lastAuth;
                                    c.totalInternetUsageTime += diff;
                                    c.lastAuth = -1;
                                }
                            }
                            
                            packetEntry pe = new packetEntry();
                            pe.id = packetId;
                            pe.time = packetTime;
                            pe.protocol = networkProtocol;
                            pe.clientRole = srcLine[7];
                            pe.clientID = Convert.ToInt32(srcLine[8]);
                            srcList.Add(pe);                            
                        }
                    }
                    sr.Close();
                    if (c.lastAuth != -1)
                    {
                        c.totalInternetUsageTime += 1440 - c.lastAuth;
                    }
                    clientArray[c.clientID] = c;
                }
            }

            srcList = MergeSort(srcList);
            //srcList.RemoveRange(0, userCount);
            StreamWriter destWrite = new StreamWriter("destTotal.txt");
            string[] fileArray2 = Directory.GetFiles(folderName + "5destMesh/");
            for (int i = 0; i < fileArray2.Length; i++)
            {
                if (fileArray2[i].Contains("txt"))
                {
                    StreamReader sr = new StreamReader(fileArray2[i]);
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (line != "")
                        {
                            destWrite.WriteLine(line);
                        }
                    }
                    sr.Close();
                }
            }
            destWrite.Close();
            /*
            StreamWriter destWrite2 = new StreamWriter("destTotalPT.txt");
            string[] fileArray3 = Directory.GetFiles(folderName + "destPT/");
            for (int i = 0; i < fileArray3.Length; i++)
            {
                if (fileArray3[i].Contains("txt"))
                {
                    StreamReader sr = new StreamReader(fileArray3[i]);
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (line != "")
                        {
                            destWrite2.WriteLine(line);
                        }
                    }
                    sr.Close();
                }
            }
            destWrite2.Close();
            */
            PointPairList list = new PointPairList();
            
            int count = 1;
                       
            for(int i = 0; i < srcList.Count; i++)
            {                
                string packetId = srcList[i].id;
                double sendTime = srcList[i].time;
                string networkProtocol = srcList[i].protocol;
                int clientID = srcList[i].clientID;
                if (networkProtocol == "InitialAuth" || networkProtocol == "Reuse")//networkProtocol == "PacketTransfer" || networkProtocol == "Reuse" || networkProtocol == "ChangeAlias" || networkProtocol == "Disconnection" || networkProtocol == "End-To-End")
                {
                    StreamReader dest = new StreamReader("destTotal.txt");
                    while (!dest.EndOfStream)
                    {
                        string[] destLine = dest.ReadLine().Split('$');
                        if (packetId == destLine[4])
                        {
                            double arriveTime = Convert.ToDouble(destLine[5].Replace('.', ','));
                            if (arriveTime - sendTime > 3) break;
                            clientArray[clientID].totalInternetUsageDelay += arriveTime - sendTime;
                            totalDelay = totalDelay + (arriveTime - sendTime);
                            double average = totalDelay / count;

                            list.Add(sendTime, average);

                            count++;

                            break;
                        }
                    }
                    dest.Close();
                }
                /*else if (networkProtocol == "SeamlessMobility" || networkProtocol == "Roaming" || networkProtocol == "PacketTransfer")
                {
                    StreamReader dest = new StreamReader("destTotalPT.txt");
                    while (!dest.EndOfStream)
                    {
                        string[] destLine = dest.ReadLine().Split('$');
                        if (packetId == destLine[4])
                        {
                            double arriveTime = Convert.ToDouble(destLine[5].Replace('.', ','));
                            if (arriveTime - sendTime > 3) break;
                            clientArray[clientID].totalInternetUsageDelay += arriveTime - sendTime;
                            totalDelay = totalDelay + (arriveTime - sendTime);
                            double average = totalDelay / count;

                            list.Add(sendTime, average);

                            count++;

                            break;
                        }
                    }
                    dest.Close();
                }*/
            }

            StreamWriter sw = new StreamWriter("clientArray.txt");
            for (int i = 0; i < clientArray.Length; i++)
            {
                client c = clientArray[i];
                sw.WriteLine(c.clientID.ToString() + " " + c.clientRole + " " + c.totalInternetUsageTime.ToString() + " " + c.totalInternetUsageDelay.ToString());
            }
            sw.Close();

			// Generate a blue curve with circle symbols, and "My Curve 2" in the legend
			LineItem myCurve = myPane.AddCurve( "Average Delay Curve", list, Color.Blue,
									SymbolType.None );
			// Fill the area under the curve with a white-red gradient at 45 degrees
			//myCurve.Line.Fill = new Fill( Color.White, Color.Red, 45F );
			// Make the symbols opaque by filling them with white
			myCurve.Symbol.Fill = new Fill( Color.White );

			// Fill the axis background with a color gradient
			myPane.Chart.Fill = new Fill( Color.White, Color.LightGoldenrodYellow, 45F );

			// Fill the pane background with a color gradient
			myPane.Fill = new Fill( Color.White, Color.FromArgb( 220, 220, 255 ), 45F );

			// Calculate the Axis Scale Ranges
			zgc.AxisChange();
		}

		private void Form1_Resize( object sender, EventArgs e )
		{
			SetSize();
		}

		private void SetSize()
		{
			zg1.Location = new Point( 10, 10 );
			// Leave a small margin around the outside of the control
			zg1.Size = new Size( this.ClientRectangle.Width - 20, this.ClientRectangle.Height - 20 );
		}

        private void button1_Click(object sender, EventArgs e)
        {
            CreateGraph(zg1);
            SetSize();
            //label1.Visible = false; label2.Visible = false; label3.Visible = false;
            //textBox1.Visible = false; textBox2.Visible = false; textBox3.Visible = false;
            button1.Visible = false;
            comboBox1.Visible = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            double studentTime = 0;
            double studentDelay = 0;
            double workerTime = 0;
            double workerDelay = 0;
            double nonworkerTime = 0;
            double nonworkerDelay = 0;

            int studentCount = 0;
            int workerCount = 0;
            int nonworkerCount = 0;

            StreamReader sr = new StreamReader("clientArray.txt");
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                string[] lineArray = line.Split(' ');
                double tmpTime = Convert.ToDouble(lineArray[2]);
                double tmpDelay = Convert.ToDouble(lineArray[3]);
                if (lineArray[1] == "student")
                {
                    studentCount++;
                    studentTime += tmpTime;
                    studentDelay += tmpDelay;
                }
                else if (lineArray[1] == "worker")
                {
                    workerCount++;
                    workerTime += tmpTime;
                    workerDelay += tmpDelay;
                }
                else
                {
                    nonworkerCount++;
                    nonworkerTime += tmpTime;
                    nonworkerDelay += tmpDelay;
                }
            }
            sr.Close();

            StreamWriter sw = new StreamWriter("finalResults.txt");
            sw.WriteLine("total student internet usage time: " + studentTime.ToString());
            sw.WriteLine("total worker internet usage time: " + workerTime.ToString());
            sw.WriteLine("total nonworker internet usage time: " + nonworkerTime.ToString());

            sw.WriteLine("");

            sw.WriteLine("total student delay: " + studentDelay.ToString());
            sw.WriteLine("total worker delay: " + workerDelay.ToString());
            sw.WriteLine("total nonworker delay: " + nonworkerDelay.ToString());

            sw.WriteLine("");

            double stuAvgTime = studentTime / studentCount;
            double workerAvgTime = workerTime / workerCount;
            double nonworkerAvgTime = nonworkerTime / nonworkerCount;
            sw.WriteLine("average student internet usage: " + stuAvgTime.ToString());
            sw.WriteLine("average worker internet usage: " + workerAvgTime.ToString());
            sw.WriteLine("average nonworker internet usage: " + nonworkerAvgTime.ToString());

            sw.WriteLine("");

            double stuAvgDelay = studentDelay / studentCount;
            double workerAvgDelay = workerDelay / workerCount;
            double nonworkerAvgDelay = nonworkerDelay / nonworkerCount;
            sw.WriteLine("average student delay: " + stuAvgDelay.ToString());
            sw.WriteLine("average worker delay: " + workerAvgDelay.ToString());
            sw.WriteLine("average nonworker delay: " + nonworkerAvgDelay.ToString());

            sw.Close();

            MessageBox.Show("Done");
        }
	}
}