using System;
using System.Linq;

namespace Hook.Plugin
{
    internal class Range
    {
        public readonly decimal Start, End;
        public readonly bool IncludeStart, IncludeEnd;
        public Range(decimal start, decimal end, bool includeStart, bool includeEnd)
        {
            if (start.CompareTo(end) > 0)
            {
                throw new ArgumentException("start is bigger than end");
            }
            Start = start;
            End = end;
            IncludeStart = includeStart;
            IncludeEnd = includeEnd;
        }

        public bool Within(decimal value) => MatchStart(value) && MatchEnd(value);

        public bool MatchStart(decimal value)
        {
            var comparsion = value.CompareTo(Start);
            return comparsion > 0
                || (comparsion == 0 && IncludeStart);
        }

        public bool MatchEnd(decimal value)
        {
            var comparsion = value.CompareTo(End);
            return comparsion < 0
                || (comparsion == 0 && IncludeEnd);
        }

        public static Range Parse(string str)
        {
            var trim = str.Trim();
            bool iStart, iEnd;
            char start = trim[0], end = trim[trim.Length - 1];

            if (start == '(')
            {
                iStart = false;
            }
            else if (start == '[')
            {
                iStart = true;
            }
            else
            {
                throw new ArgumentException(string.Format("starting with {0}", start));
            }

            if (end == ')')
            {
                iEnd = false;
            }
            else if (end == ']')
            {
                iEnd = true;
            }
            else
            {
                throw new ArgumentException(string.Format("ending with {0}", end));
            }

            var nums = trim.Remove(0, 1).Remove(trim.Length - 2).Split(',').Select(x => x.Trim()).ToArray();
            if (nums.Length != 2)
            {
                throw new ArgumentException("between the brackets dosn't match *[0-9|.],*[0-9|.]");
            }

            var aS = decimal.TryParse(nums[0], out decimal a);
            var bS = decimal.TryParse(nums[1], out decimal b);

            if (!aS || !bS)
            {
                throw new ArgumentException("between the brackets aren't both decimals");
            }
            return new Range(a, b, iStart, iEnd);
        }

        public static Type[] SupportedTypes => new Type[] { typeof(long), typeof(double), typeof(int) };
        public const double Step = 0.1;
    }
}
