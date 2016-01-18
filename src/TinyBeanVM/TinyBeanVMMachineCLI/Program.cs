/*

 */
using System;
using System.IO;

namespace TinyBeanVMMachineCLI
{
	class Program
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("TinyBeanVM Machine v0.1");
			if (args.Length < 1)
			{
				Console.WriteLine("Usage: TinyBeanVMMachineCLI <input.tb> [-debug/-disassemble]");
				return;
			}
			Console.ForegroundColor = ConsoleColor.White;
			bool debug = args.Length>1&&args[1]=="-debug";
			bool disassemble = args.Length>1&&args[1]=="-disassemble";
			int debugLevel = debug?1:0;
			if (disassemble)
			{
				debugLevel = 2;
			}
			string inputFile = args[0];
			MemoryStream ms = new MemoryStream(File.ReadAllBytes(inputFile));
			new TinyBeanVM().ExecuteCode(ms,debugLevel);
		}
	}
}