using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.IO;

namespace MessagePassingTesting
{
    class Program
    {
        const float SERVER_WAIT_TO_CONNECT = 15;

        static void WriteBoolGrid(string filename, bool[,] data, int x, int y)
        {
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            StreamWriter oFile = new StreamWriter(filename);

            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < y; j++)
                {
                    oFile.Write((data[i,j] ? 1 : 0) + " "); // notepad can't view this file correctly so diplay it in VSCode
                }
                oFile.WriteLine();
            }

            oFile.Close();
        }



        static void ServerMethod(IPAddress ip)
        {
            // Start server
            Console.WriteLine("SERVER: Opening server . . .");
            TcpListener server = new TcpListener(ip, 8001);
            server.Start();
            Console.WriteLine("SERVER: Server opened");

            // Wait for connection request from client
            Stopwatch watch = new Stopwatch();
            watch.Start();
            Console.WriteLine("SERVER: Waiting for connection . . . ");
            while (watch.Elapsed.TotalSeconds < SERVER_WAIT_TO_CONNECT && !server.Pending()) { Console.Write(". "); Thread.Sleep(1000); };
            Console.WriteLine("\n");
            if (!server.Pending())
            {
                Console.WriteLine("SERVER: Timed out, no connection request detected");
                return;
            }
            
            // Accept socket
            Socket socket = server.AcceptSocket();
            if (!socket.Connected)
            {
                Console.WriteLine("SERVER: Failed to accept socket connection");
                return;
            }

            // Generate random Data
            Random rand = new Random();
            bool[,] data = new bool[1080, 1920];
            byte[] bData = new byte[1080*1920/8];

            for (int i = 0; i < 1080; i++)
            {
                for (int j = 0; j < 1920; j+=8)
                {
                    byte packedData = 0x00;
                    for (int k = 0; k < 8; k++)
                    {
                        data[i, j+k] = rand.Next(0, 2) == 1;
                        packedData += (byte)(data[i, j + k] ? 0x01 << (7-k) : 0x00);
                    }
                    
                    // Pack boolean data into 8 bools per byte
                    bData[(i * 1920/8) + (j/8)] = packedData;
                }
            }

            // Attempt to send the data
            try
            {
                socket.Send(bData);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SOCKET: error sending byte data : " + e.ToString());
                return;
            }

            // Print data to output file
            WriteBoolGrid("serverOutput.txt", data, 1080, 1920);
        }



        static void ClientMethod(IPAddress ip)
        {
            // Open client and attempt to connect
            TcpClient client = new TcpClient();
            NetworkStream stream;
            client.Connect(ip, 8001);
            if (client.Connected)
            {
                Console.WriteLine("CLIENT: Client Connected");
                stream = client.GetStream();
            }
            else
            {
                return;
            }

            // Receive data from server
            Byte[] bData = new byte[1080*1920/8];
            try
            {
                stream.Read(bData);
            }
            catch (SocketException e)
            {
                Console.WriteLine("CLIENT: error receiving byte data : " + e.ToString());
                return;
            }

            int dataWidth = 1920;
            // Convert data to bool and print to text file
            bool[,] data = new bool[1080, 1920];
            for (int i = 0; i < 1080; i++)
            {
                for (int j = 0; j < dataWidth; j += 8)
                {
                    // unpack byte into 8 booleans
                    Byte boolSet = bData[(i*dataWidth + j)/8];
                    data[i, j] = (boolSet & 0x80) == 0x80;
                    data[i, j+1] = (boolSet & 0x40) == 0x40;
                    data[i, j+2] = (boolSet & 0x20) == 0x20;
                    data[i, j+3] = (boolSet & 0x10) == 0x10;
                    data[i, j+4] = (boolSet & 0x08) == 0x08;
                    data[i, j+5] = (boolSet & 0x04) == 0x04;
                    data[i, j+6] = (boolSet & 0x02) == 0x02;
                    data[i, j+7] = (boolSet & 0x01) == 0x01;
                }
            }

            WriteBoolGrid("clientOutput.txt", data, 1080, 1920);
        }



        static void Main(string[] args)
        {
            // Get local IP Address for TCP connection
            IPAddress localIP = Dns.GetHostEntry("localhost").AddressList[1];
            Console.WriteLine("Localhost IP Adderss: {0}", localIP);

            // Starting threads
            Thread sThread = new Thread(() => ServerMethod(localIP));
            //Thread cThread = new Thread(() => ClientMethod(localIP));
            Console.WriteLine("Starting Threads:\n");
            sThread.Start();
            //cThread.Start();

            // Start java thread
            ProcessStartInfo javaProgram = new ProcessStartInfo("java.exe", "-jar JavaMessagePassingTesting.jar localhost 8001")
            {
                CreateNoWindow = false,
                UseShellExecute = false
            };

            Process jProc = Process.Start(javaProgram);

            sThread.Join();
            //cThread.Join();
            if (jProc == null)
            {
                Console.WriteLine("ERROR: could not start java program");
                return;
            }
            jProc.WaitForExit();
            jProc.Close();

            Console.WriteLine("\n\nSockets complete");

            try
            {
                StreamReader sFile = new StreamReader("serverOutput.txt");
                StreamReader cFile = new StreamReader("javaClientOutput.txt");
                //StreamReader cFile = new StreamReader("clientOutput.txt");

                bool same = true;

                for (int i = 0; i < 1080 && same; i++)
                {
                    same = cFile.ReadLine().Equals(sFile.ReadLine());
                    if (!same)
                    {
                        Console.WriteLine("Line {0} is different", i);
                    }
                }

                cFile.Close();
                sFile.Close();

                if (same)
                {
                    Console.WriteLine("The files are the same!");
                }
                else
                {
                    Console.WriteLine("The files are different :(");
                }
            }
            catch
            {
                Console.WriteLine("");
            }
        }
    }
}
