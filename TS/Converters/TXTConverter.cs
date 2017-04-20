using TS.Interfaces;
using TS.Structs;
using System.Collections.Generic;
using System.Text;
using System;
using System.IO;

public class TXTConverter : ToFileConverter
{
    public byte[] Result { get; protected set; }

    public void Convert(TradeRecord records)
    {
        string txt_text = $"<Trade{records.id}><id>{records.id}</id><account>{records.account}</account><volume>{records.volume}</volume><comment>{records.comment}</comment></Trade>\n";
        Result = Encoding.UTF8.GetBytes(txt_text);
    }
    public void Save(Stream stream)
    {
        stream.Write(Result, 0, Result.Length);
    }


    ~TXTConverter()
    {
        if (Result != null) Result = null;
    }
}
