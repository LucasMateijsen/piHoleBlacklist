using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BlocklistMaker
{
    public static class BlocklistHandler
    {
        public static List<Tuple<string, string>> GetBlocklists()
        {
            // TODO LM: Get from Registry
            var returnValue = new List<Tuple<string, string>>();

            returnValue.Add(new Tuple<string, string>("https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts", "Base list"));
            //returnValue.Add(new Tuple<string, string>("https://raw.githubusercontent.com/PolishFiltersTeam/KADhosts/master/KADhosts.txt", "Suspicious Lists"));
            //returnValue.Add(new Tuple<string, string>("https://raw.githubusercontent.com/FadeMind/hosts.extras/master/add.Spam/hosts", "Suspicious Lists"));
            returnValue.Add(new Tuple<string, string>("https://v.firebog.net/hosts/static/w3kbl.txt", "Suspicious Lists"));
            //returnValue.Add(new Tuple<string, string>("https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts", "Base list"));
            //returnValue.Add(new Tuple<string, string>("https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts", "Base list"));
            //returnValue.Add(new Tuple<string, string>("https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts", "Base list"));
            //returnValue.Add(new Tuple<string, string>("https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts", "Base list"));
            //returnValue.Add(new Tuple<string, string>("https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts", "Base list"));
            //returnValue.Add(new Tuple<string, string>("https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts", "Base list"));
            //returnValue.Add(new Tuple<string, string>("https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts", "Base list"));

            return returnValue;
        }

        public static List<string> ToList2(string list)
        {
            var returnValue = Regex.Split(list, "\r\n|\r|\n").Where(l => l != String.Empty).ToList();

            return returnValue;
        }

        public static List<string> RemoveComments2(List<string> list)
        {
            // If we did the first one with a regex as well, we would end up with empty lines
            var trimmed = list.Where(l => !l.StartsWith("#"));
            // \s*  (Any leading whitespace)
            // #    (The # character)
            // .*   (Any character following the #)
            // $    (Untill the end of line)
            trimmed = trimmed.Select(l => Regex.Replace(l, @"\s*#.*$", ""));

            return trimmed.ToList();
        }

        public static List<string> RemoveDestination2(List<string> list)
        {
            var replacedValues = list.ToList();

            for (int i = 0; i < replacedValues.Count; i++)
            {
                // ^            (From the start of the line)
                // 4 times the following
                //    \d{1,3}   (Between 1 and 3 digits)
                //    .         (A point)
                // [\s]         (A whitespace)
                replacedValues[i] = Regex.Replace(replacedValues[i], @"^0\.0\.0\.0[\s]*", "");
                replacedValues[i] = Regex.Replace(replacedValues[i], @"^127\.0\.0\.1[\s]*", "");
            }

            return replacedValues;
        }

        public static List<string> RemoveIfExistInTarget2(List<string> source, List<string> target)
        {
            var x = source.Where(s => target.Contains(s) == false).ToList();

            return x;
        }



        public static string[] ToList(string list)
        {
            var returnValue = Regex.Split(list, "\r\n|\r|\n").Where(l => l != String.Empty).ToArray();

            return returnValue;
        }

        public static string RemoveComments(string record)
        {
            if (record.StartsWith("#") || record.EndsWith("localhost"))
            {
                return string.Empty;
            }

            record = Regex.Replace(record, @"\s*#.*$", "");

            return record;
        }

        public static string RemoveDestination(string record)
        {
            record = Regex.Replace(record, @"^0\.0\.0\.0[\s]*", "");
            return Regex.Replace(record, @"^127\.0\.0\.1[\s]*", "");
        }

        public static List<string> RemoveIfExistInTarget(List<string> source, List<string> target)
        {
            var x = source.Where(s => target.Contains(s) == false).ToList();

            return x;
        }
    }
}
