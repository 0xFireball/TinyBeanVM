/*

 */
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

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
	class SystemStack
	{
		public Stack<short> stack;
		public SystemStack(int maxSize)
		{
			MaxSize = maxSize;
			stack = new Stack<short>();
		}
		public int MaxSize;
	}
	class SystemMemory
	{
		public short[] memory;
		public SystemMemory(int size)
		{
			memory = new short[size/2];
			for (int i=0;i<memory.Length;i++)
			{
				memory[i]=0;
			}
		}
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
			A = 0; //1
			B = 0; //2
			T = 0; //3
			X = 0; //4
			Z = 0; //5
			C = 0; //6
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
				case 5:
					ret = Z;
					break;
				case 6:
					ret = C;
					break;
			}
			return ret;
		}
	}
}
