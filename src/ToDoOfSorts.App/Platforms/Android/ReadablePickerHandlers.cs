using Android.App;
using Android.Content.Res;
using Microsoft.Maui.Handlers;

namespace ToDoOfSorts.App.Platforms.Android;

internal static class ReadablePickerButtons
{
    public static void Attach(AlertDialog dialog)
    {
        // Native picker actions inherit colorPrimary, which is our window background.
        // Keep their labels legible on the native dialog's light or dark surface.
        dialog.ShowEvent += (_, _) =>
        {
            var dark = (dialog.Context?.Resources?.Configuration?.UiMode & UiMode.NightMask) == UiMode.NightYes;
            var color = global::Android.Graphics.Color.ParseColor(dark ? "#EFE5CB" : "#25271E");
            dialog.GetButton((int)global::Android.Content.DialogButtonType.Positive)?.SetTextColor(color);
            dialog.GetButton((int)global::Android.Content.DialogButtonType.Negative)?.SetTextColor(color);
        };
    }
}

public sealed class ReadableTimePickerHandler : TimePickerHandler
{
    protected override TimePickerDialog CreateTimePickerDialog(int hour, int minute)
    {
        var dialog = base.CreateTimePickerDialog(hour, minute);
        ReadablePickerButtons.Attach(dialog);
        return dialog;
    }
}

public sealed class ReadableDatePickerHandler : DatePickerHandler
{
    protected override DatePickerDialog CreateDatePickerDialog(int year, int month, int day)
    {
        var dialog = base.CreateDatePickerDialog(year, month, day);
        ReadablePickerButtons.Attach(dialog);
        return dialog;
    }
}
