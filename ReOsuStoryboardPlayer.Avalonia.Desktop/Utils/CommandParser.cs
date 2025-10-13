using System;
using System.Collections.Generic;
using System.Linq;
using ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Parameters;
using ReOsuStoryboardPlayer.Core.Utils;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.Utils;

internal static class CommandParser
{
    private static readonly ParamParserV2 _parser = new('-', '\"', '\'');

    public static DesktopParameters Parse(string args, out string cmdName)
    {
        var array = args.Split(' ');
        cmdName = array.First();
        var arg = string.Join(" ", array.Skip(1));
        if (_parser.TryDivide(arg, out var p))
            return new DesktopParameters(p);

        return null;
    }

    public static DesktopParameters Parse(string[] args, out string cmdName)
    {
        for (var i = 0; i < args.Length; i++)
        {
            var s = args[i];
            if (s.Any(k => k == ' '))
                args[i] = $"{_parser.Quotes.First()}{s}{_parser.Quotes.First()}";
        }

        return Parse(string.Join(" ", args), out cmdName);
    }

    public class ParamParserV2
    {
        private readonly string _cmdFlagStr;
        private readonly string[] _quotesStr;

        public ParamParserV2(char cmdFlag, params char[] quotes)
        {
            _quotesStr = quotes.Select(k => k.ToString()).ToArray();
            _cmdFlagStr = cmdFlag.ToString();
            Quotes = quotes;
            CmdFlag = cmdFlag;
        }

        public char[] Quotes { get; }
        public char CmdFlag { get; }

        public bool TryDivide(string args, out DesktopParameters p)
        {
            p = new DesktopParameters();
            var argStr = args.Trim();

            p.SimpleArgs.AddRange(argStr.Split(' '));
            if (argStr == "")
            {
                p = null;
                return false;
            }

            var splitedParam = new List<string>();
            try
            {
                splitedParam.AddRange(argStr.Split(' '));
                foreach (var item in splitedParam)
                    if (Quotes.Any(q => ContainsChar(q, item)))
                        throw new ArgumentException();

                var combined = true;
                foreach (var item in _quotesStr)
                {
                    for (var i = 0; i < splitedParam.Count - 1; i++)
                    {
                        string cur = splitedParam[i], next = splitedParam[i + 1];

                        if (cur.StartsWith(item) && !cur.EndsWith(item))
                        {
                            combined = false;
                            splitedParam[i] = cur + " " + next;
                            splitedParam.Remove(next);
                            if (splitedParam[i].EndsWith(item))
                                combined = true;
                            i--;
                        }
                    }

                    if (!combined) throw new ArgumentException("Expect '" + item + "'.");
                }

                string tmpKey = null;
                var isLastKeyOrValue = false;

                splitedParam.Add(_cmdFlagStr);
                foreach (var item in splitedParam)
                {
                    string tmpValue = null;
                    if (item.StartsWith(_cmdFlagStr))
                    {
                        if (tmpKey != null)
                            p.Switches.Add(tmpKey);

                        tmpKey = item.Remove(0, 1);
                        isLastKeyOrValue = true;
                    }
                    else
                    {
                        foreach (var q in Quotes)
                            tmpValue = tmpValue == null ? item.Trim(q) : tmpValue.Trim(q);
                        if (!isLastKeyOrValue)
                        {
                            p.FreeArgs.Add(tmpValue);
                            //throw new ArgumentException("Expect key.");
                        }
                        else
                        {
                            p.Args.Add(tmpKey, tmpValue);
                            tmpKey = null;
                            isLastKeyOrValue = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                p = null;
                return false;
            }

            return true;
        }

        private bool ContainsChar(char ch, string str)
        {
            var cs = str.ToCharArray();
            for (var i = 1; i < cs.Length - 1; i++)
                if (cs[i] == ch)
                    return true;

            return false;
        }
    }
}