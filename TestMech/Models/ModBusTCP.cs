using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Arion.Data.Utilities;

namespace TestMech.Models
{
    //public enum Modbusbyte : byte{IDH=0,IDL=1,ProtocolH=2,ProtocolL=3,LenH=4,LenL=5,UI=6,FunctionCode=7}

    public class ModBusTcp : IDisposable
    {
        public enum LinkStatus { Disconnected = 0, Connected = 1, Connecting = 2 }

        #region MODBUS

        //frame modbus
        public const byte IdH = 0;

        public const byte IdL = 1;
        public const byte ProtocolH = 2;
        public const byte ProtocolL = 3;
        public const byte ByteNH = 4;
        public const byte ByteNL = 5;
        public const byte UI = 6;
        public const byte FunctionCode = 7;
        public const byte AddrH = 8;
        public const byte AddrL = 9;
        public const byte BitsH = 10;
        public const byte BitsL = 11;
        public const byte NumberOfBytes = 12;

        // supported function codes
        public const ushort fDiagnosticsReturnQueryData = 0;

        public const byte fReadCoils = 1;
        public const byte fReadInputs = 2;
        public const byte fReadHoldingRegisters = 3;
        public const byte fReadInputRegisters = 4;
        public const byte fWriteSingleCoil = 5;
        public const byte fWriteSingleRegister = 6;
        public const byte fDiagnostics = 8;
        public const byte fWriteMultipleCoils = 15;
        public const byte fWriteMultipleRegisters = 16;
        public const byte fReadWriteMultipleRegisters = 23;

        public const ushort CoilOn = 0xFF00;
        public const ushort CoilOff = 0x0000;

        public const byte ERR = 0x80;

        private string[] ErrorCodes = new string[7] { "0-NoError", "Illegal function", "Illegal data address", "illegal data value", "Slave device failure", "5-unknown", "Slave device busy" };

        public const int MaximumDiscreteRequestResponseSize = 260;  //o 253
        public const int MaximumTelegramLenght = 260;       //Byte

        public const ushort MAXMSGID = 1000;

        private ushort mLastMsgId = 0;

        private byte mLstErrorFunctionCode = 0;
        private byte mLastErrorCode = 0;
        public byte[] mLastDataReceived = new byte[MaximumTelegramLenght];

        public object mSending = new object();      //lock per gestire una comunicazione e risposta alla volta

        #endregion MODBUS

        #region Member variables

        private TcpClient mSender;

        private NetworkStream mnetStream;
        public int mConnectToPort;             //porta verso la quale trasmettere nel caso di connessione ad un server (tipo CanConNet)
        public IPAddress mConnectToIP;            //e ip
        public bool isConnecting = false;

        private int mMaxWaitTime = 500;            //millisecondi attesa max
        //long mWaitingForReply;              //flag per stato di attesa risposta
        //string mWaitedReply;

        // bool connected = false;

        // const int MAXCOMMANDS = 500;

        public long mMustQuit = 0;
        //public Thread rxThread;

        public bool mEnableLog = true;

        public string ConnectionName = "";

        public ushort MaxInputRegisters = 100;
        public ushort MaxHoldingRegisters = 100;
        public ushort MaxWriteRegisters = 100;

        public delegate void ConnectionCallBack(IAsyncResult result);

        public ConnectionCallBack connection_callback = null;

        #endregion Member variables

        //
        /*
        static public void Main()
        {
        }
        */

        public ModBusTcp()      //costruttore generico
        {
            mLastMsgId = 0;
        }

        public ModBusTcp(IPAddress remoteip, int remoteport = 502, bool connect = true)
        {
            mLastMsgId = 0;
            if (connect)
                InitSenderSocket(remoteip, remoteport);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                mSender?.Close();
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool InitSenderSocket(IPAddress ip, int remoteport = 502, bool force = false)
        {
            if (isConnecting)
                return true;
            
            mMustQuit = 0;
            mConnectToIP = ip;
            mConnectToPort = remoteport;

            isConnecting = true;
            mSender?.Close();

            mSender = new TcpClient(AddressFamily.InterNetwork);  //mSendToIP, mSendToPort);

            mSender.SendTimeout = 100;
            mSender.NoDelay = true;

            mLastMsgId = 0;

            Addlog("Try to connect:" + mConnectToIP.ToString() + ":" + mConnectToPort.ToString());
            try
            {
                if (force)
                {
                    mSender.Connect(mConnectToIP, remoteport);
                    if (mSender.Connected)
                    {
                        mnetStream = mSender.GetStream();
                        Addlog(string.Format("Connected to {0}:{1}", mConnectToIP, mConnectToPort));
                        isConnecting = false;
                    }
                }
                else
                {
                    mSender.BeginConnect(mConnectToIP, remoteport, new AsyncCallback(requestCallback), mSender);
                }
            }
            catch (Exception e)
            {
                isConnecting = false;
                Addlog(e.Message);
                return false;
            }
            //Addlog(string.Format("Ready to active send to {0}:{1}", mSendToIP, mSendToPort));

            return true;
        }

        private void requestCallback(IAsyncResult result)
        {
            try
            {
                // Retrieve the socket from the state object.
                TcpClient client = (TcpClient)result.AsyncState;

                if (client.Connected)
                {
                    mnetStream = client.GetStream();

                    // Complete the connection.
                    client.EndConnect(result);

                    Addlog(string.Format("Connected to {0}:{1}", mConnectToIP.ToString(), mConnectToPort));
                }
                else
                {
                    Addlog(string.Format("Connection failed with {0}:{1}", mConnectToIP.ToString(), mConnectToPort));
                    client.EndConnect(result);
                }

                connection_callback?.Invoke(result);
            }
            catch (Exception e)
            {
                Addlog(e.ToString());
            }
            isConnecting = false;
        }

        public void Disconnect()
        {
            Interlocked.Exchange(ref mMustQuit, 1);
            Thread.Sleep(15);
            try
            {
                mnetStream?.Close();
                mSender?.Close();
            }
            catch (Exception e)
            {
                Addlog(e.ToString());
            }
        }

        /*
        public bool ForceConnection()
        {
            CloseConnection();
            InitSenderSocket(mConnectToIP,mConnectToPort);
            TimeSpan dt;
            DateTime StartTime = DateTime.Now;
            //bool timeout = false;

            if (mnetStream == null)
                return false;
            do
            {
                dt = DateTime.Now - StartTime;
                if (dt.TotalMilliseconds >= 3000)
                {
                    m_log.AddLog("Wait reply timeout!!");
                    break;
                }
                if (GetStatus() == (int)LinkStatus.Connected)
                    return true;
                Thread.Sleep(1);
            } while (mMustQuit == 0);

            if (GetStatus() == (int)LinkStatus.Connected)
                return true;

            return false;
        }
        */

        public void SetWaitTime(int t)
        {
            mMaxWaitTime = t;
        }

        public int GetStatus()
        {
            if (isConnecting)
            {
                return (int)LinkStatus.Connecting;
            }
            else if (mSender?.Client != null)
            {
                if ((mSender.Connected) && (mSender.GetStream().CanWrite))
                    return (int)LinkStatus.Connected;
            }
            else
            {
                return (int)LinkStatus.Disconnected;
            }

            return (int)LinkStatus.Disconnected;
        }

        public bool Send(byte[] data, int len = 12)
        {
            int i = 0;
            string st = "Send:";
            try
            {
                if (mnetStream?.CanWrite == true)
                {
                    /*
                    while ((mnetStream.CanRead) && (mnetStream.DataAvailable))
                    {
                        byte[] data_r = new byte[1000];
                        mnetStream.Read(data_r, 0, 1000);
                    }
                    */

                    mnetStream.Write(data, 0, len);
                    if (mEnableLog)
                    {
                        for (i = 0; i < len; i++)
                            st += data[i].ToString() + " ";

                        Addlog(st);
                    }
                    //Console.WriteLine(st);
                    //Console.WriteLine("---");
                    //n=mSender.Client.Send(data, len,SocketFlags.None);
                    return true;
                }
                else
                {
                    Addlog("Transmission not allowed");
                    return false;
                }
            }
            catch (Exception e)
            {
                Addlog(e.ToString());
            }
            return false;
        }

        private bool WaitReply(ushort msgid)
        {
            WaitLoopTime wl = new WaitLoopTime(5);
            TimeSpan dt;
            DateTime StartTime = DateTime.UtcNow;
            bool timeout = false;
            int byteread = 0;
            int totalbyte = 0;
            int i = 0;
            string st = "Rec:";

            byte bL = Convert.ToByte(msgid & 0xff), bH = Convert.ToByte((msgid & 0xff00) >> 8);

            if (mnetStream == null)
                return true;

            try
            {
                do
                {
                    if ((mnetStream.CanRead) && (mnetStream.DataAvailable))
                    {
                        byteread = mnetStream.Read(mLastDataReceived, totalbyte, MaximumDiscreteRequestResponseSize);

                        totalbyte += byteread;

                        if (mEnableLog)
                        {
                            for (i = 0; i < totalbyte; i++)
                                st += mLastDataReceived[i].ToString() + " ";

                            Addlog(st);
                        }

                        if ((totalbyte > 5) && (totalbyte >= 6 + (mLastDataReceived[ByteNH] << 8) + mLastDataReceived[ByteNL]))   //5th e 6th byte so quanti byte devo aspettarmi
                        {
                            if ((mLastDataReceived[IdH] != bH) || (mLastDataReceived[IdL] != bL))
                            {
                                Addlog(string.Format("Modbus Error! Invalid message ID {0}", msgid));
                                return false;
                            }

                            if (mLastDataReceived[FunctionCode] > ERR)
                            {
                                mLstErrorFunctionCode = Convert.ToByte(mLastDataReceived[FunctionCode] - ERR);
                                mLastErrorCode = mLastDataReceived[8];
                                if (mLastDataReceived[8] < ErrorCodes.Length)
                                    Addlog(string.Format("Modbus Error! function {0} err: {1}-{2}", mLstErrorFunctionCode, mLastErrorCode, ErrorCodes[mLastErrorCode]));
                                else
                                    Addlog(string.Format("Modbus Error! function {0} err: {1}", mLstErrorFunctionCode, mLastErrorCode));

                                return false;
                            }
                            break;      //ho ricevuto tutti i dati
                        }
                    }

                    dt = DateTime.UtcNow - StartTime;
                    if (dt.TotalMilliseconds >= mMaxWaitTime)
                    {
                        Addlog("Wait reply timeout!!");
                        //Console.WriteLine("Wait reply timeout!!");
                        timeout = true;
                        break;
                    }
                    wl.Inc();
                } while (mMustQuit == 0);
            }
            catch (Exception e)
            {
                Addlog("Modbus error in wait reply function");
                Addlog(e.Message);
                return false;
            }

            return !timeout;
        }

        /*
        public string Send(string msg,string waitedreply)
        {
            mWaitedReply = waitedreply;
            mWaitingForReply = 1;
            mSender.Send(Encoding.ASCII.GetBytes(msg), msg.Length, mSendToIP, mSendToPort);

            if (WaitReply())
                return "";

            return "";
        }

        public void SendTo(string msg, IPEndPoint ip_port)
        {
            //Addlog (string.Format("Send
            mSender.Send(Encoding.ASCII.GetBytes(msg), msg.Length,ip_port);
        }
        */

        public void Addlog(string st)
        {
            
        }

        /// <summary>
        /// Read serveral digital outputs, max 2000
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="howmany"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public bool ReadCoils(ushort addr, ushort howmany, byte[] values)
        {
            bool r = false;
            byte[] data = new byte[12];
            byte bL, bH;
            int nbyte = howmany / 8;
            if (howmany != (howmany / 8 * 8))
                nbyte++;
            lock (mSending)
            {
                bH = Convert.ToByte((mLastMsgId & 0xff00) >> 8);
                bL = Convert.ToByte(mLastMsgId & 0xff);
                data[IdH] = bH;
                data[IdL] = bL;
                data[ProtocolH] = 0;
                data[ProtocolL] = 0;    //0=Modbus
                data[ByteNH] = 0;
                data[ByteNL] = 6;       //quanti byte seguono
                data[UI] = 1;
                data[FunctionCode] = fReadCoils;
                bH = Convert.ToByte((addr & 0xff00) >> 8);
                bL = Convert.ToByte(addr & 0xff);
                data[AddrH] = bH;
                data[AddrL] = bL;
                bH = Convert.ToByte((howmany & 0xff00) >> 8);
                bL = Convert.ToByte(howmany & 0xff);
                data[BitsH] = bH;
                data[BitsL] = bL;

                if (Send(data))
                {
                    r = WaitReply(mLastMsgId);
                    if (r)
                    {
                        for (int i = 0; i < nbyte; i++)
                            values[i] = mLastDataReceived[9 + i];
                    }
                }
                mLastMsgId = (ushort)((mLastMsgId + 1) % MAXMSGID);
            }
            return r;
        }

        /// <summary>
        /// Read serveral digital inputs, max 2000
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="howmany"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public bool ReadDicreteInputs(ushort addr, ushort howmany, byte[] values)
        {
            bool r = false;
            byte[] data = new byte[12];
            byte bL, bH;
            int nbyte = howmany / 8;
            if (howmany != (howmany / 8 * 8))
                nbyte++;

            lock (mSending)
            {
                bH = Convert.ToByte((mLastMsgId & 0xff00) >> 8);
                bL = Convert.ToByte(mLastMsgId & 0xff);
                data[IdH] = bH;
                data[IdL] = bL;
                data[ProtocolH] = 0;
                data[ProtocolL] = 0;    //0=Modbus
                data[ByteNH] = 0;
                data[ByteNL] = 6;       //quanti byte seguono
                data[UI] = 1;
                data[FunctionCode] = fReadInputs;
                bH = Convert.ToByte((addr & 0xff00) >> 8);
                bL = Convert.ToByte(addr & 0xff);
                data[AddrH] = bH;
                data[AddrL] = bL;
                bH = Convert.ToByte((howmany & 0xff00) >> 8);
                bL = Convert.ToByte(howmany & 0xff);
                data[BitsH] = bH;
                data[BitsL] = bL;

                if (Send(data))
                {
                    r = WaitReply(mLastMsgId);
                    if (r)
                    {
                        for (int i = 0; i < nbyte; i++)
                            values[i] = mLastDataReceived[9 + i];
                    }
                }
                mLastMsgId = (ushort)((mLastMsgId + 1) % MAXMSGID);
            }
            return r;
        }

        /// <summary>
        /// Read several register in word oriented area
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="howmanyregisters"></param>
        /// <param name="returndata"></param>
        /// <returns></returns>
        public bool ReadHoldingRegisters(ushort addr, ushort howmanyregisters, byte[] returndata)
        {
            int i;
            bool r = false;
            byte[] data = new byte[12];
            byte bL, bH;

            int returndatapos = 0;
            ushort last_cycle_registercount = (ushort)(howmanyregisters % MaxHoldingRegisters);

            if (last_cycle_registercount == 0)
                last_cycle_registercount = MaxHoldingRegisters;

            int cycles = 1;
            cycles = (int)Math.Ceiling((double)howmanyregisters / (double)MaxHoldingRegisters);

            howmanyregisters = MaxHoldingRegisters;

            lock (mSending)
            {
                for (int k = 0; k < cycles; k++)
                {
                    if (k == (cycles - 1))
                        howmanyregisters = last_cycle_registercount;

                    bH = Convert.ToByte((mLastMsgId & 0xff00) >> 8);
                    bL = Convert.ToByte(mLastMsgId & 0xff);
                    data[IdH] = bH;
                    data[IdL] = bL;
                    data[ProtocolH] = 0;
                    data[ProtocolL] = 0;    //0=Modbus
                    data[ByteNH] = 0;
                    data[ByteNL] = 6;       //quanti byte seguono
                    data[UI] = 1;
                    data[FunctionCode] = fReadHoldingRegisters;
                    bH = Convert.ToByte((addr & 0xff00) >> 8);
                    bL = Convert.ToByte(addr & 0xff);
                    data[AddrH] = bH;
                    data[AddrL] = bL;
                    bH = Convert.ToByte((howmanyregisters & 0xff00) >> 8);
                    bL = Convert.ToByte(howmanyregisters & 0xff);
                    data[BitsH] = bH;
                    data[BitsL] = bL;

                    if (Send(data))
                    {
                        r = WaitReply(mLastMsgId);
                        if (r)
                        {
                            for (i = 0; i < howmanyregisters * 2; i++)
                            {
                                returndata[returndatapos] = mLastDataReceived[9 + i];       //dal 9 iniziano i dati utili gli altri sono l'header
                                returndatapos++;
                            }
                        }
                    }
                    addr += MaxHoldingRegisters;
                    mLastMsgId = (ushort)((mLastMsgId + 1) % MAXMSGID);
                }
            }
            return r;
        }

        /// <summary>
        /// Read several register in word oriented area, max 125 registers
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="howmanyregisters"></param>
        /// <param name="returndata"></param>
        /// <returns></returns>
        public bool ReadHoldingRegisters(ushort addr, ushort howmanyregisters, ushort[] returndata)
        {
            byte[] data = new byte[howmanyregisters * 2];

            bool r = ReadHoldingRegisters(addr, howmanyregisters, data);

            int k = 0;
            if (r)
            {
                for (int i = 0; i < data.Length; i += 2)
                {
                    returndata[k] = (ushort)((Convert.ToUInt16(data[i]) << 8) + data[i + 1]);
                    k++;
                }
            }
            return r;
        }

        /// <summary>
        /// Read several register in word oriented area
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="howmanyregisters"></param>
        /// <param name="returndata"></param>
        /// <param name="withheader"></param>
        /// <returns></returns>
        public bool ReadInputRegister(ushort addr, ushort howmanyregisters, byte[] returndata)
        {
            bool r = false;
            byte[] data = new byte[12];
            byte bL, bH;
            int i;

            int returndatapos = 0;
            ushort last_cycle_registercount = (ushort)(howmanyregisters % MaxInputRegisters);

            if (last_cycle_registercount == 0)
                last_cycle_registercount = MaxInputRegisters;

            int cycles = 1;
            cycles = (int)Math.Ceiling((double)howmanyregisters / (double)MaxInputRegisters);

            howmanyregisters = MaxInputRegisters;

            lock (mSending)
            {
                for (int k = 0; k < cycles; k++)
                {
                    if (k == (cycles - 1))
                        howmanyregisters = last_cycle_registercount;

                    bH = Convert.ToByte((mLastMsgId & 0xff00) >> 8);
                    bL = Convert.ToByte(mLastMsgId & 0xff);
                    data[IdH] = bH;
                    data[IdL] = bL;
                    data[ProtocolH] = 0;
                    data[ProtocolL] = 0;    //0=Modbus
                    data[ByteNH] = 0;
                    data[ByteNL] = 6;       //quanti byte seguono
                    data[UI] = 1;
                    data[FunctionCode] = fReadInputRegisters;
                    bH = Convert.ToByte((addr & 0xff00) >> 8);
                    bL = Convert.ToByte(addr & 0xff);
                    data[AddrH] = bH;
                    data[AddrL] = bL;
                    bH = Convert.ToByte((howmanyregisters & 0xff00) >> 8);
                    bL = Convert.ToByte(howmanyregisters & 0xff);
                    data[BitsH] = bH;
                    data[BitsL] = bL;

                    if (Send(data))
                    {
                        r = WaitReply(mLastMsgId);
                        if (r)
                        {
                            for (i = 0; i < howmanyregisters * 2; i++)
                            {
                                returndata[returndatapos] = mLastDataReceived[9 + i];
                                returndatapos++;
                            }
                        }
                    }
                    //else
                    //    break;
                    addr += MaxInputRegisters;
                    mLastMsgId = (ushort)((mLastMsgId + 1) % MAXMSGID);
                    if (!r)
                        break;
                }
            }
            return r;
        }

        /// <summary>
        /// /// Read several register in word oriented area
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="howmanyregisters"></param>
        /// <param name="returndata"></param>
        /// <param name="withheader"></param>
        /// <returns></returns>
        public bool ReadInputRegister(ushort addr, ushort howmanyregisters, short[] returndata)
        {
            byte[] data = new byte[howmanyregisters * 2];

            bool r = ReadInputRegister(addr, howmanyregisters, data);
            int k = 0;
            if (r)
            {
                for (int i = 0; i < data.Length; i += 2)
                {
                    returndata[k] = (short)((Convert.ToInt16(data[i]) << 8) + data[i + 1]);
                    k++;
                }
            }
            return r;
        }

        /// <summary>
        /// Set digital output
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool WriteSingleCoil(ushort addr, ushort value)
        {
            if ((value != CoilOn) && (value != CoilOff))
            {
                Addlog("Invalid coil value:" + value.ToString() + " for addr: " + addr.ToString());
                return false;
            }
            if (GetStatus() != (int)LinkStatus.Connected)
            {
                Addlog("Device not connected:" + value.ToString() + " for addr: " + addr.ToString());
                return false;
            }
            bool r = false;
            byte[] data = new byte[12];
            byte bL, bH;
            lock (mSending)
            {
                bH = Convert.ToByte((mLastMsgId & 0xff00) >> 8);
                bL = Convert.ToByte(mLastMsgId & 0xff);
                data[IdH] = bH;
                data[IdL] = bL;
                data[ProtocolH] = 0;
                data[ProtocolL] = 0;    //0=Modbus
                data[ByteNH] = 0;
                data[ByteNL] = 6;       //quanti byte seguono
                data[UI] = 1;
                data[FunctionCode] = fWriteSingleCoil;
                bH = Convert.ToByte((addr & 0xff00) >> 8);
                bL = Convert.ToByte(addr & 0xff);
                data[AddrH] = bH;
                data[AddrL] = bL;
                bH = Convert.ToByte((value & 0xff00) >> 8);
                //bH = Convert.ToByte(1);
                bL = Convert.ToByte(value & 0xff);
                data[BitsH] = bH;
                data[BitsL] = bL;

                if (Send(data))
                    r = WaitReply(mLastMsgId);
                else
                    Addlog("failed to send!?!?!");
                mLastMsgId = (ushort)((mLastMsgId + 1) % MAXMSGID);
            }
            return r;
        }

        /// <summary>
        /// Write resister/output
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool WriteSingleRegister(ushort addr, ushort value)
        {
            bool r = false;
            byte[] data = new byte[12];
            byte bL, bH;
            lock (mSending)
            {
                bH = Convert.ToByte((mLastMsgId & 0xff00) >> 8);
                bL = Convert.ToByte(mLastMsgId & 0xff);
                data[IdH] = bH;
                data[IdL] = bL;
                data[ProtocolH] = 0;
                data[ProtocolL] = 0;    //0=Modbus
                data[ByteNH] = 0;
                data[ByteNL] = 6;       //quanti byte seguono
                data[UI] = 1;
                data[FunctionCode] = fWriteSingleRegister;
                bH = Convert.ToByte((addr & 0xff00) >> 8);
                bL = Convert.ToByte(addr & 0xff);
                data[AddrH] = bH;
                data[AddrL] = bL;
                bH = Convert.ToByte((value & 0xff00) >> 8);
                bL = Convert.ToByte(value & 0xff);
                data[BitsH] = bH;
                data[BitsL] = bL;

                if (Send(data))
                    r = WaitReply(mLastMsgId);
                mLastMsgId = (ushort)((mLastMsgId + 1) % MAXMSGID);
            }
            return r;
        }

        /// <summary>
        /// Set multiple digital outputs, max 1968 bits
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        /// <param name="startaddr"></param>
        /// <param name="howmanybits"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public bool WriteMultipleCoil(ushort startaddr, ushort howmanybits, byte[] values)
        {
            bool r = false;

            byte howmanybyte = (byte)Math.Ceiling((double)howmanybits / 8.0);
            byte[] data = new byte[13 + howmanybyte];
            byte bL, bH;
            lock (mSending)
            {
                bH = Convert.ToByte((mLastMsgId & 0xff00) >> 8);
                bL = Convert.ToByte(mLastMsgId & 0xff);
                data[IdH] = bH;
                data[IdL] = bL;
                data[ProtocolH] = 0;
                data[ProtocolL] = 0;    //0=Modbus
                data[ByteNH] = 0;
                data[ByteNL] = (byte)(7 + howmanybyte);       //quanti byte seguono
                //data[ByteNL] = 6;
                data[UI] = 1;
                data[FunctionCode] = fWriteMultipleCoils;
                bH = Convert.ToByte((startaddr & 0xff00) >> 8);
                bL = Convert.ToByte(startaddr & 0xff);
                data[AddrH] = bH;
                data[AddrL] = bL;
                bH = Convert.ToByte((howmanybits & 0xff00) >> 8);
                bL = Convert.ToByte(howmanybits & 0xff);
                data[BitsH] = bH;
                data[BitsL] = bL;
                data[NumberOfBytes] = howmanybyte;

                for (int i = 0; i < howmanybyte; i++)
                    data[13 + i] = values[i];

                if (Send(data, 13 + howmanybyte))
                    r = WaitReply(mLastMsgId);
                else
                    Addlog("failed to send!?!?!");

                mLastMsgId = (ushort)((mLastMsgId + 1) % MAXMSGID);
            }
            return r;
        }

        /// <summary>
        /// Write multiple registers
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="howmanyregisters"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public bool WriteMultipleRegister(ushort addr, ushort howmanyregisters, byte[] values)
        {
            bool r = false;

            byte howmanybyte = 0;

            byte[] data = new byte[13 + (MaxWriteRegisters * 2)];
            byte bL, bH;

            int senddatapos = 0;
            ushort last_cycle_registercount = (ushort)(howmanyregisters % MaxWriteRegisters);

            if (last_cycle_registercount == 0)
                last_cycle_registercount = MaxWriteRegisters;

            int cycles = 1;
            cycles = (int)Math.Ceiling((double)howmanyregisters / (double)MaxWriteRegisters);

            howmanyregisters = MaxWriteRegisters;

            lock (mSending)
            {
                for (int k = 0; k < cycles; k++)
                {
                    if (k == (cycles - 1))
                        howmanyregisters = last_cycle_registercount;

                    howmanybyte = (byte)(howmanyregisters * 2);

                    bH = Convert.ToByte((mLastMsgId & 0xff00) >> 8);
                    bL = Convert.ToByte(mLastMsgId & 0xff);
                    data[IdH] = bH;
                    data[IdL] = bL;
                    data[ProtocolH] = 0;
                    data[ProtocolL] = 0;    //0=Modbus
                    data[ByteNH] = 0;
                    data[ByteNL] = (byte)(7 + howmanybyte);       //quanti byte seguono
                    //data[ByteNL] = 6;
                    data[UI] = 1;
                    data[FunctionCode] = fWriteMultipleRegisters;
                    bH = Convert.ToByte((addr & 0xff00) >> 8);
                    bL = Convert.ToByte(addr & 0xff);
                    data[AddrH] = bH;
                    data[AddrL] = bL;
                    bH = (byte)((howmanyregisters & 0xff00) >> 8);
                    bL = (byte)(howmanyregisters & 0xff);
                    data[BitsH] = bH;
                    data[BitsL] = bL;
                    data[NumberOfBytes] = howmanybyte;

                    for (int i = 0; i < howmanybyte; i++)
                    {
                        data[13 + i] = values[senddatapos];
                        senddatapos++;
                    }

                    if (Send(data, 13 + howmanybyte))
                        r = WaitReply(mLastMsgId);
                    else
                        Addlog("failed to send!?!?!");

                    addr += MaxWriteRegisters;

                    mLastMsgId = (ushort)((mLastMsgId + 1) % MAXMSGID);
                }
            }
            return r;
        }

        /// <summary>
        /// Write multiple registers, max 123
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="startaddr"></param>
        /// <param name="howmanyregisters"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public bool WriteMultipleRegister(ushort startaddr, ushort howmanyregisters, ushort[] values)
        {
            int k = 0;

            byte howmanybyte = (byte)(howmanyregisters * 2);

            byte[] data = new byte[howmanybyte];
            for (int i = 0; i < howmanybyte; i += 2)      //Modbus usa Big-Eindian
            {
                data[i] = (byte)(values[k] >> 8);
                data[i + 1] = (byte)(values[k] & 0xFF);
                k++;
            }
             bool r = WriteMultipleRegister(startaddr, howmanyregisters, data);
            return r;
        }
    }
}