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
			if (args.Length != 1)
			{
				Console.WriteLine("Usage: TinyBeanVMMachineCLI <input.tb>");
				return;
			}
			string inputFile = args[0];
			MemoryStream ms = new MemoryStream(File.ReadAllBytes(inputFile));
			new TinyBeanVM().ExecuteCode(ms);
		}
	}
}