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
	
	public enum TinyBeanVMOutputType
	{
		Console,
		String,
		File,
	}
	
	/// <summary>
	/// Description of TinyBeanVM.
	/// </summary>
	public class TinyBeanVM
	{
		SystemRegisters registers;
		SystemMemory memory;
		SystemStack stack;
		BinaryReader bCode;
		TinyBeanVMOutputType outputType;
		Dictionary<short,int> labels;
		int debugLevel;
		int cep = -1; //current excecution point
		public void DebugS(string str)
		{
			if (debugLevel>0)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write("[DEBUG] ");
				Console.WriteLine(str);
				Console.ForegroundColor = ConsoleColor.White;
			}
		}
		public void DisA(string str)
		{
			if (debugLevel>1)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write("[DISASM] ");
				Console.WriteLine(str);
				Console.ForegroundColor = ConsoleColor.White;
			}
		}
		public TinyBeanVM(TinyBeanVMOutputType vmOutputType=TinyBeanVMOutputType.Console)
		{
			registers = new SystemRegisters(); //Registers
			memory = new SystemMemory(4096); //4k ram (2048 16-bit values)
			stack = new SystemStack(256); //stack of size 256
			outputType = vmOutputType;
		}
		public void ExecuteCode(MemoryStream mCode,int debugLevel)
		{
			DebugS("Starting VM...");
			this.debugLevel = debugLevel;
			bCode = new BinaryReader(mCode);
			DebugS("Checking Header...");
			CheckHeader();
		}
		public void WriteLine(params object[] args)
		{
			Console.WriteLine(args);
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
					DebugS("Bad header.");
					throw new TinyBeanVMException("Incorrect TinyBeanVM Header!");
				}
			}
			DebugS("Valid header.");
			DebugS("Reading bytecode...");
			List<short> __b = new List<short>();
			short[] bc;
			while (bCode.BaseStream.Position != bCode.BaseStream.Length)
			{
				__b.Add(bCode.ReadInt16());
			}
			bc = __b.ToArray();
			#if DEBUG
			DebugS("Starting Parse...Debug Mode...");
			Parse(bc);
			#else
			try
			{
				DebugS("Starting Parse...");
				Parse(bc);
			}
			catch (Exception ex)
			{
				DebugS("Unhandled Internal Exception: "+ex.ToString());
				throw new TinyBeanVMException(String.Format("TinyBeanVM Excecution Exception: {0}",ex.ToString()));
			}
			#endif
			DebugS("End of code - VM Exit 0");
		}
		private void Parse(short[] bc)
		{
			cep = 0; //reset Current Execution Point
			labels = new Dictionary<short,int>();
			DebugS("Running pass 1 - Flow Analysis");
			//Pass 1 - flow analysis
			DebugS(string.Format("Bytecode Length: {0}",bc.Length));
			if (bc.Length%6!=0)
				DebugS("Warning: Bytecode length is of invalid length. It must be divisible by 6.");
			while (cep<bc.Length) //loop through the code
			{
				short[] n1 = new short[] {bc[cep],bc[cep+1]};
				bool label = ASMParse.islbl(n1);
				if (!label)
				{
					//short[] n1 = new short[]{ bc[cep] , bc[cep+1]};
					short[] n2 = new short[]{ bc[cep+2] , bc[cep+3]};
					short[] n3 = new short[]{ bc[cep+4] , bc[cep+5]};
					cep+=6;
				}
				if (label)
				{
					short lid = ASMParse.lblid(n1);
					//WriteLine("Adding Label: ID: {0}, CEP: {1}",lid, cep);
					labels.Add(lid, cep); //add to list of labels.
					cep+=6;
				}
			}
			//Pass 1 complete
			DebugS("Pass 1 complete");
			cep = 0; //reset Current Execution Point
			cep = labels[0];
			DebugS("Running Pass 2 - On the fly interpretation");
			//Pass 2 - on-the-fly interpretation
			while (cep<bc.Length) //loop through the code
			{
				short[] n1 = new short[] {bc[cep],bc[cep+1]};
				bool label = ASMParse.islbl(n1);
				if (!label)
				{
					//short[] n1 = new short[]{ bc[cep] , bc[cep+1]};
					short[] n2 = new short[]{ bc[cep+2] , bc[cep+3]};
					short[] n3 = new short[]{ bc[cep+4] , bc[cep+5]};
					ExecuteLine(n1, n2, n3);
					SystemLimits();
					//ParseLine(new short[] {bc[cep],bc[cep+1],bc[cep+2]});
					cep+=6;
				}
				if (label)
				{
					short lid = ASMParse.lblid(n1);
					cep+=6;
				}
			}
			DebugS("Pass 2 Complete");
		}
		private void SystemLimits()
		{
			if (stack.stack.Count > stack.MaxSize)
			{
				throw new TinyBeanVMException("TinyBeanStackOverflowException - The stack has overflowed.");
			}
		}
		private string IntelliParse(short[] value)
		{
			string rv;
			int by_r_type2 = ASMParse.by_r_type(value);
			if (by_r_type2 == 0)
			{
				rv = ASMParse.lit2sh(value).ToString();
			}
			else if (by_r_type2 == 1)
			{
				rv = ASMParse.get_rgNfromId(registers.GetById(ASMParse.rlit2sh(value)));
			}
			else if (by_r_type2 == 2) //memory address target
			{
				rv = "$"+ASMParse.lit2sh(value).ToString();;
			}
			else if (by_r_type2==4)
			{
				short lbid = ASMParse.lit2sh(value);
				rv = ":"+lbid.ToString();
			}
			else
			{
				rv = "<UNKOWN SYMBOL>";
			}
			return rv;
		}
		private void ExecuteLine(short[] n1, short[] n2, short[] n3)
		{
			string cmdlit = ASMParse.opc2s(n1); //OPCode to string
			DisA(cmdlit+" "+IntelliParse(n2)+","+IntelliParse(n3));
			
			//WriteLine("Address: {0} - {1}", cep, cmdlit);
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
				if (by_r_type2 == 2) //memory address target
				{
					registers.A = memory.memory[ASMParse.lit2sh(n2)];
				}				
			}
			
			if ( n1.SequenceEqual(ASMParse.s2opc("lma")) ) //lma
			{
				int by_r_type2 = ASMParse.by_r_type(n2);
				short v2=-1;
				if (by_r_type2 == 0)
				{
					v2 = ASMParse.lit2sh(n2);
				}
				if (by_r_type2 == 1)
				{
					v2 = registers.GetById(ASMParse.rlit2sh(n2));
				}
				if (by_r_type2 == 2) //memory address target
				{
					v2 = memory.memory[ASMParse.lit2sh(n2)];
				}
				memory.memory[v2] = registers.A;
			}
			
			if ( n1.SequenceEqual(ASMParse.s2opc("mov")) ) //mov
			{
				int by_r_type2 = ASMParse.by_r_type(n2);
				int mloc2=-1; //location of destination operand
				if (by_r_type2 == 0) //literal target -> illegal
				{
					throw new TinyBeanVMInvalidOperationException("Attempted to write protected memory. [MOV]");
				}
				if (by_r_type2 == 1) //register target
				{
					mloc2 = ASMParse.rlit2sh(n2);
				}
				if (by_r_type2 == 2) //memory address target
				{
					mloc2 = ASMParse.lit2sh(n2);
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
				if (by_r_type3 == 2) //get memory address value
				{
					v3 = memory.memory[ASMParse.lit2sh(n3)]; //get value at address
				}
				
				//move value into destination
				if (by_r_type2 == 1) //register target
				{
					registers.AssignById(mloc2, v3);
				}
				if (by_r_type2 == 2) //memory target
				{
					memory.memory[mloc2] = v3;
				}				
			}
			
			if ( n1.SequenceEqual(ASMParse.s2opc("add")) ) //add
			{
				int by_r_type2 = ASMParse.by_r_type(n2);
				int mloc2=-1; //location of destination operand
				if (by_r_type2 == 0) //literal target -> illegal
				{
					throw new TinyBeanVMInvalidOperationException("Attempted to write protected memory. [ADD]");
				}
				if (by_r_type2 == 1) //register target
				{
					mloc2 = ASMParse.rlit2sh(n2);
				}
				if (by_r_type2 == 2) //memory address target
				{
					throw new TinyBeanVMInvalidOperationException("Cannot perform arithmetic operations on memory. [ADD]");
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
				if (by_r_type3 == 2) //get memory address value
				{
					v3 = memory.memory[ASMParse.lit2sh(n3)]; //get value at address
				}
				
				//move value into destination
				if (by_r_type2 == 1) //register target
				{
					short gr = registers.GetById(mloc2); //current register value
					short nv = (short)(v3+gr); //new value (original value+operand)
					registers.AssignById(mloc2, nv);
				}
			}
			
			if ( n1.SequenceEqual(ASMParse.s2opc("sub")) ) //sub
			{
				int by_r_type2 = ASMParse.by_r_type(n2);
				int mloc2=-1; //location of destination operand
				if (by_r_type2 == 0) //literal target -> illegal
				{
					throw new TinyBeanVMInvalidOperationException("Attempted to write protected memory. [SUB]");
				}
				if (by_r_type2 == 1) //register target
				{
					mloc2 = ASMParse.rlit2sh(n2);
				}
				if (by_r_type2 == 2) //memory address target
				{
					throw new TinyBeanVMInvalidOperationException("Cannot perform arithmetic operations on memory. [SUB]");
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
				if (by_r_type3 == 2) //get memory address value
				{
					v3 = memory.memory[ASMParse.lit2sh(n3)]; //get value at address
				}
				
				//move value into destination
				if (by_r_type2 == 1) //register target
				{
					short gr = registers.GetById(mloc2); //current register value
					short nv = (short)(gr-v3); //new value (original value-operand)
					registers.AssignById(mloc2, nv);
				}
			}
			
			if ( n1.SequenceEqual(ASMParse.s2opc("cmp")) ) //cmp
			{
				int by_r_type2 = ASMParse.by_r_type(n2);
				int mloc2=-1; //location of destination operand
				short v2 = -1;
				if (by_r_type2 == 0) //literal op -> ok
				{
					mloc2 = ASMParse.lit2sh(n2);
					v2 = (short)mloc2;
				}
				if (by_r_type2 == 1) //register op
				{
					mloc2 = ASMParse.rlit2sh(n2);
					v2=(short)registers.GetById(mloc2);
				}
				if (by_r_type2 == 2) //memory address op
				{
					mloc2 = ASMParse.lit2sh(n2);
					v2=(short)memory.memory[mloc2];
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
				if (by_r_type3 == 2) //get memory address value
				{
					v3 = memory.memory[ASMParse.lit2sh(n3)]; //get value at address
				}
				
				//set z flag accordingly
				if (v2==v3)
				{
					registers.Z = 0;
				}
				else
				{
					registers.Z = (short)(v2-v3); //is this right?
				}
			}
			
			if ( n1.SequenceEqual(ASMParse.s2opc("push")) ) //push
			{
				int by_r_type2 = ASMParse.by_r_type(n2);
				short mloc2=-1; //item to push
				
				//push value
				
				if (by_r_type2 == 0) //literal
				{
					mloc2 = ASMParse.lit2sh(n2);
				}
				if (by_r_type2 == 1) //register
				{
					mloc2 = registers.GetById(ASMParse.rlit2sh(n2));
				}
				if (by_r_type2 == 2) //memory address
				{
					mloc2 =  memory.memory[ASMParse.lit2sh(n2)];
				}
				//push value
				stack.stack.Push(mloc2);
			}
			
			if ( n1.SequenceEqual(ASMParse.s2opc("pop")) ) //pop
			{
				int by_r_type2 = ASMParse.by_r_type(n2);
				int mloc2=-1; //target location
				if (by_r_type2 == 0) //literal target -> illegal
				{
					throw new TinyBeanVMInvalidOperationException("Attempted to write protected memory. [POP]");
				}
				if (by_r_type2 == 1) //register target
				{
					mloc2 = ASMParse.rlit2sh(n2);
				}
				if (by_r_type2 == 2) //memory address target
				{
					mloc2 = ASMParse.lit2sh(n2);
				}
				short v3=stack.stack.Pop();
				//pop value into target
				if (by_r_type2 == 1) //register target
				{
					registers.AssignById(mloc2, v3);
				}
				if (by_r_type2 == 2) //memory target
				{
					memory.memory[mloc2] = v3;
				}
			}
			if (  n1.SequenceEqual(ASMParse.s2opc("bcall")) )
			{
				int by_r_type2 = ASMParse.by_r_type(n2);
				if (by_r_type2==3) //name
				{
					__bcall(ASMParse.lit2sh(n2));
				}
				else
				{
					throw new TinyBeanVMInvalidOperationException("Cannot bcall invalid location. [BCALL]");
				}
			}
			if (  n1.SequenceEqual(ASMParse.s2opc("exit")) )
			{
				int by_r_type2 = ASMParse.by_r_type(n2);
				short v2=-1;
				if (by_r_type2 == 0)
				{
					v2 = ASMParse.lit2sh(n2);
				}
				if (by_r_type2 == 1)
				{
					v2 = registers.GetById(ASMParse.rlit2sh(n2));
				}
				if (by_r_type2 == 2) //memory address target
				{
					v2 = memory.memory[ASMParse.lit2sh(n2)];
				}
				Environment.Exit(v2);
			}
			if (  n1.SequenceEqual(ASMParse.s2opc("jmp")) )
			{
				int by_r_type2 = ASMParse.by_r_type(n2);
				if (by_r_type2==4)
				{
					short lbid = ASMParse.lit2sh(n2);
					cep = labels[lbid];
					DisA(string.Format("Jump target: {0} Jump Address: {1}", lbid, cep));
				}
				else
				{
					throw new TinyBeanVMInvalidOperationException("Cannot jump to invalid location. [jmp]");
				}
			}
			if (  n1.SequenceEqual(ASMParse.s2opc("jz")) )
			{
				int by_r_type2 = ASMParse.by_r_type(n2);
				if (by_r_type2==4)
				{
					short lbid = ASMParse.lit2sh(n2);
					if (registers.Z==0)
						cep = labels[lbid];
					//WriteLine("Jump target: {0} Jump Address: {0}", lbid, cep);
				}
				else
				{
					throw new TinyBeanVMInvalidOperationException("Cannot jump to invalid location. [jz]");
				}
			}
			if (  n1.SequenceEqual(ASMParse.s2opc("jnz")) )
			{
				int by_r_type2 = ASMParse.by_r_type(n2);
				if (by_r_type2==4)
				{
					short lbid = ASMParse.lit2sh(n2);
					if (registers.Z!=0)
						cep = labels[lbid];
					//WriteLine("Jump target: {0} Jump Address: {0}", lbid, cep);
				}
				else
				{
					throw new TinyBeanVMInvalidOperationException("Cannot jump to invalid location. [jnz]");
				}
			}
			
			if (  n1.SequenceEqual(ASMParse.s2opc(".mem")) )
			{
								
			}
			
			
			
			if ( n1.SequenceEqual(ASMParse.s2opc("dmpreg")) ) //dmpreg
			{
				WriteLine("DMPREG");
				WriteLine("A: {0}",registers.A);
				WriteLine("B: {0}",registers.B);
				WriteLine("T: {0}",registers.T);
				WriteLine("X: {0}",registers.X);
				WriteLine("Z: {0}",registers.Z);
				WriteLine("C: {0}",registers.C);
			}
			if ( n1.SequenceEqual(ASMParse.s2opc("dmpmem")) ) //dmpreg
			{
				WriteLine("DMPMEM");
				for (int i=0;i<memory.memory.Length;i++)
				{
					Console.Write(" ${0}:{1} ", i, memory.memory[i]);
					if (i%4==0)
					{
						WriteLine();
					}
				}
			}
		}
		
		private void __bcall(short functionId)
		{
			
			/* * * * * * * * * * * * *
			 * FUNCTIONS
			 * 0x0001 - print
			 * 0x0002 - readline
			 * * * * * * * * * * * * */
			if (functionId == 0x001)
			{
				//print - print characters from memory
				List<char> str = new List<char>(); //string
				for (int i = 0; i<memory.memory.Length; i++)
				{
					short nc = memory.memory[i]; //read char from stack
					if (nc==-1)
					{
						break;
					}
					char c = (char)nc;
					str.Add(c);
				}
				Console.Write(str.ToArray()); //print char array to console
				registers.X = (short)str.Count; //load length into X register.
			}
			if (functionId == 0x002)
			{
				//readline - read characters, push to stack.
				List<char> str = Console.ReadLine().ToList(); //string
				str.Reverse();
				stack.stack.Push(-1);
				for (int i=0;i<str.Count;i++)
				{
					short sc = (short)str[i];
					stack.stack.Push(sc);
				}
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
