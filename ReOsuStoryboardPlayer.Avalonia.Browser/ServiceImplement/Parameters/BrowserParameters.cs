using System;
using System.Collections.Generic;
using System.Linq;
using ReOsuStoryboardPlayer.Avalonia.Browser.Utils;
using ReOsuStoryboardPlayer.Avalonia.Services.Parameters;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Parameters;

public class BrowserParameters : IParameters
{
    public BrowserParameters()
    {
        
    }

    public BrowserParameters(BrowserParameters p)
    {
        Args = p.Args;
        FreeArgs = p.FreeArgs;
        Switches = p.Switches;
        SimpleArgs = p.SimpleArgs;
        CommandName = p.CommandName;
    }

    internal Dictionary<string, string> Args { get; } = [];
    internal List<string> FreeArgs { get; } = [];
    internal List<string> Switches { get; } = [];
    internal List<string> SimpleArgs { get; } = [];

    public string CommandName { get; }

    public bool TryGetArg(string key, out string value)
    {
        if (Args.ContainsKey(key))
        {
            value = Args[key];
            return true;
        }

        value = null;
        return false;
    }

    public bool TryGetArg(out string value, params string[] keys)
    {
        var x = Args.FirstOrDefault(pair => keys.Any(key => key == pair.Key));

        value = x.Value;

        return !string.IsNullOrWhiteSpace(x.Key);
    }

    public bool TryGetSwitch(params string[] option_names)
    {
        return Switches.Any(x => option_names.Contains(x));
    }
}