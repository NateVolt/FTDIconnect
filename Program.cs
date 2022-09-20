// adapted from FTDI Loopback example by Brandin Claar <brandin@remodulate.com>

using System;
using System.Threading;

using FTD2XX_NET;

using lpcprog;
using System.Configuration;

namespace LoopBack
{
    class Program
    {
        static void attention()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.BackgroundColor = ConsoleColor.Black;
        }

        static void prompt()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.BackgroundColor = ConsoleColor.Black;
        }

        static void subdue()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.BackgroundColor = ConsoleColor.Black;
        }

        static void program()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.BackgroundColor = ConsoleColor.DarkBlue;
        }

        static void Main(string[] args)
        {
            program();
            Console.WriteLine("This program contains a software component made available under the GNU Lesser General Public License.  The source code of this component is available here:");
            Console.WriteLine("https://github.com/brand7n/lpc21isp");

            // Create new instance of the FTDI device class
            FTDI myFtdiDevice = new FTDI();

            attention();
            Console.WriteLine("ATTEMPTING TO CONNECT...");
            subdue();


            while (true)
            {
                try
                {
                    open(myFtdiDevice);
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    attention();
                    Console.WriteLine("DISCONNECTED.  RESTART OR REATTACH DEVICE.");
                    subdue();
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    myFtdiDevice.CyclePort();
                }
            }
        }

        static int myStrDelegate(int n, String m)
        {
            Console.Write(m);
            return 0;
        }

        static void open(FTDI myFtdiDevice)
        {
            UInt32 ftdiDeviceCount = 0;
            FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;

            // Determine the number of FTDI devices connected to the machine
            ftStatus = myFtdiDevice.GetNumberOfDevices(ref ftdiDeviceCount);
            // Check status
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                return;
            }


            // If no devices available, return
            if (ftdiDeviceCount == 0) return;

            Console.WriteLine("Number of FTDI devices: " + ftdiDeviceCount.ToString());


            // Allocate storage for device info list
            FTDI.FT_DEVICE_INFO_NODE[] ftdiDeviceList = new FTDI.FT_DEVICE_INFO_NODE[ftdiDeviceCount];

            // Populate our device list
            ftStatus = myFtdiDevice.GetDeviceList(ftdiDeviceList);

            int oIdx=-1;
            if (ftStatus == FTDI.FT_STATUS.FT_OK)
            {
                for (Int32 i = 0; i < ftdiDeviceCount; i++)
                {
                    if (ftdiDeviceList[i].Type == FTDI.FT_DEVICE.FT_DEVICE_X_SERIES)
                    {
                        oIdx = i;
                    }
                }

                if (oIdx < 0)
                {
                    Console.WriteLine("no matching device");
                    return;
                }
            }

            Console.WriteLine("Device Index: " + oIdx.ToString());
            Console.WriteLine("Flags: " + String.Format("{0:x}", ftdiDeviceList[oIdx].Flags));
            Console.WriteLine("Type: " + ftdiDeviceList[oIdx].Type.ToString());
            Console.WriteLine("ID: " + String.Format("{0:x}", ftdiDeviceList[oIdx].ID));
            Console.WriteLine("Location ID: " + String.Format("{0:x}", ftdiDeviceList[oIdx].LocId));
            Console.WriteLine("Serial Number: " + ftdiDeviceList[oIdx].SerialNumber.ToString());
            Console.WriteLine("Description: " + ftdiDeviceList[oIdx].Description.ToString());

            myFtdiDevice.SetDTR(false);
            myFtdiDevice.SetRTS(false);

            // Open first device in our list by serial number
            ftStatus = myFtdiDevice.OpenBySerialNumber(ftdiDeviceList[oIdx].SerialNumber);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Wait for a key press
                Console.WriteLine("Failed to open device (error " + ftStatus.ToString() + ")");
                //Console.ReadKey();
                return;
            }

            myFtdiDevice.SetDTR(false);
            myFtdiDevice.SetRTS(false);

            // Set up device data parameters
            uint baud = 115200;
            if (ConfigurationManager.AppSettings["BaudRate"] != null)
            {
                baud = UInt32.Parse(ConfigurationManager.AppSettings["BaudRate"]);
            }
            ftStatus = myFtdiDevice.SetBaudRate(baud);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Wait for a key press
                Console.WriteLine("Failed to set Baud rate (error " + ftStatus.ToString() + ")");
                //Console.ReadKey();
                return;
            }

            // Set data characteristics - Data bits, Stop bits, Parity
            ftStatus = myFtdiDevice.SetDataCharacteristics(FTDI.FT_DATA_BITS.FT_BITS_8, FTDI.FT_STOP_BITS.FT_STOP_BITS_1, FTDI.FT_PARITY.FT_PARITY_NONE);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Wait for a key press
                Console.WriteLine("Failed to set data characteristics (error " + ftStatus.ToString() + ")");
                //Console.ReadKey();
                return;
            }

            // Set flow control - set RTS/CTS flow control
            ftStatus = myFtdiDevice.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_NONE, 0x11, 0x13);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Wait for a key press
                Console.WriteLine("Failed to set flow control (error " + ftStatus.ToString() + ")");
                //Console.ReadKey();
                return;
            }

            // times out at 100ms
            ftStatus = myFtdiDevice.SetTimeouts(100, 100);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Wait for a key press
                Console.WriteLine("Failed to set timeouts (error " + ftStatus.ToString() + ")");
                //Console.ReadKey();
                return;
            }

            attention();
            Console.WriteLine("CONNECTED. PRESS ANY KEY TO START.");
            Console.WriteLine("CTRL-R: RESET");
            Console.WriteLine("CTRL-P: PROGRAM");
            Console.WriteLine("CTRL-T: SET TIME");
            prompt();

            myFtdiDevice.SetDTR(true);
            myFtdiDevice.SetRTS(true);

            while (true)
            {
                UInt32 numBytes = 0;
                byte[] buf = new byte[1024];

                while (Console.KeyAvailable)
                {
                    ConsoleKeyInfo cki = Console.ReadKey(true);
                    //Console.WriteLine("{0} (character '{1}')", cki.Key, cki.KeyChar);
                    if ((cki.Modifiers & ConsoleModifiers.Control) != 0)
                    {
                        if (cki.Key.ToString().ToUpper().EndsWith("R"))
                        {
                            attention();
                            Console.WriteLine("\nRESET!");
                            prompt();

                            myFtdiDevice.SetRTS(false);
                            myFtdiDevice.SetDTR(true);
                            Thread.Sleep(100);
                            myFtdiDevice.SetDTR(false);
                            continue;
                        }
                        if (cki.Key.ToString().ToUpper().EndsWith("P"))
                        {
                            attention();

                            Console.WriteLine("\nPROGRAM MODE:");
                            myFtdiDevice.Close();
                            program();

                            string firmwareName = ConfigurationManager.AppSettings["FirmwareName"];
                            LpcProgrammer p = new LpcProgrammer(new StringOutDelegate(myStrDelegate));
                            //p.Prepare();
                            int r = p.Program(firmwareName);

                            attention();
                            Console.WriteLine("\nRETURN VALUE: {0}", r);
                            subdue();

                            throw new Exception("Leaving program mode");
                        }
                        if (cki.Key.ToString().ToUpper().EndsWith("T"))
                        {
                            //  Time: 10:11:0
                            //  Date: 2827.5.9
                            string datePatt = @"yyyy.M.d";
                            string timePatt = @"HH:mm:ss";
                            DateTime dispDt = DateTime.Now;
                            string dtString = "set_date " + dispDt.ToString(datePatt) + "\r";
                            string tmString = "set_time " + dispDt.ToString(timePatt) + "\r";
                            //Console.WriteLine(dtString);
                            //Console.WriteLine(tmString);
                            System.Text.ASCIIEncoding ASCII  = new System.Text.ASCIIEncoding();
                            Byte[] cmdBytes = ASCII.GetBytes(dtString);
                            myFtdiDevice.Write(cmdBytes, cmdBytes.Length, ref numBytes);
                            Thread.Sleep(200);
                            cmdBytes = ASCII.GetBytes(tmString);
                            myFtdiDevice.Write(cmdBytes, cmdBytes.Length, ref numBytes);
                            continue;
                        }
                    }
                    buf[0] = (byte)(cki.KeyChar & 0xff);

                    if (cki.Key == ConsoleKey.Enter)
                        buf[0] = (byte)'\n';

                    numBytes = 0;
                    ftStatus = myFtdiDevice.Write(buf, 1, ref numBytes);
                    if (ftStatus != FTDI.FT_STATUS.FT_OK)
                    {
                        throw new Exception("Failed to write to device (error " + ftStatus.ToString() + ")");
                        //return;
                    }
                }

                numBytes = 0;
                ftStatus = myFtdiDevice.GetRxBytesAvailable(ref numBytes);
                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    throw new Exception("Failed to get number of bytes available to read (error " + ftStatus.ToString() + ")");
                    //return;
                }
                if (numBytes > 0)
                {
                    UInt32 numBytesRead = 0;
                    byte[] buffer = new byte[1024];
                    // Note that the Read method is overloaded, so can read string or byte array data
                    //ftStatus = myFtdiDevice.Read(out readData, 1024, ref numBytesRead);
                    ftStatus = myFtdiDevice.Read(buffer, 1024, ref numBytesRead);
                    if (ftStatus != FTDI.FT_STATUS.FT_OK)
                    {
                        // Wait for a key press
                        throw new Exception("Failed to read data (error " + ftStatus.ToString() + ")");
                        //Console.ReadKey();
                        //return;
                    }
                    string readData = System.Text.Encoding.ASCII.GetString(buffer, 0, (int)numBytesRead);
                    Console.Write(readData);
                }
                else
                {
                    Thread.Sleep(10);
                }

            }
        }

    }
}
