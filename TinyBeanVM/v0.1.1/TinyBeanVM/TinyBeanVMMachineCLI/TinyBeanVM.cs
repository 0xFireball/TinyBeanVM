/*

 */
using System;
using System.IO;
using System.Linq;
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
	class TinyBeanVMInvalidOperationException : Exception
	{
		public TinyBeanVMInvalidOperationException() : base() {}
		public TinyBeanVMInvalidOperationException(string message) : base(message) {}
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
		public void AssignById(int id, short value)
		{
			switch (id)
			{
				case 1:
					A = value;
					break;
				case 2:
					B = value;
					break;
				case 3:
					T = value;
					break;
				case 4:
					X = value;
					break;
			}
		}
		public short GetById(int id)
		{
			short ret = -1;
			switch (id)
			{
				case 1:
					ret = A;
					break;
				case 2:
					ret = B;
					break;
				case 3:
					ret = T;
					break;
				case 4:
					ret = X;
					break;
			}
			return ret;
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
			//Magic shorts
			short[] magicshorts = new short[] {(short)'T', (short)'B', (short)'V', (short)'M'};
			for (int i = 0; i < magicshorts.Length; i++)
			{
				short rBy = bCode.ReadInt16();
				bool validTBVM = magicshorts[i] == rBy;
				if (!validTBVM)
				{
					throw new TinyBeanVMException("Incorrect TinyBeanVM Header!");
				}
			}
			List<short> __b = new List<short>();
			short[] bc;
			while (bCode.BaseStream.Position != bCode.BaseStream.Length)
			{
				__b.Add(bCode.ReadInt16());
			}
			bc = __b.ToArray();
			try
			{
				Parse(bc);
			}
			catch (Exception ex)
			{
				throw new TinyBeanVMException(String.Format("TinyBeanVM Excecution Exception: {0}",ex.ToString()));
			}
		}
		private void Parse(short[] bc)
		{
			cep = 0;
			while (cep<bc.Length) //loop through the code
			{
				short[] n1 = new short[] {bc[cep],bc[cep+1]};
				bool label = n1 == ASMParse.lbl();
				if (!label)
				{
					//short[] n1 = new short[]{ bc[cep] , bc[cep+1]};
					short[] n2 = new short[]{ bc[cep+2] , bc[cep+3]};
					short[] n3 = new short[]{ bc[cep+4] , bc[cep+5]};
					ParseLine(n1, n2, n3);
					//ParseLine(new short[] {bc[cep],bc[cep+1],bc[cep+2]});
					cep+=6;
				}				
			}
		}
		private void ParseLine(short[] n1, short[] n2, short[] n3)
		{
			if ( n1.SequenceEqual(ASMParse.s2opc("lda")) ) //lda
			{
				int by_r_type2 = ASMParse.by_r_type(n2);
				if (by_r_type2 == 0)
				{
					registers.A = ASMParse.lit2sh(n2);
				}
				if (by_r_type2 == 1)
				{
					registers.A = registers.GetById(ASMParse.rlit2sh(n2));
				}				
			}
			if ( n1.SequenceEqual(ASMParse.s2opc("mov")) ) //mov
			{
				int by_r_type2 = ASMParse.by_r_type(n2);
				int mloc2=-1; //location of destination operand
				if (by_r_type2 == 0)
				{
					throw new TinyBeanVMInvalidOperationException("Attempted to write protected memory. [MOV]");
				}
				if (by_r_type2 == 1)
				{
					mloc2 = ASMParse.rlit2sh(n2);
				}
				int by_r_type3 = ASMParse.by_r_type(n3);
				short v3=-1;
				if (by_r_type3 == 0)
				{
					v3 = ASMParse.lit2sh(n3); //get value of literal
				}
				
				if (by_r_type3 == 1)
				{
					v3 = registers.GetById(ASMParse.rlit2sh(n3)); //get value of register
				}
				//move value into destination
				if (by_r_type2 == 1) //register target
				{
					registers.AssignById(mloc2, v3);
				}
			}
			if ( n1.SequenceEqual(ASMParse.s2opc("dmpreg")) ) //dmpreg
			{
				Console.WriteLine("DMPREG");
				Console.WriteLine("A: {0}",registers.A);
				Console.WriteLine("B: {0}",registers.B);
				Console.WriteLine("T: {0}",registers.T);
				Console.WriteLine("X: {0}",registers.X);
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
