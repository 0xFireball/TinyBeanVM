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
	
	/// <summary>
	/// Description of TinyBeanVM.
	/// </summary>
	public class TinyBeanVM
	{
		SystemRegisters registers;
		SystemMemory memory;
		SystemStack stack;
		BinaryReader bCode;
		Dictionary<short,int> labels;
		int cep = -1; //current excecution point
		public TinyBeanVM()
		{
			registers = new SystemRegisters(); //Registers
			memory = new SystemMemory(4096); //4k ram (2048 16-bit values)
			stack = new SystemStack(256); //stack of size 256
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
			#if DEBUG
			Parse(bc);
			#else
			try
			{
				Parse(bc);
			}
			catch (Exception ex)
			{
				throw new TinyBeanVMException(String.Format("TinyBeanVM Excecution Exception: {0}",ex.ToString()));
			}
			#endif
		}
		private void Parse(short[] bc)
		{
			cep = 0; //reset Current Execution Point
			labels = new Dictionary<short,int>();
			
			//Pass 1 - flow analysis
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
					//Console.WriteLine("Adding Label: ID: {0}, CEP: {1}",lid, cep);
					labels.Add(lid, cep); //add to list of labels.
					cep+=6;
				}
			}
			//Pass 1 complete
			
			cep = 0; //reset Current Execution Point
			cep = labels[0];
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
		}
		private void SystemLimits()
		{
			if (stack.stack.Count > stack.MaxSize)
			{
				throw new TinyBeanVMException("StackOverflowException - The stack has overflowed.");
			}
		}
		private void ExecuteLine(short[] n1, short[] n2, short[] n3)
		{
			string cmdlit = ASMParse.opc2s(n1); //OPCode to string
			//Console.WriteLine("Address: {0} - {1}", cep, cmdlit);
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
			if (  n1.SequenceEqual(ASMParse.s2opc("jmp")) )
			{
				int by_r_type2 = ASMParse.by_r_type(n2);
				if (by_r_type2==4)
				{
					short lbid = ASMParse.lit2sh(n2);
					cep = labels[lbid];
					//Console.WriteLine("Jump target: {0} Jump Address: {0}", lbid, cep);
				}
				else
				{
					throw new TinyBeanVMInvalidOperationException("Cannot jump to invalid location. [jmp]");
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
			if ( n1.SequenceEqual(ASMParse.s2opc("dmpmem")) ) //dmpreg
			{
				Console.WriteLine("DMPMEM");
				for (int i=0;i<memory.memory.Length;i++)
				{
					Console.Write(" ${0}:{1} ", i, memory.memory[i]);
					if (i%4==0)
					{
						Console.WriteLine();
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
				//print - print characters from the stack
				List<char> str = new List<char>(); //string
				while (stack.stack.Count > 0)
				{
					short nc = stack.stack.Pop(); //read char from stack
					if (nc==-1)
					{
						break;
					}
					char c = (char)nc;
					str.Add(c);
				}
				Console.Write(str.ToArray()); //print char array to console
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
