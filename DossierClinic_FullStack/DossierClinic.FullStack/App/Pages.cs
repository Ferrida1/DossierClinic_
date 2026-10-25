using System.Text.Json.Nodes; using static DossierClinic.Api;
namespace DossierClinic;
static class F {
    public static Entry E(string ph, Keyboard k = null, bool pw = false) => new() { Placeholder = ph, Keyboard = k ?? Keyboard.Default, IsPassword = pw, BackgroundColor = Colors.White };
    public static Label T(string t, int s = 22) => new() { Text = t, FontSize = s, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink };
    public static void Root(Page p) => Application.Current.Windows[0].Page = p;
}
public class SplashPage : ContentPage {
    public SplashPage() { BackgroundColor = Ui.Pri; Content = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Padding = 40, Spacing = 10, Children = {
        new Label { Text = "DossierClinic", FontSize = 34, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center },
        new Label { Text = "Dossiers patients, consultations et factures, même sans réseau.", TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center } } }; }
    protected override async void OnAppearing() { base.OnAppearing(); await Task.Delay(1800); F.Root(new NavigationPage(new StartPage())); }
}
public class StartPage : ContentPage {
    public StartPage() { BackgroundColor = Colors.White; Content = new VerticalStackLayout { Padding = 40, Spacing = 14, VerticalOptions = LayoutOptions.Center, Children = {
        new Label { Text = "DossierClinic", FontSize = 34, FontAttributes = FontAttributes.Bold, TextColor = Ui.Pri, HorizontalTextAlignment = TextAlignment.Center }, F.T("Commençons", 20),
        Ui.Btn("Se connecter", () => Navigation.PushAsync(new LoginPage())), Ui.Btn("Inscrivez-vous", () => Navigation.PushAsync(new SignupPage()), true) } }; }
}
public class LoginPage : ContentPage {
    public LoginPage() { BackgroundColor = Colors.White; Title = "Se connecter"; var em = F.E("E-mail", Keyboard.Email); var pw = F.E("Mot de passe", pw: true);
        Content = new VerticalStackLayout { Padding = 24, Spacing = 12, Children = { em, pw,
            Ui.Btn("Se connecter", async () => { var r = await Post("/login", new { email = em.Text, password = pw.Text });
                if (!r.Ok) { await DisplayAlert("Connexion", r.Code == 403 ? "Compte en attente de validation par l’administrateur" : r.Code == 401 ? "E-mail ou mot de passe incorrect" : r.Err, "OK"); return; }
                Token(S(r.Body, "token")); Name = S(r.Body, "name"); Role = S(r.Body, "role");
                await DisplayAlert("Youpi ! Bon retour !", "Vous vous êtes connecté(e) avec succès.", "Aller à l’accueil"); F.Root(new AppShell()); }),
            Ui.Btn("Mot de passe oublié ?", async () => { var c = await DisplayPromptAsync("Réinitialisation", "Code d’invitation (ex. CL-4F7A)"); if (c == null) return;
                var n = await DisplayPromptAsync("Réinitialisation", "Nouveau mot de passe (6 caractères min.)"); if (n == null) return;
                if (await Need(this, Post("/reset", new { email = em.Text, code = c, password = n })) != null) await DisplayAlert("Succès", "Votre mot de passe a été réinitialisé avec succès.", "Se connecter"); }, true) } }; }
}
public class SignupPage : ContentPage {
    public SignupPage() { BackgroundColor = Colors.White; Title = "Inscrivez-vous"; var nm = F.E("Nom complet"); var em = F.E("E-mail", Keyboard.Email); var pw = F.E("Mot de passe (6 caractères min.)", pw: true);
        var cb = new CheckBox { Color = Ui.Pri }; var terms = new HorizontalStackLayout { Children = { cb, new Label { Text = "J’accepte les Conditions d’utilisation et la Politique de confidentialité.", VerticalOptions = LayoutOptions.Center, WidthRequest = 280 } } };
        Content = new VerticalStackLayout { Padding = 24, Spacing = 12, Children = { nm, em, pw, terms, Ui.Btn("Inscrivez-vous", async () => {
            if (!cb.IsChecked) { await DisplayAlert("Inscription", "Acceptez les conditions d’utilisation.", "OK"); return; }
            var b = await Need(this, Post("/signup", new { name = nm.Text, email = em.Text, password = pw.Text })); if (b == null) return;
            await DisplayAlert("Compte créé", $"Un administrateur doit valider votre compte avant la première connexion.\nVotre code d’invitation : {S(b, "Code")}", "OK"); await Navigation.PopAsync(); }) } }; }
}
public class PatientsPage : Base {
    readonly SearchBar sb = new() { Placeholder = "Nom, téléphone ou numéro de dossier" };
    public PatientsPage() { Title = "Patients"; sb.TextChanged += (_, _) => Refresh(); ToolbarItems.Add(new ToolbarItem("+ Nouvelle fiche", null, () => Navigation.PushAsync(new NewPatientPage()))); }
    protected override void Tap(string id) => Navigation.PushAsync(new PatientPage(int.Parse(id)));
    protected override async void Refresh() { if (!Top.Children.Contains(sb)) Top.Add(sb); var b = await Need(this, Get("/patients?q=" + Uri.EscapeDataString(sb.Text ?? ""))); if (b == null) return;
        List.ItemsSource = b.AsArray().Select(p => new Row($"{S(p, "prenom")} {S(p, "nom").ToUpper()}", $"{S(p, "num")} · {S(p, "sexe")} · {S(p, "tel")}", D(p, "balance") > 0 ? Money(D(p, "balance")) : "", S(p, "id"), Ui.Red)).ToList(); }
}
public class NewPatientPage : ContentPage {
    public NewPatientPage() { Title = "Nouvelle fiche"; BackgroundColor = Ui.Bg; var nom = F.E("Nom"); var pre = F.E("Prénom"); var sexe = new Picker { Title = "Sexe", ItemsSource = new[] { "Femme", "Homme" }, SelectedIndex = 0 };
        var nai = new DatePicker { MaximumDate = DateTime.Today, Date = DateTime.Today.AddYears(-30) }; var tel = F.E("Téléphone", Keyboard.Telephone); var qua = F.E("Quartier");
        var grp = new Picker { Title = "Groupe sanguin", ItemsSource = new[] { "Inconnu", "O+", "O-", "A+", "A-", "B+", "B-", "AB+", "AB-" }, SelectedIndex = 0 }; var al = F.E("Allergies"); var ct = F.E("Personne à prévenir (nom et téléphone)"); var mo = F.E("Motif de la visite");
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 16, Spacing = 10, Children = { F.T("Nouvelle fiche"), nom, pre, sexe, new Label { Text = "Date de naissance" }, nai, tel, qua, grp, al, ct, mo,
            Ui.Btn("Enregistrer", async () => { var b = await Need(this, Post("/patients", new { nom = nom.Text, prenom = pre.Text, sexe = sexe.SelectedItem, naissance = nai.Date.ToString("yyyy-MM-dd"), tel = tel.Text, quartier = qua.Text, groupe = grp.SelectedItem, allergies = al.Text, contact = ct.Text, motif = mo.Text }));
                if (b == null) return; var id = int.Parse(S(b, "id")); await Navigation.PushAsync(new PatientPage(id)); Navigation.RemovePage(this); }),
            Ui.Btn("Annuler", () => Navigation.PopAsync(), true) } } }; }
}
public class PatientPage : Base {
    readonly int id; public PatientPage(int id) { this.id = id; }
    protected override async void Refresh() { var p = await Need(this, Get("/patients/" + id)); if (p == null) return; Title = S(p, "num"); Top.Clear();
        Top.Add(F.T($"{S(p, "prenom")} {S(p, "nom").ToUpper()}")); Top.Add(new Label { Text = S(p, "num"), TextColor = Ui.Muted });
        Top.Add(Ui.Kv("Sexe, âge", $"{S(p, "sexe")}, {S(p, "age")} ans")); Top.Add(Ui.Kv("Naissance", Dt(S(p, "naissance")))); Top.Add(Ui.Kv("Téléphone", S(p, "tel"))); Top.Add(Ui.Kv("Quartier", S(p, "quartier"))); Top.Add(Ui.Kv("Groupe sanguin", S(p, "groupe"))); Top.Add(Ui.Kv("Personne à prévenir", S(p, "contact")));
        if (S(p, "allergies") != "") Top.Add(Ui.Alert("Allergie : " + S(p, "allergies"), true)); if (D(p, "balance") > 0) Top.Add(Ui.Alert("Reste à payer : " + Money(D(p, "balance"))));
        if (Role != "Médecin") Top.Add(Ui.Btn("Créer une facture", () => Navigation.PushAsync(new NewInvoicePage(id, S(p, "prenom") + " " + S(p, "nom")))));
        if (Role != "Accueil et caisse") { Top.Add(Ui.Btn("Historique", () => Navigation.PushAsync(new HistoryPage(id, S(p, "allergies"))), true)); Top.Add(Ui.Btn("Consultation", () => Navigation.PushAsync(new ConsultPage(id, S(p, "prenom") + " " + S(p, "nom"), S(p, "allergies"))))); }
        else Top.Add(new Label { Text = "Historique et consultation : réservés au médecin", FontSize = 12, TextColor = Ui.Muted });
        var l2 = p["last"]; Top.Add(new Label { Text = "Dernière visite", FontAttributes = FontAttributes.Bold }); Top.Add(new Label { Text = l2 == null ? "Aucune visite" : $"{S(l2, "diag")}\n{Dt(S(l2, "date"))} · {S(l2, "motif")}" }); }
}
public class HistoryPage : Base {
    readonly int id; readonly string al; public HistoryPage(int id, string al) { this.id = id; this.al = al; Title = "Historique"; }
    protected override async void Refresh() { Top.Clear(); Top.Add(F.T("Historique")); if (al != "") Top.Add(Ui.Alert("Allergie : " + al, true));
        var b = await Need(this, Get($"/patients/{id}/consults")); if (b == null) return;
        List.ItemsSource = b.AsArray().Select(c => new Row(S(c, "diag") != "" ? S(c, "diag") : S(c, "motif"), $"{Dt(S(c, "date"))} · {S(c, "motif")} · {S(c, "temp")} °C {S(c, "tension")}\nOrdonnance : {(S(c, "ordo") == "" ? "Aucune ordonnance" : S(c, "ordo"))}", "", "0", Ui.Pri)).ToList(); }
}
public class ConsultPage : ContentPage {
    public ConsultPage(int id, string nom, string al) { Title = "Consultation"; BackgroundColor = Ui.Bg; var mo = F.E("Motif de la consultation"); var te = F.E("Temp. (°C)", Keyboard.Numeric); var ts = F.E("Tension (ex. 12/8)"); var po = F.E("Poids (kg)", Keyboard.Numeric); var dg = F.E("Diagnostic");
        var od = new Editor { Placeholder = "Ordonnance (médicament, posologie, durée)", HeightRequest = 120, BackgroundColor = Colors.White }; var v = new VerticalStackLayout { Padding = 16, Spacing = 10 };
        v.Add(F.T("Consultation")); v.Add(new Label { Text = $"{DateTime.Today:d MMM yyyy} · {nom}" }); if (al != "") v.Add(Ui.Alert("Allergie : " + al, true));
        foreach (var x in new View[] { mo, te, ts, po, dg, od }) v.Add(x);
        v.Add(Ui.Btn("Prescrire", async () => { if (await Need(this, Post($"/patients/{id}/consults", new { motif = mo.Text, temp = te.Text, tension = ts.Text, poids = po.Text, diag = dg.Text, ordo = od.Text })) != null) { await DisplayAlert("Consultation", "Ordonnance enregistrée", "OK"); await Navigation.PopAsync(); } }));
        v.Add(Ui.Btn("Annuler", () => Navigation.PopAsync(), true)); Content = new ScrollView { Content = v }; }
}
