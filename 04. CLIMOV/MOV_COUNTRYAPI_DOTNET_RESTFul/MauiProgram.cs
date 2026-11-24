namespace ec.edu.monster;

/// <summary>Arranque de MAUI: registra el modelo de la app y quita el subrayado de los campos de Android.</summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        builder.Services.AddSingleton<AplicacionCountryApi>();

        // Sin la línea inferior que Android dibuja en los Entry y Picker: el contorno lo pone Campo.
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("SinSubrayado", (handler, _) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
        });
        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("SinSubrayado", (handler, _) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
        });

        return builder.Build();
    }
}
