using System;
using System.IO.Ports;
using System.Management;
using System.Threading;
using Joycon4CS;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Globalization;

namespace DecaMoveCommon
{
	// Token: 0x0200000A RID: 10
	public enum DongleState
	{
		// Token: 0x0400002A RID: 42
		Closed,
		// Token: 0x0400002B RID: 43
		Open,
		// Token: 0x0400002C RID: 44
		Paired,
		// Token: 0x0400002D RID: 45
		Streaming
	}
}

namespace DecaMoveEmulator
{
	class Program
    {
		static byte[] batpkt = { 0x62, 0x62, 0x13, 0x37 };//, 0x0D, 0x0A };
		static byte[] verpkt = { 0x76, 0x76, 3, 1, 3, 3, 7, 3, 1, 3, 3, 7, 3, 1, 3, 3, 7, 3, 7, 3, 1, 3, 3, 7, 3, 7 };//, 0x0D, 0x0A };
		static byte[] onpkt = { 0x66, 0x66, 0x01 };//, 0x0D, 0x0A };

		static dynamic decaMoveChannel;
		static MethodInfo processPacket;

		static void Main(string[] args)
        {
			CheckForCom();
			PatchDMS();
			StreamJoyconData();
        }

		static void CheckForCom()
        {
			if(!Directory.Exists(@"C:\Program Files (x86)\com0com"))
            {
				Console.WriteLine("com0com not detected! Press enter to run installer.");
				Console.WriteLine("IMPORTANT!!!!!! In the 'Choose Components' section UNCHECK 'CNCA0 <-> CNCB0' otherwise it will not be setup properly.");
				Console.ReadLine();
				Process.Start("Setup_com0com_v3.0.0.0_W7_x64_signed.exe");
				Console.WriteLine("Press enter when finished installing.");
				Console.ReadLine();
			}
        }
		
		static void PatchDMS()
        {
			new Thread(() =>
			{
				var currDir = Directory.GetCurrentDirectory();
				Directory.SetCurrentDirectory(@"C:\Program Files\Megadodo Games\DecaHub");

				var dms2asm = Assembly.LoadFrom("DecaMoveService2.exe");
				var dmstype = dms2asm.GetType("DecaMoveService2.DecaMoveService");
				var dmctype = dms2asm.GetType("DecaMoveService2.DecaMoveChannel");
				dynamic decaMoveService = Activator.CreateInstance(dmstype, new object[] { });

				MethodInfo StartService = dmstype.GetMethod("OnStart", BindingFlags.NonPublic | BindingFlags.Instance);
				StartService.Invoke(decaMoveService, new object[] { new string[] { } });

				PropertyInfo DecaMoveChannel = dmstype.GetProperty("DecaMoveChannel", BindingFlags.NonPublic | BindingFlags.Instance);
				decaMoveChannel = DecaMoveChannel.GetValue(decaMoveService);

				decaMoveChannel.DmInfo.Streaming.Set(true);
				decaMoveChannel.DmInfo.Paired.Set(true);

				FieldInfo vendorIdStr = dmctype.GetField("vendorIdStr", BindingFlags.NonPublic | BindingFlags.Instance);
				vendorIdStr.SetValue(decaMoveChannel, "CNCA");

				FieldInfo productIdStr = dmctype.GetField("productIdStr", BindingFlags.NonPublic | BindingFlags.Instance);
				productIdStr.SetValue(decaMoveChannel, "CNCA");

				MethodInfo tryOpen = dmctype.GetMethod("TryOpen", BindingFlags.NonPublic | BindingFlags.Instance);
				tryOpen.Invoke(decaMoveChannel, new object[] { false });

				MethodInfo installStuff = dmctype.GetMethod("InstallStuff", BindingFlags.NonPublic | BindingFlags.Instance);
				installStuff.Invoke(decaMoveChannel, new object[] { true });

				processPacket = dmctype.GetMethod("ProcessPacket", BindingFlags.NonPublic | BindingFlags.Instance);
			}).Start();
		}


		static void StreamJoyconData()
        {
			while (decaMoveChannel == null || processPacket == null)
				Thread.Sleep(50);

			var joyconManager = JoyconManager.Instance;

			Console.WriteLine("Scanning for joycons...");
			joyconManager.Scan();

			Joycon jc = null;
			if (joyconManager.j.Count > 0)
			{
				joyconManager.Start();
				jc = joyconManager.j[0];
			}
			else
			{
				Console.WriteLine("Press enter to exit.");
				Console.ReadLine();
				Environment.Exit(1);
				return;
			}

			//var port = new SerialPort(GetComPort(), 256000);
			//ReadSerialData(port);

			//port.Open();

			//port.Write(verpkt, 0, verpkt.Length);
			//port.Write(onpkt, 0, onpkt.Length);
			//port.Write(batpkt, 0, batpkt.Length);

			processPacket.Invoke(decaMoveChannel, new object[] { verpkt });
			processPacket.Invoke(decaMoveChannel, new object[] { onpkt });
			processPacket.Invoke(decaMoveChannel, new object[] { batpkt });

			while (true)
			{
				JoyconManager.Instance.Update();

				var pkt = EncodeQuaternion(jc.GetVector());
				//port.Write(pkt, 0, pkt.Length);
				processPacket.Invoke(decaMoveChannel, new object[] { pkt });
				Thread.Sleep(5);
			}
		}

        private static void ReadSerialData(SerialPort port)
        {
			new Thread(() =>
			{
				while (!port.IsOpen) { Thread.Sleep(100); }
				while(port.IsOpen)
                {
					Console.WriteLine(port.ReadLine());
                }
			}).Start();
        }

		private static string GetComPort()
		{
			string result;
			using (ManagementObjectCollection devices = new ManagementClass("Win32_SerialPort").GetInstances())
			{
				foreach (ManagementBaseObject device in devices)
				{
					string pnpDeviceId = device["PNPDeviceID"].ToString();
					if (pnpDeviceId.Contains("CNCB"))
					{
						return device["DeviceID"].ToString();
					}
				}
				result = null;
			}
			return result;
		}

		public static byte[] EncodeQuaternion(Quaternion q)
        {
			byte[] bytes = { 0x78, 0x78, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

			var x = BitConverter.GetBytes((short)(q.X / 6.10351563E-05f));
			bytes[4] = x[0];
			bytes[5] = x[1];

			var y = BitConverter.GetBytes((short)(q.Y / 6.10351563E-05f));
			bytes[6] = y[0];
			bytes[7] = y[1];

			var z = BitConverter.GetBytes((short)(q.Z / 6.10351563E-05f));
			bytes[8] = z[0];
			bytes[9] = z[1];

			var w = BitConverter.GetBytes((short)(q.W / 6.10351563E-05f));
			bytes[2] = w[0];
			bytes[3] = w[1];

			return bytes;
        }
	}
}
