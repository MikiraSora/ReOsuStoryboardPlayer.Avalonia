namespace ReOsuStoryboardPlayer.Avalonia.Services.Parameters;

public interface IParameters
{
    string CommandName { get; }
    bool TryGetArg(string key, out string value);
    bool TryGetArg(out string value, params string[] keys);
    bool TryGetSwitch(params string[] option_names);
}