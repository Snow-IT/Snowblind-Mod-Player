using Microsoft.Win32.TaskScheduler;
using System;
using System.Diagnostics;

namespace SnowblindModPlayer;

public static class TaskSchedulerService
{
    private const string TaskName = "SnowblindModPlayer";

    public static bool IsAutostartEnabled()
    {
        using var ts = new TaskService();
        var t = ts.GetTask(TaskName);
        return t != null && t.Enabled;
    }

    public static void EnableAutostart()
    {
        var exe = Process.GetCurrentProcess().MainModule?.FileName
                  ?? throw new InvalidOperationException("Exe path not found.");

        using var ts = new TaskService();
        var td = ts.NewTask();
        td.RegistrationInfo.Description = "Starts Snowblind-Mod Player on user logon.";

        td.Triggers.Add(new LogonTrigger { UserId = Environment.UserName });
        td.Actions.Add(new ExecAction(exe, "--autoplay", null));

        td.Settings.DisallowStartIfOnBatteries = false;
        td.Settings.StopIfGoingOnBatteries = false;
        td.Settings.StartWhenAvailable = true;
        td.Settings.Hidden = true;

        ts.RootFolder.RegisterTaskDefinition(TaskName, td);
    }

    public static void DisableAutostart()
    {
        using var ts = new TaskService();
        var t = ts.GetTask(TaskName);
        if (t != null)
            ts.RootFolder.DeleteTask(TaskName, false);
    }
}
