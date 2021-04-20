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
using Pose;
using HarmonyLib;

namespace DecaMoveEmulator
{
	class StatePatch
    {
		public static void MyPostfix(ref object __result)
		{
			__result = 3;
		}
	}

	class SerialWritePatch
    {
		public static void MyPrefix(object __instance){}

		public static void MyPostfix(object __instance, string text)
        {
			Console.WriteLine("[Serial Packet] " + text);
        }
    }

	class Program
    {
		static byte[] batpkt = { 0x62, 0x62, 0x13, 0x37 };//, 0x0D, 0x0A };
		static byte[] verpkt = { 0x76, 0x76, 3, 1, 3, 3, 7, 3, 1, 3, 3, 7, 3, 1, 3, 3, 7, 3, 7, 3, 1, 3, 3, 7, 3, 7 };//, 0x0D, 0x0A };
		static byte[] onpkt = { 0x66, 0x66, 0x01 };//, 0x0D, 0x0A };

		static dynamic decaMoveChannel;
		static MethodInfo processPacket;

		static void Main(string[] args)
        {
			PatchDMS();
			StreamJoyconData();
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

				var harmony = new Harmony("com.example.patch");

				MethodInfo getState = dmctype.GetMethod("get_State", BindingFlags.Instance | BindingFlags.Public);
				var mPostfix = typeof(StatePatch).GetMethod("MyPostfix", BindingFlags.Static | BindingFlags.Public);
				harmony.Patch(getState, new HarmonyMethod(mPostfix), new HarmonyMethod(mPostfix));

				MethodInfo write = dmctype.GetMethod("Write", BindingFlags.NonPublic | BindingFlags.Instance);
				var mPrefixWrite = typeof(SerialWritePatch).GetMethod("MyPrefix", BindingFlags.Static | BindingFlags.Public);
				var mPostfixWrite = typeof(SerialWritePatch).GetMethod("MyPostfix", BindingFlags.Static | BindingFlags.Public);
				harmony.Patch(write, new HarmonyMethod(mPrefixWrite), new HarmonyMethod(mPostfixWrite));

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

			processPacket.Invoke(decaMoveChannel, new object[] { verpkt });
			processPacket.Invoke(decaMoveChannel, new object[] { onpkt });
			processPacket.Invoke(decaMoveChannel, new object[] { batpkt });

			while (true)
			{
				JoyconManager.Instance.Update();

				var pkt = EncodeQuaternion(jc.GetVector());
				processPacket.Invoke(decaMoveChannel, new object[] { pkt });
				Thread.Sleep(5);
			}
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
