namespace DossierClinic;
public class AppShell : Shell {
    public AppShell() { var t = new TabBar(); void A(string n, Func<Page> m) => t.Items.Add(new ShellContent { Title = n, ContentTemplate = new DataTemplate(() => m()) });
        A("Aujourd’hui", () => new TodayPage()); A("Patients", () => new PatientsPage()); if (Api.Role != "Médecin") A("Facturation", () => new BillingPage()); A("Compte", () => new AccountPage());
        Items.Add(t); BackgroundColor = Ui.Pri; Shell.SetForegroundColor(this, Colors.White); }
}
