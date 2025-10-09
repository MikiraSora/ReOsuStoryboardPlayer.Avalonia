using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;

public static class TaskEx
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NoWait(this Task task)
    {
        //ignore.
    }
}