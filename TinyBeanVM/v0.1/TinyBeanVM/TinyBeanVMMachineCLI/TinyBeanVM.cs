/*

 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TinyBeanVMAssemblerCLI.Parsing;

namespace TinyBeanVMMachineCLI
{
	class TinyBeanVMException: Exception
	{
		public TinyBeanVMException() : base() {}
		public TinyBeanVMException(string message) : base(message) {}
	}
	class SystemRegisters
	{
		public short A; //general
		public short B; //general
		public short T; //general
		public short X; //general
		public short Z; //zero
		public short C; //carry
		public SystemRegisters()
		{
			A = 0;
			B = 0;
			T = 0;
			X = 0;
			Z = 0;
			C = 0;
		}
	}
	/// <summary>
	/// Description of TinyBeanVM.
	/// </summary>
	public class TinyBeanVM
	{
		SystemRegisters registers;
		BinaryReader bCode;
		int cep = -1; //current excecution point
		public TinyBeanVM()
		{
			registers = new SystemRegisters();
		}
		public void ExecuteCode(MemoryStream mCode)
		{
			bCode = new BinaryReader(mCode);
			CheckHeader();
		}
		private void CheckHeader()
		{
			//Magic bytes
			byte[] magicBytes = new byte[] {0x54, 0x42, 0x56, 0x4D};
			for (int i = 0; i < magicBytes.Length; i++)
			{
				int rBy = bCode.ReadByte();
				bool validTBVM = magicBytes[i] == rBy;
				if (!validTBVM)
				{
					throw new TinyBeanVMException("Incorrect TinyBeanVM Header!");
				}
			}
			byte[] bc = bCode.ReadBytes(int.MaxValue);
			try
			{
				Parse(bc);
			}
			catch (Exception ex)
			{
				throw new TinyBeanVMException(String.Format("TinyBeanVM Excecution Exception: {0}",ex.ToString()));
			}
		}
		private void Parse(byte[] bc)
		{
			cep = 0;
			byte n1 = bc[cep];
			bool label = n1 == ASMParse.lbl();
			if (!label)
			{
				ParseLine(new byte[] {bc[cep],bc[cep+1],bc[cep+2]});
				cep+=3;
			}
		}
		private void ParseLine(byte n1, byte n2, byte n3)
		{
			if (n1 == 0x10) //lda
			{
				registers.A = n2;
			}
			if (n1 == 0x11) //mov
			{
				
			}
		}
		
		private string[] reverseStringFormat(string template, string str)
		{
		    string pattern = "^" + Regex.Replace(template, @"\{[0-9]+\}", "(.*?)") + "$";
		
		    Regex r = new Regex(pattern);
		    Match m = r.Match(str);
		
		    List<string> ret = new List<string>();
		
		    for (int i = 1; i < m.Groups.Count; i++)
		    {
		        ret.Add(m.Groups[i].Value);
		    }
		
		    return ret.ToArray();
		}
		
		
	}
}
