/*

 */
using System;
using System.IO;
using System.Text;
using System.Linq;
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
		short lblId;
		int orgLoc = 0;
		Dictionary<string,short> lbltable;
		public TinyBeanVM()
		{
			registers = new SystemRegisters();
			lblId = 1;
		}
		public MemoryStream AssembleCode(StreamReader sReader)
		{
			MemoryStream output = new MemoryStream();
			BinaryWriter outputStream = new BinaryWriter(output);
			List<string> cLines = new List<string>();
			lbltable = new Dictionary<string, short>();

			string line;
			while ((line = sReader.ReadLine()) != null)
		    {
				cLines.Add(line);
		    }
			code = cLines.ToArray();
			Console.Write("Writing header...");
			//Magic shorts
			short[] magicshorts = new short[] {(short)'T', (short)'B', (short)'V', (short)'M'};
			for (int i = 0; i < magicshorts.Length; i++)
			{
				outputStream.Write(magicshorts[i]);
			}
			Console.WriteLine("Done");
			Console.WriteLine("Begin parsing code { >");
			short[] pO = Parse();
			for (int i = 0; i<pO.Length; i++)
			{
				outputStream.Write(pO[i]);
			}
			Console.WriteLine("< } Done Parsing Code");
			/*
			output = new MemoryStream();
			outputStream = new BinaryWriter(output);
			short[] dmagicshorts = new short[] {(short)'T', (short)'B', (short)'V', (short)'M'};
			for (int i = 0; i < dmagicshorts.Length; i++)
			{
				outputStream.Write(dmagicshorts[i]);
			}
			*/
			outputStream.Flush();
			output.Position = 0;
			return output;
		}
		public string Reverse(string text)
		{
		   if (text == null) return null;
		
		   // this was posted by petebob as well 
		   char[] array = text.ToCharArray();
		   Array.Reverse(array);
		   return new String(array);
		}
		private string[] PreProcessLine(string line)
		{
			List<string> ins = new List<string>{line};
			if (ins[0].Contains(".mem"))
			{
				string c = ins[0];
				string[] opc = reverseStringFormat(" .mem '{0}',0", c); //.mem '<some value>'
				bool rm = ins.Remove(c);
				string data = opc[0];//Reverse(opc[0]);
				int dl = data.Length;
				for (int i =0;i<dl;i++)
				{
					char ch = data[i];
					int cv = Convert.ToInt32(ch);
					string nL = String.Format(" mov ${0},{1}",orgLoc+i,cv);
					ins.Add(nL);
				}
				ins.Add(String.Format(" mov ${0},{1}",orgLoc+dl,-1));
				orgLoc+=dl+1;
			}
			if (ins[0].Contains(".org"))
			{
				string c = ins[0];
				string[] opc = reverseStringFormat(" .org {0}", c); //.mem '<some value>'
				ins.Remove(c);
				string data = opc[0];
				int.TryParse(data, out orgLoc);
			}
			return ins.ToArray();
		}
		private void RunPreprocessor()
		{
			List<string> mcode = code.ToList();
			List<string> newcode = new List<string>(mcode);
			for (int i=0;i<mcode.Count;i++)
			{
				string line = mcode[i];
				string[] newlines = PreProcessLine(line);
				int insIx = newcode.IndexOf(line);
				newcode.Remove(line);
				newcode.InsertRange(insIx, newlines);
			}
			mcode = newcode;
			code = mcode.ToArray();
		}
		private short[] Parse()
		{
			List<short> outputBy = new List<short>();
			Console.Write("Running preprocessor..");
			RunPreprocessor(); //preprocess special items
			Console.WriteLine(".Done");
			Console.Write("Begin parsing {0} lines in memory...",code.Length);
			for (int i = 0;i<code.Length;i++)
			{
				string line = code[i];
				outputBy.AddRange(ParseLine(line,i));
			}
			Console.WriteLine("Done.");
			return outputBy.ToArray();
		}
		private short[] ParseLine(string c, int line)
		{
			int error=0;
			List<short> rv = new List<short>();
			bool label = false;
			if (c.EndsWith(":"))
			{
				label = true;
			}
			if (c=="")
			{
				rv.AddRange(ASMParse.nop()); //NOP
			}
			if (!label)
			{
				//Parse instruction
				string[] opc = reverseStringFormat(" {0} {1},{2}", c);
				if (opc.Length<3)
				{
					opc = reverseStringFormat(" {0} {1}", c);
				}
				if (opc.Length<2)
				{
					opc = reverseStringFormat(" {0}", c);
				}
				short[] bop = ASMParse.s2opc("nop",out error);
				short[] b1 = ASMParse.r2by("nop");
				short[] b2 = ASMParse.r2by("nop");
				if (opc.Length>=3)
				{
					bop = ASMParse.s2opc(opc[0],out error);
					b1 = ASMParse.r2by(opc[1]);
					b2 = ASMParse.r2by(opc[2]);
				}
				else if (opc.Length>=2)
				{
					bop = ASMParse.s2opc(opc[0],out error);
					b1 = ASMParse.r2by(opc[1]);
				}
				else if (opc.Length>=1)
				{
					bop = ASMParse.s2opc(opc[0],out error);
				}
				rv.AddRange(bop);
				rv.AddRange(b1);
				rv.AddRange(b2);
			}
			else //It is a label
			{
				//set the label id
				
				short mTmpLblid = lblId; //temporary label id
				string rln = c.Substring(0, c.Length-1); //raw label name
				
				if (rln == "start")
					mTmpLblid = 0; //If start, replace with 0 (starting address)
				
				short[] lb = ASMParse.lbl((short)mTmpLblid);
				
				for (int i=0;i<code.Length;i++)
				{
					code[i] = code[i].Replace(rln,mTmpLblid.ToString()); //get raw label name and replace all instances
				}
				
				lbltable.Add(rln, mTmpLblid);
				
				lblId++;
				rv.AddRange(lb); //6 shorts
			}
			if (error!=0)
			{
				Console.WriteLine("Error on line {0}",line);
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
