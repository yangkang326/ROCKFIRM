using HslCommunication;
using HslCommunication.Core.IMessage;
using HslCommunication.ModBus;
using System;
using System.Net.Sockets;
using System.Text;

namespace ROCKFIRM
{
    internal class recorddata
    {
        private Socket socketCore = null;
        public bool connectSuccess;
        private byte[] buffer = new byte[2048];
        private OperateResult operate1;

        private OperateResult operate2;

        public ModbusTcpMessage _modbusTcpMessage = new ModbusTcpMessage();
        //modbus

        public ModbusTcpNet modbusTcpclient_led = new ModbusTcpNet("192.168.1.12", 502, 1);

        public string data_time
        {
            get;
            set;
        }

        public string fydeg
        {
            get;
            set;
        }

        public string fgdeg
        {
            get;
            set;
        }

        public bool led1_status
        {
            get;
            set;
        }

        public bool led2_status
        {
            get;
            set;
        }

        public bool connect1_status
        {
            get;
            set;
        }

        public bool connect2_status
        {
            get;
            set;
        }

        public string meter
        {
            get;
            set;
        }

        public bool connect_client0()
        {
            this.modbusTcpclient_led.ConnectTimeOut = 100;
            this.operate1 = this.modbusTcpclient_led.ConnectServer();
            this.connect1_status = this.operate1.IsSuccess;
            return this.operate1.IsSuccess;
        }

        public bool connect_meter()
        {
            try
            {
                socketCore?.Close();
                socketCore = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                connectSuccess = false;
                new System.Threading.Thread(() =>
                {
                    System.Threading.Thread.Sleep(2000);
                    if (!connectSuccess) socketCore?.Close();
                }).Start();
                socketCore.Connect(System.Net.IPAddress.Parse("192.168.1.13"), int.Parse("4196"));
                socketCore.BeginReceive(buffer, 0, 2048, SocketFlags.None, new AsyncCallback(ReceiveCallBack), socketCore);
                connectSuccess = true;
            }
            catch (Exception ex)
            {
                //MessageBox.Show(HslCommunication.StringResources.Language.ConnectedFailed + Environment.NewLine + ex.Message);
            }
            return connectSuccess;
        }

        private void ReceiveCallBack(IAsyncResult ar)
        {
            try
            {
                int length = socketCore.EndReceive(ar);
                socketCore.BeginReceive(buffer, 0, 2048, SocketFlags.None, new AsyncCallback(ReceiveCallBack), socketCore);

                if (length == 0) return;

                byte[] data = new byte[length];
                Array.Copy(buffer, 0, data, 0, length);
                string msg = string.Empty;
                msg = Encoding.ASCII.GetString(data);
                string msgstr = msg.ToString();
                fydeg = msgstr.Substring(2, 8);
                fgdeg = msg.Substring(13, 8);
                meter = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "    " + msg + Environment.NewLine;
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
            }
        }

        public bool getledstatus()
        {
            OperateResult operateResult = this.modbusTcpclient_led.ReadCoil("100");
            OperateResult operateResult2 = this.modbusTcpclient_led.ReadCoil("101");
            bool flag = operateResult.IsSuccess & operateResult2.IsSuccess;
            if (flag)
            {
                this.led1_status = this.modbusTcpclient_led.ReadCoil("100").Content;
                this.led2_status = this.modbusTcpclient_led.ReadCoil("101").Content;
            }
            return operateResult.IsSuccess & operateResult2.IsSuccess;
        }

        public void chang_led1_status()
        {
            bool connect1_status = this.connect1_status;
            if (connect1_status)
            {
                this.getledstatus();
                bool led1_status = this.led1_status;
                if (led1_status)
                {
                    this.modbusTcpclient_led.Write("100", false);
                }
                else
                {
                    this.modbusTcpclient_led.Write("100", true);
                }
            }
            else
            {
                this.connect_client0();
            }
        }

        public void chang_led2_status()
        {
            bool connect1_status = this.connect1_status;
            if (connect1_status)
            {
                this.getledstatus();
                bool led2_status = this.led2_status;
                if (led2_status)
                {
                    this.modbusTcpclient_led.Write("101", false);
                }
                else
                {
                    this.modbusTcpclient_led.Write("101", true);
                }
            }
            else
            {
                this.connect_client0();
            }
        }

        internal object SP_ReadData_DataReceived()
        {
            throw new NotImplementedException();
        }
    }
}