using System;
namespace OpenWeb
{
	public static class StringExtensions
	{
		public static bool OICContains(this string inStr, string toMatch)
		{
			return inStr.IndexOf(toMatch, System.StringComparison.OrdinalIgnoreCase) > -1;
		}
		public static bool OICEquals(this string inStr, string toMatch)
		{
			return inStr.Equals(toMatch, System.StringComparison.OrdinalIgnoreCase);
		}
		public static bool OICStartsWith(this string inStr, string toMatch)
		{
			return inStr.StartsWith(toMatch, System.StringComparison.OrdinalIgnoreCase);
		}
		public static bool OICStartsWithAny(this string inStr, params string[] toMatch)
		{
			bool result;
			for (int i = 0; i < toMatch.Length; i++)
			{
				if (inStr.StartsWith(toMatch[i], System.StringComparison.OrdinalIgnoreCase))
				{
					result = true;
					return result;
				}
			}
			result = false;
			return result;
		}
		public static bool OICEndsWithAny(this string inStr, params string[] toMatch)
		{
			bool result;
			for (int i = 0; i < toMatch.Length; i++)
			{
				if (inStr.EndsWith(toMatch[i], System.StringComparison.OrdinalIgnoreCase))
				{
					result = true;
					return result;
				}
			}
			result = false;
			return result;
		}
		public static bool OICEndsWith(this string inStr, string toMatch)
		{
			return inStr.EndsWith(toMatch, System.StringComparison.OrdinalIgnoreCase);
		}
	}
}
