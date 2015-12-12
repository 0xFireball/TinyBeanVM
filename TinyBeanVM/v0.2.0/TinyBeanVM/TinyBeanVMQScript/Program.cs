/*

 */
using System;

namespace TinyBeanVMQScript
{
	class Program
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("TinyBeanVM QScript Compiler v0.1");
			if (args.Length != 2)
			{
				Console.WriteLine("Usage: TinyBeanVMQScript <input.tbq> <output.tbasm>");
				return;
			}
			string inputFile = args[0];
			string outputFile = args[1];
			string[] code = System.IO.File.ReadAllLines(inputFile);
			string[] output = new QSCompiler().CompileFile(code);
			System.IO.File.WriteAllLines(outputFile, output);
		}
	}
}