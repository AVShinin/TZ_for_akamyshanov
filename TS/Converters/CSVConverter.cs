using TS.Interfaces;
using TS.Structs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class CSVConverter : ToFileConverter
{
    public byte[] Result { get; protected set; }
    public void Convert(TradeRecord records)
    {
        string csv_text = $"{records.id};{records.account};{records.volume};{records.comment}\n";
        Result = Encoding.UTF8.GetBytes(csv_text);
    }

    public void Save(Stream stream)
    {
        stream.Write(Result, 0, Result.Length);
    }

    ~CSVConverter()
    {
        if (Result != null) Result = null;
    }
}
