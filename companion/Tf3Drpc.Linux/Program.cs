using System;

using Avalonia;

namespace Tf3Drpc.Linux;

/// <summary>
/// Starts the isolated Linux desktop feasibility host.
/// </summary>
internal static class Program
{
    internal static bool DisableTray { get; private set; }

    internal static bool SmokeTest { get; private set; }

    [STAThread]
    private static int Main(string[] args)
    {
        bool checkResources = false;

        foreach (string argument in args)
        {
            switch (argument)
            {
                case "--check-resources":
                    checkResources = true;
                    break;

                case "--no-tray":
                    DisableTray = true;
                    break;

                case "--smoke-test":
                    SmokeTest = true;
                    break;

                default:
                    Console.Error.WriteLine("Usage: Tf3Drpc.Linux [--check-resources] [--no-tray] [--smoke-test]");

                    return 2;
            }
        }

        if (checkResources)
            return ResourceCheck.Run();

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime(args);
    }
}
