using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parsers
{
    public abstract partial class Item : ICsvFormat
    {
        protected static TType[] CreateInstances<TType>(int count)
            where TType : class, new()
        {
            TType[] array = new TType[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = new TType();
            }
            return array;
        }

        public int Version; // 11C
        public int Unknown080 { get; set; }
        public string Unknown048 { get; set; }
        public string Unknown068 { get; set; }
        public string Unknown008 { get; set; }
        public string Unknown004 { get; set; }
        public int Unknown084 { get; set; }
        public int Unknown090 { get; set; }
        public int Unknown088 { get; set; }
        public short Unknown0A4 { get; set; }
        public short Unknown0A0 { get; set; }
        public short Unknown0A2 { get; set; }
        public short Unknown0A6 { get; set; }
        public short Unknown0A8 { get; set; }
        public int Unknown08C { get; set; }
        public int Unknown094 { get; set; }
        public int Unknown098 { get; set; }
        public int Unknown09C { get; set; }
        public short Unknown0AA { get; set; }
        public short Unknown0AC { get; set; }
        public short Unknown0B0 { get; set; }
        public short Unknown0AE { get; set; }
        public BlobSprite Unknown0B4 { get; set; }

        public Item()
        {
            this.Unknown08C = -1;
            this.Unknown098 = 44800;
            this.Unknown09C = 1;
            this.Unknown0B4 = new BlobSprite();
        }

        public virtual void Read(ICsvReader reader)
        {
            this.Version = reader.GetInt('v');
            this.Unknown080 = reader.GetInt();
            this.Unknown048 = reader.GetString();
            this.Unknown068 = reader.GetString();
            this.Unknown008 = reader.GetString();
            this.Unknown004 = reader.GetString();
            this.Unknown084 = reader.GetInt();
            this.Unknown090 = reader.GetInt();
            this.Unknown088 = reader.GetInt();
            this.Unknown0A4 = reader.GetShort();

            if (this.Version >= 8)
            {
                this.Unknown0A0 = reader.GetShort();
            }
            else
            {
                reader.Skip();
            }

            this.Unknown0A2 = reader.GetShort();
            this.Unknown0A6 = (this.Version >= 1) ? reader.GetShort() : (short)0;

            if (this.Version >= 5 && this.Version < 34)
            {
                reader.Skip();
            }

            if (this.Version >= 12)
            {
                this.Unknown0A8 = reader.GetShort();
            }
            else
            {
                reader.Skip();
            }

            if (this.Version >= 13)
            {
                this.Unknown08C = reader.GetInt();
            }
            else
            {
                reader.Skip();
            }

            this.Unknown094 = (this.Version >= 29) ? reader.GetInt() : 0;
            this.Unknown098 = (this.Version >= 35) ? reader.GetInt() : 44800;
            this.Unknown09C = (this.Version >= 47) ? reader.GetInt() : 1;
            this.Unknown0AA = (this.Version >= 51) ? reader.GetShort() : (short)0;
            this.Unknown0AC = (this.Version >= 52) ? reader.GetShort() : (short)0;
            this.Unknown0B0 = (this.Version >= 55) ? reader.GetShort() : (short)0;
            this.Unknown0AE = (this.Version >= 56) ? reader.GetShort() : (short)0;

            if (this.Version >= 33)
            {
                this.Unknown0B4.ReadV3(reader);
            }
            else if (this.Version >= 17)
            {
                this.Unknown0B4.ReadV2(reader);
            }
            else
            {
                this.Unknown0B4.ReadV1(reader);
            }

            if (this.Version < 15)
            {
                // this should probably be some form of parser.Skip() but fuck it~
                new BlobSprite().ReadV1(reader);
            }

            if (this.Version < 3)
            {
                this.Unknown0B4.FixBlobId();
            }
        }
    }
}
