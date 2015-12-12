/*

 */
using System;

namespace TinyBeanVMAssemblerCLI.Parsing
{
	/// <summary>
	/// Description of ASMParse.
	/// </summary>
	public class ASMParse
	{
		public static byte lbl()
		{
			return 0x5f; //label
		}
		public static byte s2opc(string s)
		{
			switch (s)
			{
				case "lda":
					return 0x10;
					break;
				case "mov":
					return 0x11;
					break;
				default:
					return 0x0f;
					break;
			}
		}

		public static byte r2by(string s)
		{
			switch (s)
			{
				case "a":
					return 0x01;
					break;
				default:
					int d = 0x00;
					int.TryParse(s, out d);
					return (byte)d;
					break;
			}
		}
	}
}
