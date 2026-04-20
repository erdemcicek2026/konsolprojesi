namespace konsolprojesi;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    // Dikkat: IActivationState kelimesinin yanındaki '?' işaretini SİLDİK.
    protected override Window CreateWindow(IActivationState activationState)
    {
        return new Window(new MainPage());
    }
}