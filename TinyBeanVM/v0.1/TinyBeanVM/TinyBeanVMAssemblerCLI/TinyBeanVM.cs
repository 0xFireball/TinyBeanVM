/*

 */
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using TinyBeanVMAssemblerCLI.Parsing;

namespace TinyBeanVMAssemblerCLI
{
	class SystemRegisters
	{
		short A; //general
		short B; //general
		short T; //general
		short X; //general
		short Z; //zero
		short C; //carry
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
		string[] code;
		int lblId;
		public TinyBeanVM()
		{
			registers = new SystemRegisters();
			lblId = 1;
		}
		public MemoryStream AssembleCode(StreamReader sReader)
		{
			MemoryStream output = new MemoryStream();
			List<string> cLines = new List<string>();
			string line;
			while ((line = sReader.ReadLine()) != null)
		    {
				cLines.Add(line);
		    }
			code = cLines.ToArray();
			//Magic bytes
			byte[] magicBytes = new byte[] {0x54, 0x42, 0x56, 0x4D};
			for (int i = 0; i < magicBytes.Length; i++)
			{
				output.WriteByte(magicBytes[i]);
			}
			byte[] pO = Parse();
			for (int i = 0; i<pO.Length; i++)
			{
				output.WriteByte(pO[i]);
			}
			return output;
		}
		private byte[] Parse()
		{
			List<byte> outputBy = new List<byte>();
			foreach (string line in code)
			{
				outputBy.AddRange(ParseLine(line));
			}
			return outputBy.ToArray();
		}
		private byte[] ParseLine(string c)
		{
			List<byte> rv = new List<byte>();
			bool label = false;
			if (c.EndsWith(":"))
			{
				label = true;
			}
			if (c=="")
			{
				rv.Add(0x0f); //NOP
			}
			if (!label)
			{
				//Parse instruction
				string[] opc = reverseStringFormat(" {0} {1},{2}", c);
				byte bop = ASMParse.s2opc(opc[0]);
				byte b1 = ASMParse.r2by(opc[1]);
				byte b2 = ASMParse.r2by(opc[2]);
				rv.Add(bop);
				rv.Add(b1);
				rv.Add(b2);
			}
			return rv.ToArray();
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
