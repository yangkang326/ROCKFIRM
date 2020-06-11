using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HslCommunication;
using HslCommunication.ModBus;

namespace ROCKFIRM
{
    class recorddata
    {
		private OperateResult operate1;

		private OperateResult operate2;

		public ModbusTcpNet modbusTcpclient_led = new ModbusTcpNet("192.168.1.12", 502, 1);

		public ModbusTcpNet modbusTcpclient_meter = new ModbusTcpNet("192.168.1.13", 502, 1);

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

		public bool connect_client0()
		{
			this.modbusTcpclient_led.ConnectTimeOut = 100;
			this.operate1 = this.modbusTcpclient_led.ConnectServer();
			this.connect1_status = this.operate1.IsSuccess;
			return this.operate1.IsSuccess;
		}

		public bool connect_client1()
		{
			this.modbusTcpclient_meter.ConnectTimeOut = 100;
			this.operate2 = this.modbusTcpclient_meter.ConnectServer();
			this.connect2_status = this.operate2.IsSuccess;
			return this.operate2.IsSuccess;
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

		public bool getmeter()
		{
			OperateResult operateResult = this.modbusTcpclient_meter.ReadInt16("0");
			OperateResult operateResult2 = this.modbusTcpclient_meter.ReadInt16("1");
			bool flag = !operateResult.IsSuccess || !operateResult2.IsSuccess;
			if (flag)
			{
				this.operate2.IsSuccess = false;
				this.connect_client1();
			}
			else
			{
				this.fydeg = this.modbusTcpclient_meter.ReadInt16("0").Content.ToString();
				this.fgdeg = this.modbusTcpclient_meter.ReadInt16("1").Content.ToString();
			}
			return this.operate1.IsSuccess & this.operate2.IsSuccess;
		}
	}
}
