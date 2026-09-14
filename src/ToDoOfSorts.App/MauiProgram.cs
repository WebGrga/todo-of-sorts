using Microsoft.Extensions.Logging;
using ToDoOfSorts.Core;
using ToDoOfSorts.Infrastructure;
using ToDoOfSorts.App.Services;

namespace ToDoOfSorts.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "Body");
            fonts.AddFont("OpenSans-Semibold.ttf", "BodyBold");
            fonts.AddFont("Anton-Regular.ttf", "Stamp");
            fonts.AddFont("IBMPlexMono-Regular.ttf", "Mono");
        });
#if ANDROID
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler<TimePicker, Platforms.Android.ReadableTimePickerHandler>();
            handlers.AddHandler<DatePicker, Platforms.Android.ReadableDatePickerHandler>();
        });
        Microsoft.Maui.Handlers.LabelHandler.Mapper.AppendToMapping("PrintedType", (handler, _) =>
            handler.PlatformView.SetIncludeFontPadding(false));
        Microsoft.Maui.Handlers.ButtonHandler.Mapper.AppendToMapping("PrintedButton", (handler, _) =>
        {
            if (handler.PlatformView is Google.Android.Material.Button.MaterialButton button)
            { button.InsetTop = 0; button.InsetBottom = 0; }
        });
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("PaperField", (handler, _) =>
            handler.PlatformView.BackgroundTintList = global::Android.Content.Res.ColorStateList.ValueOf(global::Android.Graphics.Color.Transparent));
        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("PaperField", (handler, _) =>
            handler.PlatformView.BackgroundTintList = global::Android.Content.Res.ColorStateList.ValueOf(global::Android.Graphics.Color.Transparent));
        Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("PaperField", (handler, _) =>
            handler.PlatformView.BackgroundTintList = global::Android.Content.Res.ColorStateList.ValueOf(global::Android.Graphics.Color.Transparent));
        Microsoft.Maui.Handlers.TimePickerHandler.Mapper.AppendToMapping("PaperField", (handler, _) =>
            handler.PlatformView.BackgroundTintList = global::Android.Content.Res.ColorStateList.ValueOf(global::Android.Graphics.Color.Transparent));
#endif
        builder.Services.AddSingleton<IClock, SystemClock>();
        builder.Services.AddSingleton(sp => new LocalStore(Path.Combine(FileSystem.AppDataDirectory, "daybook.db3"), sp.GetRequiredService<IClock>()));
        builder.Services.AddSingleton<IFeedbackService, StampFeedback>();
#if ANDROID
        builder.Services.AddSingleton<IReminderService, Platforms.Android.AndroidReminders>();
#elif IOS
        builder.Services.AddSingleton<IReminderService, Platforms.iOS.IosReminders>();
#endif
        builder.Services.AddSingleton<AppSession>();
#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
