# DossierClinic – full stack (ASP.NET Core + .NET MAUI)
## 1. API  (Api/)
    cd Api && dotnet run          # http://0.0.0.0:5000, base SQLite clinic.db créée automatiquement
Comptes de départ : admin@clinique.cm / admin123 · medecin@clinique.cm / medecin123 · caisse@clinique.cm / caisse123 (à changer !)
Clé JWT : variable d'environnement  Jwt__Key  (32 caractères min.) avant la mise en ligne.
## 2. Application mobile  (App/)
    dotnet new maui -n DossierClinic.App   (.NET 10 : net10.0-android)
    supprimer MainPage.xaml(.cs) et AppShell.xaml(.cs) ; copier Api.cs Ui.cs Pages.cs Pages2.cs AppShell.cs ; remplacer App.xaml.cs
    dans Platforms/Android/AndroidManifest.xml : <application android:usesCleartextTraffic="true" ...>   (HTTP en développement)
    dotnet build -t:Run -f net8.0-android
Émulateur Android : l'API est jointe via http://10.0.2.2:5000 (déjà configuré). Téléphone réel : Preferences.Set("api","http://IP-DU-PC:5000").
NOTE: supprimer Api/clinic.db avant de relancer l'API (nouvelle colonne Lines).
