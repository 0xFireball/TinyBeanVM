/*

 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace TinyBeanVMAssemblerCLI.Parsing
{
	/// <summary>
	/// Description of ASMParse.
	/// </summary>
	public class ASMParse
	{
		static short lblnum = 0;
		static Dictionary<string, short[]> opcodes = new Bictionary<string, short[]>()
		{
			{ "lbl:", new short[]{ 0x005f, 0x0000} },
			{ "nop", new short[]{ 0x0000, 0x000f} },
			{ "dmpreg", new short[]{0x0000, 0x1100} }, //dump registers (debugging)
			{ "dmpmem", new short[]{0x0000, 0x1101} }, //dump memory (debugging)
			{ "lda", new short[]{0x0000, 0x1110} }, //load into A register
			{ "mov", new short[]{0x0000, 0x1111} }, //mov value to location
			{ "push", new short[]{0x0000, 0x1112} }, //push value
			{ "pop", new short[]{0x0000, 0x1113} }, //pop value
			{ "bcall", new short[]{0x0000, 0x1114} }, //call built-in code
			{ "jmp", new short[]{0x0000, 0x1115} }, //jump to label
			{ "add", new short[]{0x0000, 0x1116} }, //add to register
			{ "sub", new short[]{0x0000, 0x1117} }, //subtract from register
			{ "jz", new short[]{0x0000, 0x1118} }, //jump if Z register = 0
			{ "jnz", new short[]{0x0000, 0x1119} }, //jump if Z register NOT = 0
			{ "cmp", new short[]{0x0000, 0x111a} }, //set Z to 0 if operands equal, otherwise set to sub
			{ "lma", new short[]{0x0000, 0x111b} }, //load A into location
			{ "exit", new short[]{0x0000, 0x111c} }, //exit with exit code
			{ ".mem", new short[]{0x0000, 0x111d} }, //assign memory
		};
		static Dictionary<short, int> RegisterIds = new Dictionary<short, int>()
		{
			{0x1000,1},
			{0x1001,2},
			{0x1002,3},
			{0x1003,4},
		};

		public static short[] lbl(short count)
		{
			return new short[] { opcodes["lbl:"][0], count, 0x0000, 0x0000, 0x0000, 0x0000}; //label, padding of 4
		}
		public static bool islbl(short[] by)
		{
			return by[0]==opcodes["lbl:"][0];
		}
		public static short lblid(short[] lbl)
		{
			return lbl[1];
		}
		public static short[] nop()
		{
			return opcodes["nop"]; //nop		
		}
		public static short[] s2opc(string s, out int error)
		{
			if (s==""||s==" ")
			{
				error = 0;
				return new short[0];
			}
			if (opcodes.ContainsKey(s))
			{
				error = 0;
				return opcodes[s];
			}
			else
			{
				error = -1;
				Console.WriteLine("Error: Instruction not found: {0}",s);
				return new short[0];
			}
			
		}
		public static string opc2s(short[] opc)
		{
			Dictionary<short[],string> rev_opc_l = opcodes.ToDictionary(kp => kp.Value, kp => kp.Key);
			Dictionary<short, string> reverse_opcodes = new Dictionary<short, string>();
			foreach (KeyValuePair<short[], string> kvp in rev_opc_l)
			{
				reverse_opcodes.Add(kvp.Key[1], kvp.Value);
			}
			return reverse_opcodes[opc[1]];
		}
		public static int by_r_type(short[] by)
		{
			if (by[0] == 0x0001) //register
			{
				return 1; //register
			}
			if (by[0] == 0x0002) //num literal
			{
				return 0; //numerical literal
			}
			if (by[0] == 0x0003) //memory address
			{
				return 2; //memory address
			}
			if (by[0] == 0x000f) //name
			{
				return 3; //name
			}
			if (by[0] == 0x0004) //name
			{
				return 4; //label
			}
			return -1; //unknown type
		}
		public static  short lit2sh(short[] lit) //literal to short
		{
			return lit[1];
		}
		public static int rlit2sh(short[] lit) //register literal to register id
		{
			return RegisterIds[lit[1]];
		}
		public static short[] r2by(string s)
		{
			switch (s)
			{
				//0x0001 - register
				//0x0002 - constant
				//REGISTERS:
				//0x1000 - A
				case "a":
					return new short[] {0x0001, 0x1000};
				case "b":
					return new short[] {0x0001, 0x1001};
				case "t":
					return new short[] {0x0001, 0x1002};
				case "x":
					return new short[] {0x0001, 0x1003};
				default:
					bool memloc = s.StartsWith("$",StringComparison.InvariantCulture); //$ to access RAM
					bool name = s.StartsWith("*",StringComparison.InvariantCulture);
					bool label = s.StartsWith(":",StringComparison.InvariantCulture);
					//bool data = s.StartsWith("''$",StringComparison.InvariantCulture);
					if (!memloc && !name && !label) //numerical literal
					{
						short d = 0x0000;
						bool x = short.TryParse(s, out d);
						return new short[]{ 0x0002, d};
					}
					else if (memloc) //memory location
					{
						string s1 = s.Substring(1);
						short d = 0x0000;
						bool x = short.TryParse(s1, out d);
						return new short[]{ 0x0003, d};
					}
					else if (name) //0x000f - name type
					{
						string s1 = s.Substring(1);
						short d = 0x0000;
						bool x = short.TryParse(s1, out d);
						return new short[]{ 0x000f, d};
					}
					else if (label) //0x0004 - label type
					{
						string s1 = s.Substring(1);
						short d = 0x0000;
						//d = lblnum;
						bool x = short.TryParse(s1, out d);
						return new short[]{ 0x0004, d};
					}
					return opcodes["nop"];
			}
		}
	}
	public class Bictionary<T1, T2> : Dictionary<T1, T2>
	{
	    public T1 this[T2 index]
	    {
	        get
	        {
	            if(!this.Any(x => x.Value.Equals(index)))
	               throw new System.Collections.Generic.KeyNotFoundException();
	            return this.First(x => x.Value.Equals(index)).Key;
	        }
	    }
	}
}
