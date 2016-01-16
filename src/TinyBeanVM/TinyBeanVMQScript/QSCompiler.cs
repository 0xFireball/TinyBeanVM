/*

 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TinyBeanVMQScript
{
	/// <summary>
	/// Description of QSCompiler.
	/// </summary>
	public class QSCompiler
	{
		public QSCompiler()
		{
			
		}
		public string[] CompileFile(string[] code)
		{
			List<string> ret = new List<string>();
			ret = Parse(code);
			return ret.ToArray();
		}
		public List<string> Parse(string[] c)
		{
			List<string> ret = new List<string>();
			foreach (string l in c)
			{
				ret.Add(ParseLine(l));
			}
			while (ret.Contains(" \r\n"))
				ret.Remove(" \r\n");
			return ret;
		}
		public string ParseLine(string l)
		{
			string ret = "";
			string[] bcx = reverseStringFormat("{0} \"{1}\"",l);
			if (bcx[0] == "println")
			{
				char[] op = bcx[1].ToCharArray();
				List<char> mp = op.ToList();
				mp.Reverse();
				ret+=" push -1,\r\n";
				for (int i=0;i<mp.Count;i++)
				{
					ret+=String.Format(" push {0},\r\n",(short)mp[i]);
				}
				ret+=" bcall *1,\r\n";
			}
			return ret;
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
