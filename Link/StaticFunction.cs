using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Link
{
    public class StaticFunction
    {
        public static int PackLength = 56;
		public static int PackId = 0;
		public static int Index = 0;
		private static bool isFinished = false;
		public static List<BitArray> Result = new List<BitArray>();
		public static bool[][] Data;
		public static Encoding Encoding = Encoding.UTF8;
		public static bool isFile = false;
		private static string FileExtension;
		private static Random Random = new Random();
		public static int RR = 1; //готовность к приему
		public static int RNR = 2; // неготовность к приему
		public static int REJ = 3; // отказ
		public static int RD = 4; // запрос разъединения
		public static int DISC = 5; //разъединить соединение
		public static int RIM = 6; // запрос инициализации
		public static int SIM = 7; // установить режим инициализации
		public static int UP = 8; // запрос передачи (ненумерованный)
		public static int UA = 9; //подтверждение (ненумерованное)

		public static void AddData(int? repeatIndex, BitArray data)
		{
			var LockObject = new object();
			lock (LockObject)
			{
				try
				{
					if (repeatIndex == null)
						Result.Add(data);
					else
						Result.Insert((int)repeatIndex, data);
				}
				catch (Exception) { }
			}
		}

		public static void SerializeFile(string file)
		{
			BitArray bits;
			isFile = true;
			FileExtension = Path.GetExtension(file);
			using (FileStream fs = File.OpenRead(file))
			{
				var binaryReader = new BinaryReader(fs);
				bits = new BitArray(binaryReader.ReadBytes((int)fs.Length));
			}
			var values = new bool[bits.Count];
			for (int m = 0; m < bits.Count; m++)
				values[m] = bits[m];
			int j = 0;
			Data = values.GroupBy(s => j++ / PackLength).Select(g => g.ToArray()).ToArray();
		}
		public static void DeserializeFile(string tag)
		{
			var LockObject = new object();
			lock (LockObject)
				if (!isFinished)
				{
					isFinished = true;
					var booleans = new List<bool>();

					for (int i = 0; i < Result.Count; i++)
						for (int j = 0; j < Result[i].Length; j++)
							booleans.Add(Result[i][j]);

					var byteArray = BitArrayToByteArray(new BitArray(booleans.ToArray()));

					try
					{
						using var fs = new FileStream(tag + FileExtension, FileMode.Create, FileAccess.Write);
						fs.Write(byteArray, 0, byteArray.Length);
						ConsoleHelper.WriteToConsole(tag + FileExtension, $"Файл успешно создан..");
					}
					catch
					{
						ConsoleHelper.WriteToConsole(tag + FileExtension, $"Что-то пошло не так...");
					}
				}
		}
		public static int GetIdPack => PackId;
		public static void IncrementIndex()
		{
			var LockObject = new object();
			lock (LockObject)
				Index++;
		}
		public static void DecrementIndex()
		{
			var LockObject = new object();
			lock (LockObject)
				Index--;
		}
		public static int IndexPack()
		{
			if (PackId == 7)
            {
				PackId = 0;
			}
            else
            {
				PackId++;
			}
			return PackId;
		}
		public static BitArray SetNoiseRandom(BitArray data)
		{
			if (Random.Next(1, 100) < 10)
				for (int i = 0; i < data.Length; i++)
					if (i % Random.Next(1, 5) == 0)
						data[i] = Random.Next(1, 10) < 5;

			return data;
		}
		public static byte[] BitArrayToByteArray(BitArray data)
		{
			if (data != null)
			{
				byte[] array = new byte[(data.Length - 1) / 8 + 1];
				data.CopyTo(array, 0);
				return array;
			}
            else
            {
				return null;
            }
		}

		public static object DeserializeObject(byte[] allBytes)
		{
			if (allBytes != null)
			{
				using (var stream = new MemoryStream(allBytes))
				{
					return DeserializeFromStream(stream);
				}
			}
            else
            {
				return null;
            }
		}

		public static void DeserializeMessage(string tag)
		{
			var LockObject = new object();
			lock (LockObject)
				if (!isFinished)
				{
					isFinished = true;

					var booleans = new List<bool>();

					for (int i = 0; i < Result.Count; i++)
						for (int j = 0; j < Result[i].Length; j++)
							booleans.Add(Result[i][j]);

					ConsoleHelper.WriteToConsole(tag, $"Полученные данные: {Encoding.GetString(BitArrayToByteArray(new BitArray(booleans.ToArray())))}");
				}
		}

		private static object DeserializeFromStream(MemoryStream stream)
        {
			try
			{
				IFormatter formatter = new BinaryFormatter();
				stream.Seek(0, SeekOrigin.Begin);
				return formatter.Deserialize(stream);
			}
			catch
			{
				return null;
			}
		}

		public static byte[] SerializeObject(object obj)
		{
			BinaryFormatter bf = new BinaryFormatter();
			using (var ms = new MemoryStream())
			{
				bf.Serialize(ms, obj);
				return ms.ToArray();
			}
		}
	}
}
