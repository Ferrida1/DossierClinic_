namespace DossierClinic;
// Remplace App.xaml.cs du modèle "dotnet new maui" (garder App.xaml).
public partial class App : Application { public App() { InitializeComponent(); } protected override Window CreateWindow(IActivationState s) => new(new SplashPage()); }
