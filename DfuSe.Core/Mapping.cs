using System;
using System.Collections.Generic;
using System.Text;

namespace DfuSe.Core
{
    public class Mapping
    {
        public byte Alternate;
        public string Name;
        public int NumberOfSectors;
        public List<Sector> Sectors;

        public class Sector
        {
            public int StartAddress;
            public int AliasedAddress;
            public int SectorIndex;
            public int SectorSize;
            public byte SectorType;
            public bool UseForOperation;
        }
    }
}
