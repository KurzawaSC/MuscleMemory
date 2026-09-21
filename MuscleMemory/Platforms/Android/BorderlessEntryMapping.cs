using Android.Content.Res;
using Microsoft.Maui.Handlers;

namespace MuscleMemory;

internal static class BorderlessEntryMapping
{
    public static void Register() =>
        EntryHandler.Mapper.AppendToMapping(nameof(BorderlessEntryMapping), (handler, _) =>
            handler.PlatformView.BackgroundTintList = ColorStateList.ValueOf(Android.Graphics.Color.Transparent));
}
