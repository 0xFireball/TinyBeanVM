/*

 */
using System;
using System.IO;

namespace TinyBeanVMAssemblerCLI
{
	class Program
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("TinyBeanVM Assembler v0.1");
			if (args.Length != 2)
			{
				Console.WriteLine("Usage: TinyBeanVMAssemblerCLI <input.tbasm> <output.tb>");
				return;
			}
			string inputFile = args[0];
			string outputFile = args[1];
			
			MemoryStream outputStream = new TinyBeanVM().AssembleCode(new StreamReader(inputFile));
			FileStream outputFS = new FileStream(outputFile, FileMode.Create);
			outputStream.Position = 0;
			outputStream.CopyTo(outputFS);
			outputStream.Close();
			outputFS.Close();
		}
	}
}