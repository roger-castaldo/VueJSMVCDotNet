using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutomatedTesting.Models
{
    [ModelRoute("/models/mDataTypes")]
    [ModelJSFilePath("/resources/scripts/mDataTypes.js", modelNamespace: "App.Models")]
    internal class mDataTypes : IModel
    {
        public string id
        {
            get { return new Guid().ToString(); }
        }

        private string _stringField = "Testing123";
        [NotNullProperty()]
        public string StringField { get { return _stringField; } set { _stringField = value; } }
        private string _nullStringField = null;
        public string NullStringField { get { return _nullStringField; } set { _nullStringField = value; } }

        private char _charField = 'A';
        public char CharField { get { return _charField; } set { _charField = value; } }
        private char? _nullCharField = null;
        public char? NullCharField { get { return _nullCharField; } set { _nullCharField = value; } }
        private short _shortField = 10;
        public short ShortField { get { return _shortField; } set { _shortField=value; } }
        private short? _nullShortField = null;
        public short? NullShortField { get { return _nullShortField; } set { _nullShortField=value; } }
        private ushort _ushortField = 10;
        public ushort UShortField { get { return _ushortField; } set { _ushortField = value; } }
        private ushort? _nullUShortField = null;
        public ushort? NullUShortField { get { return _nullUShortField; } set { _nullUShortField = value; } }
        private int _intField = 10;
        public int IntField { get { return _intField; } set { _intField = value; } }
        private int? _nullIntField = null;
        public int? NullIntField { get { return _nullIntField; } set { _nullIntField = value; } }
        private uint _uintField = 10;
        public uint UIntField { get { return _uintField; } set { _uintField = value; } }
        private uint? _nullUIntField = null;
        public uint? NullUIntField { get { return _nullUIntField; } set { _nullUIntField = value; } }
        private long _longField = 10;
        public long LongField { get { return _longField; } set { _longField = value; } }
        private long? _nullLongField = null;
        public long? NullLongField { get { return _nullLongField; } set { _nullLongField = value; } }
        private ulong _ulongField = 10;
        public ulong ULongField { get { return _ulongField; } set { _ulongField = value; } }
        private ulong? _nullULongField = null;
        public ulong? NullULongField { get { return _nullULongField; } set { _nullULongField = value; } }
        private float _floatField = 10;
        public float FloatField { get { return _floatField; } set { _floatField = value; } }
        private float? _nullFloatField = null;
        public float? NullFloatField { get { return _nullFloatField; } set { _nullFloatField = value; } }
        private decimal _decimalField = 10;
        public decimal DecimalField { get { return _decimalField; } set { _decimalField = value; } }
        private decimal? _nullDecimalField = null;
        public decimal? NullDecimalField { get { return _nullDecimalField; } set { _nullDecimalField = value; } }
        private double _doubleField = 10;
        public double DoubleField { get { return _doubleField; } set { _doubleField = value; } }
        private double? _nullDoubleField = null;
        public double? NullDoubleField { get { return _nullDoubleField; } set { _nullDoubleField = value; } }
        private byte _byteField = 10;
        public byte ByteField { get { return _byteField; } set { _byteField = value; } }
        private byte? _nullByteField = null;
        public byte? NullByteField { get { return _nullByteField; } set { _nullByteField = value; } }
        private bool _boolField = false;
        public bool BooleanField { get { return _boolField; } set { _boolField = value; } }
        private bool? _nullBooleanField = null;
        public bool? NullBooleanField { get { return _nullBooleanField; } set { _nullBooleanField = value; } }

        public enum TestEnums
        {
            Test1,
            Test2,
            Test3
        }

        private TestEnums _enumField = TestEnums.Test1;
        public TestEnums EnumField { get { return _enumField; } set { _enumField = value; } }
        private TestEnums? _nullEnumField = null;
        public TestEnums? NullEnumField { get { return _nullEnumField; } set { _nullEnumField = value; } }

        private DateTime _DateTimeField = DateTime.Now;
        public DateTime DateTimeField { get { return _DateTimeField; } set { _DateTimeField = value; } }
        private DateTime? _nullDateTimeField = null;
        public DateTime? NullDateTimeField { get { return _nullDateTimeField; } set { _nullDateTimeField = value; } }

        private byte[] _byteArrayField = System.Text.ASCIIEncoding.ASCII.GetBytes("Testing123");
        [NotNullProperty()]
        public byte[] ByteArrayField { get { return _byteArrayField; } set { _byteArrayField = value; } }
        private byte[] _nullByteArrayField = null;
        public byte[] NullByteArrayField { get { return _nullByteArrayField; } set { _nullByteArrayField = value; } }

        [ModelLoadMethod()]
        public static mDataTypes Load(string id)
        {
            return null;
        }


    }

}
