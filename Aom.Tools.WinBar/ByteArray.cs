using System;
using System.Linq;
using System.Text;

namespace Aom.Tools.WinBar
{
    public class ByteArray : IEquatable<ByteArray>
    {
        public byte[] ByteData { get; }

        private ByteArray(byte[] data)
        {
            ByteData = data;
        }

        public static ByteArray FromBytes(params byte[] bytes) => new ByteArray(bytes);
        public static ByteArray FromInt32(int x) => new ByteArray(BitConverter.GetBytes(x));
        public static ByteArray FromString(string x) => new ByteArray(new ASCIIEncoding().GetBytes(x));
        public static ByteArray FromDateTime(DateTime dt)
        {
            var data = new byte[8];
            var year = BitConverter.GetBytes((short)dt.Year);
            data[0] = year[0];
            data[1] = year[1];
            data[2] = (byte)dt.Month;
            data[3] = (byte)dt.Day;
            data[4] = (byte)dt.Hour;
            data[5] = (byte)dt.Minute;
            data[6] = (byte)dt.Second;
            data[7] = (byte)dt.Millisecond;

            return new ByteArray(data);
        }

        public int ToInt32() => BitConverter.ToInt32(ByteData, 0);
        private short ToInt16() => BitConverter.ToInt16(ByteData, 0);
        public string ToAsciiString() => new ASCIIEncoding().GetString(ByteData).Trim((char)0);

        public DateTime ToDateTime()
        {
            var year = ExtractSegment(0, 2).ToInt16();
            var month = ByteData[2];
            var day = ByteData[3];
            var hour = ByteData[4];
            var minute = ByteData[5];
            var second = ByteData[6];
            var milisecond = ByteData[7];
            return new DateTime(year, month, day, hour, minute, second, milisecond);
        }

        public ByteArray ExtractSegment(int offset, int length)
        {
            var retVal = new byte[length];
            for (var i = 0; i < length; i++)
                retVal[i] = ByteData[offset + i];
            return new ByteArray(retVal);
        }

        public override string ToString() => string.Join(" ", ByteData.Select(b => b.ToString("x2").ToUpperInvariant()).ToArray());

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ByteArray) obj);
        }

        public override int GetHashCode() => ByteData?.GetHashCode() ?? 0;

        public bool Equals(ByteArray other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(ByteData, other.ByteData);
        }
    }
}