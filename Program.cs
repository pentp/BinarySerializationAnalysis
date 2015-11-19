using System;
using System.Collections.Generic;
using System.IO;
using System.Web;

namespace BinarySerializationAnalysis
{
	static class Program
	{
		static void Main(string[] args)
		{
			var analyzer = new BinarySerializationStreamAnalyzer();
			if(args.Length > 1 && ("/s".Equals(args[1], StringComparison.OrdinalIgnoreCase) || "/SessionState".Equals(args[1], StringComparison.OrdinalIgnoreCase)))
			{
				StateItemData sessionItems;
				HttpStaticObjectsCollection staticObjects;
				using(var stream = File.OpenRead(args[0]))
					Deserialize(stream, out sessionItems, out staticObjects);
				if(sessionItems != null)
				{
					Console.WriteLine("SessionItems count: " + sessionItems.Count);
					var buf = sessionItems.Buffer;
					foreach(var k in sessionItems.Keys)
					{
						var si = sessionItems[k];
						Console.WriteLine(k + ": " + (si.Length - 1));
						if(buf[si.Offset] == 20) analyzer.Read(new MemoryStream(buf, si.Offset + 1, si.Length - 1));
					}
				}
				if(staticObjects != null)
				{
					Console.WriteLine("StaticObjects: " + staticObjects.Count);
#warning TODO: display stats
				}
			}
			else
			{
				using(var stream = File.OpenRead(args[0]))
					analyzer.Read(stream);
			}
			Console.WriteLine();
			Console.WriteLine("Analyzer stats:");
			Console.WriteLine(analyzer.Analyze());
		}

		static void Deserialize(Stream stream, out StateItemData sessionItems, out HttpStaticObjectsCollection staticObjects)
		{
			var reader = new BinaryReader(stream);
			var timeout = reader.ReadInt32();
			var hasItems = reader.ReadBoolean();
			var hasStaticObjects = reader.ReadBoolean();
			sessionItems = hasItems ? new StateItemData(reader) : null;
			staticObjects = hasStaticObjects ? HttpStaticObjectsCollection.Deserialize(reader) : null;
			var eof = reader.ReadByte();
			if(eof != 0xff) throw new InvalidOperationException("Invalid session state");
		}

		sealed class StateItem
		{
			public int Offset, Length;
		}

		sealed class StateItemData:Dictionary<string, StateItem>
		{
			public new readonly string[] Keys;
			public readonly byte[] Buffer;

			public StateItemData(BinaryReader reader)
			{
				int count = reader.ReadInt32();
				if(count <= 0) return;
				int num = reader.ReadInt32();
				Keys = new string[count];
				for(int i = 0;i < count;i++)
					Keys[i] = i == num ? null : reader.ReadString();
				var offset = 0;
				for(int i = 0;i < count;i++)
				{
					var o = reader.ReadInt32();
					base[Keys[i]] = new StateItem { Offset = offset, Length = o - offset };
					offset = o;
				}
				Buffer = new byte[offset];
				if(reader.BaseStream.Read(Buffer, 0, offset) != offset) throw new InvalidOperationException("Invalid session state");
			}
		}
	}
}
