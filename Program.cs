using System;
using System.IO;

namespace BinarySerializationAnalysis
{
	static class Program
	{
		static void Main(string[] args)
		{
			var analyzer = new BinarySerializationStreamAnalyzer();
			using(var stream = File.Open(args[0], FileMode.Open, FileAccess.Read, FileShare.Read))
				analyzer.Read(stream);
			Console.WriteLine(analyzer.Analyze());
		}
	}
}
