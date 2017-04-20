using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using TS.Structs;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;

namespace TS
{
    [TestClass]
    public class UnitTest1
    {
        public delegate void StatusThread(string keyThread, string msg);
        public static List<Task> taskPool = new List<Task>();

        [TestMethod]
        public void TestMethod1()
        {
            // START THREAD
            var thread = new Thread(new ThreadStart(() => Task.WhenAll(taskPool)));
            thread.Start();

            // STATUS DELEGATE
            StatusThread status = (k, m) =>
            {
                System.Diagnostics.Debug.WriteLine($"Thread {k}; status message={m}");
                Console.WriteLine($"Thread {k}; status message={m}");
            };

            // PARAMS
            string output_file = Path.Combine(Directory.GetCurrentDirectory(), "converted");
            string[] formats = new[] { "CSV", "TXT" };

            int count = 2;      //count gen files
            int lines = 500;   //count gen lines

            // WORK
            for (int i = count; i >= 1; i--)
            {
                var fileName = CreateBinFile(Path.Combine(Directory.GetCurrentDirectory(), "binary", $"temp_{i}.bin"), lines * i).Result;
                taskPool.Add(WorkMethod(fileName, output_file, formats, status));
                Thread.Sleep(500);
            }


            //Thread.Sleep(50000);
            //thread.Abort();
            while (true) ;
        }

        private async Task<string> CreateBinFile(string fileName, int iteration)
        {
#if DEBUG
            // CREATE BIN FILE
            var fInf = new FileInfo(fileName);
            if (!fInf.Directory.Exists) fInf.Directory.Create();
            if (!fInf.Exists)
                using (var fs = new FileStream(fInf.FullName, FileMode.Create))
                    await Task.Run(() =>
                    {
                        for (int i = 0; i <= iteration; i++)
                        {
                            var buff = Helpers.StructSerializer.RawSerialize(new TradeRecord() { id = i, account = i * 123, volume = 99.99, comment = $"It's record #{i}!" });
                            fs.Write(buff, 0, buff.Length);
                            fs.Flush(true);
                            Console.WriteLine("TradeRecord flush!");
                        }
                    });
            return fInf.FullName;
#else
            return fileName;
#endif
        }

        /// <summary>
        /// Загружет и конвертирует бинарные файлы в пользовательские форматы.
        /// Результат сохраняется в папке.
        /// </summary>
        /// <param name="input_fileName">Бинарный файл</param>
        /// <param name="output_Path">Папка в которую сохраняются сконвертированные файлы</param>
        /// <param name="formats">Массив выходных форматов</param>
        /// <param name="status">Обработчик сообщений</param>
        /// <returns></returns>
        private Task WorkMethod(string input_fileName, string output_Path, string[] formats, StatusThread status = null)
        {
            return Task.Run(() =>
            {
                if (!Directory.Exists(output_Path)) Directory.CreateDirectory(output_Path);
                var fInf = new FileInfo(input_fileName);
                string outFileName = fInf.Name.Replace(fInf.Extension, string.Empty);

                // READ BIN FILE
                List<TradeRecord> records = new List<TradeRecord>();
                if (File.Exists(input_fileName))
                {
                    // CREATE ARRAY WRITE STREAMS
                    Dictionary<string, TEMPSt> help = new Dictionary<string, TEMPSt>();
                    foreach (var f in formats)
                    {
                        try
                        {
                            status?.Invoke(fInf.Name, $"Initialize {f}Converter...");
                            var inst = Activator.CreateInstance(Assembly.GetExecutingAssembly().FullName, $"{f}Converter");
                            var converter = inst.Unwrap() as Interfaces.ToFileConverter;

                            help.Add(f,
                            new TEMPSt(
                                new FileStream(Path.Combine(output_Path, $"{outFileName}.{f}"), FileMode.Create),
                                converter));
                        }
                        catch (Exception)
                        {
                            status?.Invoke(fInf.Name, $"Error! No exists class {f}Converter!");
                        }

                    }

                    // READING
                    using (var fs = new FileStream(input_fileName, FileMode.Open))
                    {
                        status?.Invoke(fInf.Name, "Start reading...");
                        byte[] buff = new byte[Marshal.SizeOf(typeof(TradeRecord))];
                        int cnt = 0;
                        while (fs.Read(buff, 0, buff.Length) > 0)
                        {
                            var record = Helpers.StructSerializer.DeserializeStruct<TradeRecord>(buff);

                            // CONVERT TO FORMATS
                            foreach (var f in formats)
                            {
                                if (!help.ContainsKey(f)) break;
                                help[f].converter.Convert(record);
                                help[f].converter.Save(help[f].stream);
                                help[f].stream.Flush(true);
                            }
                            double percent = ((double)fs.Position / (double)fs.Length) * 100;
                            status?.Invoke(fInf.Name, $"Read record={cnt++}; complete={percent:0.00}%");
                        }
                    }

                    // CLEAR
                    foreach (var h in help) h.Value.stream.Close();
                    help.Clear();

                    status?.Invoke(fInf.Name, $"Complete!");
                }
            });
        }
        private struct TEMPSt
        {
            public FileStream stream { get; set; }
            public Interfaces.ToFileConverter converter;
            public TEMPSt(FileStream stream, Interfaces.ToFileConverter converter)
            {
                this.stream = stream;
                this.converter = converter;
            }
        }
    }
}
