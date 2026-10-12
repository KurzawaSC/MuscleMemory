using MuscleMemory.Services;

namespace MuscleMemory;

public static partial class MauiProgram
{
    static partial void ConfigurePlatform(MauiAppBuilder builder)
    {
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler<Shell, TabBarShellRenderer>();
            BorderlessEntryMapping.Register();
            NumericEntryMapping.Register();
            CheckedStateMapping.Register();
        });
        builder.Services.AddSingleton<IStatusBarService, StatusBarService>();
    }
}
