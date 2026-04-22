using System.Linq;
using System.Text;

namespace Utility.Extensions
{
    public static class StringExtensions
    {
        public static string RemoveCharacters(this string str, params char[] c)
        {
            var sb = new StringBuilder();

            foreach (var current in str)
            {
                if (c.Contains(current))
                    continue;
                sb.Append(current);
            }

            return sb.ToString();
        }
    }
}
