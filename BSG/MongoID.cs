using Newtonsoft.Json;

namespace SIT.WebServer.BSG
{
    [Serializable]
    public readonly struct MongoID : IComparable<MongoID>, IEquatable<MongoID>
    {
        private static Random _random = new Random();

        private static readonly ulong _processId = UInt64_0;

        private static uint _newIdCounter;

        private readonly ulong _timeStamp;

        private readonly ulong _counter;

        private readonly string _stringID;

        public static string Default = "ffffffffffffffffffffffff";

        public static ulong TimeStamp => (ulong)DateTime.UtcNow.Ticks;

        private static ulong UInt64_0 => (ulong)((long)_random.Next(0, int.MaxValue) << 8 ^ _random.Next(0, int.MaxValue));

        public static string Generate()
        {
            return smethod_0((ulong)TimeStamp, UInt64_0 << 24);
        }

        private static string smethod_0(ulong timeStamp, ulong counter)
        {
            return timeStamp.ToString("X8").ToLower() + counter.ToString("X16").ToLower();
        }

        public static uint ConvertTimeStamp(string id)
        {
            return Convert.ToUInt32(id.Substring(0, 8), 16);
        }

        public static ulong ConvertCounter(string id)
        {
            return Convert.ToUInt64(id.Substring(8, 16), 16);
        }

        private MongoID(BinaryReader reader)
        {
            _timeStamp = reader.ReadUInt32();
            _counter = reader.ReadUInt64();
            _stringID = null;
            _stringID = method_1();
            method_0();
        }

        public MongoID(bool newProcessId)
        {
            _timeStamp = 0u;
            _stringID = null;
            if (newProcessId)
            {
                _counter = UInt64_0 << 24;
            }
            else
            {
                _newIdCounter++;
                _counter = (_processId << 24) + _newIdCounter;
            }
            _timeStamp = TimeStamp;
            _stringID = method_1();
        }

        public MongoID(string accountId)
        {
            _stringID = null;
            _timeStamp = TimeStamp;
            uint num = Convert.ToUInt32(accountId);
            uint num2 = Convert.ToUInt32(_random.Next(0, 16777215));
            _counter = 0x100000000uL | num;
            _counter <<= 24;
            _counter |= num2;
            _stringID = method_1();
        }

        private MongoID(MongoID source, int increment, bool newTimestamp)
        {
            _timeStamp = newTimestamp ? TimeStamp : source._timeStamp;
            _counter = increment > 0 ? source._counter + Convert.ToUInt32(increment) : source._counter - Convert.ToUInt32(Math.Abs(increment));
            _stringID = null;
            _stringID = method_1();
        }

        private void method_0()
        {
            ulong num = Convert.ToUInt64(_counter >> 24);
            if (_processId == num)
            {
                uint val = Convert.ToUInt32(_counter << 40 >> 40);
                _newIdCounter = Math.Max(_newIdCounter, val);
            }
        }

        public MongoID Next()
        {
            return new MongoID(this, 1, newTimestamp: true);
        }

        public static MongoID Read(BinaryReader reader)
        {
            return new MongoID(reader);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(_timeStamp);
            writer.Write(_counter);
        }

        public bool Equals(MongoID other)
        {
            if (_timeStamp == other._timeStamp)
            {
                return _counter == other._counter;
            }
            return false;
        }

        public int CompareTo(MongoID other)
        {
            if (this == other)
            {
                return 0;
            }
            if (this > other)
            {
                return 1;
            }
            return -1;
        }

        private string method_1()
        {
            return smethod_0(_timeStamp, _counter);
        }

        public override string ToString()
        {
            return _stringID.Substring(0, 24) ?? string.Empty;
        }

        public byte[] ToBytes()
        {
            byte[] array = new byte[12];
            for (int i = 0; i < 4; i++)
            {
                array[i] = (byte)(_timeStamp >> (3 - i) * 8);
            }
            for (int j = 0; j < 8; j++)
            {
                array[j + 4] = (byte)(_counter >> (7 - j) * 8);
            }
            return array;
        }

        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                if (obj is MongoID mongoID)
                {
                    return mongoID == this;
                }
                if (obj is string text)
                {
                    return text == ToString();
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            uint num = Convert.ToUInt32(_counter >> 32) * 3637;
            uint num2 = Convert.ToUInt32(_counter << 32 >> 32) * 5807;
            return (int)(_timeStamp ^ num ^ num2);
        }

        public static implicit operator string(MongoID mongoId)
        {
            return mongoId.ToString();
        }

        public static implicit operator MongoID(string id)
        {
            return new MongoID(id);
        }

        public static MongoID operator ++(MongoID id)
        {
            return new MongoID(id, 1, newTimestamp: false);
        }

        public static MongoID operator --(MongoID id)
        {
            return new MongoID(id, -1, newTimestamp: false);
        }

        public static bool operator ==(MongoID a, MongoID b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(MongoID a, MongoID b)
        {
            return !a.Equals(b);
        }

        public static bool operator >(MongoID a, MongoID b)
        {
            if (a._timeStamp <= b._timeStamp)
            {
                if (a._timeStamp == b._timeStamp)
                {
                    return a._counter > b._counter;
                }
                return false;
            }
            return true;
        }

        public static bool operator <(MongoID a, MongoID b)
        {
            if (a._timeStamp >= b._timeStamp)
            {
                if (a._timeStamp == b._timeStamp)
                {
                    return a._counter < b._counter;
                }
                return false;
            }
            return true;
        }

        public static bool operator >=(MongoID a, MongoID b)
        {
            if (!(a == b))
            {
                return a > b;
            }
            return true;
        }

        public static bool operator <=(MongoID a, MongoID b)
        {
            if (!(a == b))
            {
                return a < b;
            }
            return true;
        }
    }

}
