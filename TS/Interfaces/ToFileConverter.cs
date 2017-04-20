using System.Collections.Generic;
using System.IO;

namespace TS.Interfaces
{
    public interface ToFileConverter
    {
        byte[] Result { get; }
        void Convert(Structs.TradeRecord records);
        void Save(Stream stream);
    }
}
