using Android.App;
using Android.Content.PM;
using Android.Views;

namespace ec.edu.monster;

/// <summary>
/// Única actividad de Android. Los cambios de configuración (girar la pantalla, el teclado) no la
/// recrean, así la sesión y las listas se conservan, como en el cliente Java.
/// </summary>
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    WindowSoftInputMode = SoftInput.AdjustResize,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density
        | ConfigChanges.KeyboardHidden)]
public class MainActivity : MauiAppCompatActivity
{
}
