namespace ec.edu.monster.vista;

/// <summary>
/// Barra de progreso indeterminada, delgada, arriba del contenido (el LinearProgressIndicator
/// de Android): un tramo verde que recorre la pista celeste mientras se procesa una petición.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class BarraProgreso : ContentView
{
    private const string ANIMACION = "progreso";
    private readonly BoxView tramo;

    public BarraProgreso()
    {
        tramo = new BoxView { Color = Colores.De("Verde"), HorizontalOptions = LayoutOptions.Start };
        Content = new Grid { BackgroundColor = Colores.De("AzulClaro"), HeightRequest = 4, IsClippedToBounds = true, Children = { tramo } };
        IsVisible = false;
        SemanticProperties.SetDescription(this, "Procesando…");
    }

    public void Iniciar()
    {
        IsVisible = true;
        this.AbortAnimation(ANIMACION);
        new Animation(v =>
        {
            double ancho = Width > 0 ? Width : 360;
            tramo.WidthRequest = ancho * 0.4;
            tramo.TranslationX = -ancho * 0.4 + v * ancho * 1.4;
        }).Commit(this, ANIMACION, length: 1200, easing: Easing.CubicInOut, repeat: () => IsVisible);
    }

    public void Detener()
    {
        this.AbortAnimation(ANIMACION);
        IsVisible = false;
    }

    /// <summary>Oculta la barra sin detenerla del todo (mientras un diálogo espera al usuario).</summary>
    public void Ocultar(bool ocultar)
    {
        Opacity = ocultar ? 0 : 1;
    }
}
