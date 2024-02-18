using System.Text;

namespace WebSite.Common
{
    public class TextProcessor
    {
        public static string wsend = "wsend";
        public static bool CheckEnd(StringBuilder sb)
        {
            var content = sb.ToString();
            //如果不包含或者完全包含，直接返回
            var intersectStr = GetLongestCommonSubstring(content, wsend);
            if (intersectStr.Length == 0)
                return true;
            else
            {
                if (intersectStr == wsend || content.IndexOf(intersectStr) + intersectStr.Length != content.Length)
                {
                    return true;
                }
                else
                    return false;
            }
        }
        static string GetLongestCommonSubstring(string str1, string str2)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
            {
                return "";
            }

            int[,] dp = new int[str1.Length + 1, str2.Length + 1];
            int maxLength = 0;
            int end = 0;

            for (int i = 1; i <= str1.Length; i++)
            {
                for (int j = 1; j <= str2.Length; j++)
                {
                    if (str1[i - 1] == str2[j - 1])
                    {
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                        if (dp[i, j] > maxLength)
                        {
                            maxLength = dp[i, j];
                            end = i;
                        }
                    }
                }
            }

            return str1.Substring(end - maxLength, maxLength);
        }
    }
}
