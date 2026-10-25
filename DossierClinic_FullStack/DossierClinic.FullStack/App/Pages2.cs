using Microsoft.Maui.Controls.Shapes; using System.Text.Json.Nodes; using static DossierClinic.Api;
namespace DossierClinic;
static class W {
    public static Border Card(View c, Color bg = null) => new() { Stroke = Ui.Line, BackgroundColor = bg ?? Colors.White, StrokeShape = new RoundRectangle { CornerRadius = 14 }, Padding = 14, Content = c };
    public static Border Chip(string t, Color c) => new() { BackgroundColor = c.WithAlpha(0.15f), StrokeThickness = 0, Padding = new Thickness(10, 3), StrokeShape = new RoundRectangle { CornerRadius = 11 }, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Center,
        Content = new Label { Text = t, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = c } };
    public static View Stat(string v, string l, Color c = null) => Card(new VerticalStackLayout { Children = { new Label { Text = v, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = c ?? Ui.Ink }, new Label { Text = l, FontSize = 12, TextColor = Ui.Muted } } });
    public static Grid Cols(params View[] v) { var g = new Grid { ColumnSpacing = 8 }; for (int i = 0; i < v.Length; i++) { g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star)); g.Add(v[i], i, 0); } return g; }
    public static Grid Head(string t, string s = null) { var g = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } };
        g.Add(new VerticalStackLayout { Children = { F.T(t), new Label { Text = s ?? "", FontSize = 12, TextColor = Ui.Muted } } }, 0); g.Add(Chip(Role, Ui.Pri), 1); return g; }
}
public class TodayPage : Base {
    public TodayPage() { Title = "Aujourd’hui"; }
    protected override void Tap(string id) => Navigation.PushAsync(new PatientPage(int.Parse(id)));
    protected override async void Refresh() { var b = await Need(this, Get("/today")); if (b == null) return; Top.Clear();
        Top.Add(W.Head("Aujourd’hui", S(b, "date"))); Top.Add(W.Cols(W.Stat(S(b, "patients"), "Patients"), W.Stat(S(b, "waiting"), "En attente"), W.Stat(S(b, "received"), "Reçus")));
        Top.Add(new Label { Text = "File d’attente", FontAttributes = FontAttributes.Bold, TextColor = Ui.Muted }); Top.Add(Ui.Btn("+ Nouvelle fiche", () => Navigation.PushAsync(new NewPatientPage())));
        List.ItemsSource = b["visits"].AsArray().Select(v => new Row(S(v, "name"), $"{S(v, "time")} · {S(v, "motif")}", S(v, "status"), S(v, "patientId"), S(v, "status") == "Terminé" ? Ui.Green : S(v, "status") == "En attente" ? Ui.Pri : Ui.Amber)).ToList(); }
}
public class BillingPage : Base {
    string f = "all"; public BillingPage() { Title = "Facturation"; }
    protected override void Tap(string id) => Navigation.PushAsync(new InvoicePage(int.Parse(id)));
    protected override async void Refresh() { var b = await Need(this, Get("/invoices?filter=" + f)); if (b == null) return; Top.Clear(); Top.Add(W.Head("Facturation", "Liste"));
        Top.Add(W.Cols(W.Stat(Money(D(b, "paid")), "Encaissé"), W.Stat(Money(D(b, "due")), "Reste à payer", Ui.Red)));
        Top.Add(W.Cols(Ui.Btn("Toutes", () => { f = "all"; Refresh(); }, f != "all"), Ui.Btn("À encaisser", () => { f = "due"; Refresh(); }, f != "due"), Ui.Btn("Soldées", () => { f = "ok"; Refresh(); }, f != "ok")));
        List.ItemsSource = b["items"].AsArray().Select(i => new Row($"{S(i, "name")} · {Money(D(i, "total"))}", $"{S(i, "num")} · {Dt(S(i, "date"))}", S(i, "status"), S(i, "id"), Ui.Of(S(i, "status")))).ToList(); }
}
public class InvoicePage : Base {
    readonly int id; public InvoicePage(int id) { this.id = id; }
    protected override async void Refresh() { var i = await Need(this, Get("/invoices/" + id)); if (i == null) return; Title = S(i, "num"); Top.Clear(); double t = D(i, "total"), p = D(i, "paid"); var st = S(i, "status");
        var head = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } }; head.Add(new VerticalStackLayout { Children = { F.T(S(i, "name"), 18), new Label { Text = $"{S(i, "patientNum")} · {Dt(S(i, "date"))}", FontSize = 12, TextColor = Ui.Muted } } }, 0); head.Add(W.Chip(st, Ui.Of(st)), 1);
        var card = new VerticalStackLayout { Spacing = 6, Children = { head } }; foreach (var l in i["lines"].AsArray()) card.Add(Ui.Kv(S(l, "label"), Money(D(l, "amount"))));
        card.Add(new Label { Text = "Total : " + Money(t), FontAttributes = FontAttributes.Bold, FontSize = 16 }); card.Add(Ui.Kv("Payé", Money(p)));
        card.Add(new Label { Text = "Reste à payer : " + Money(t - p), FontAttributes = FontAttributes.Bold, FontSize = 16, TextColor = t - p > 0 ? Ui.Red : Ui.Green });
        Top.Add(W.Head("Facture " + S(i, "num"))); Top.Add(W.Card(card));
        if (t - p > 0) Top.Add(Ui.Btn("Encaisser un paiement", () => Navigation.PushModalAsync(new PaymentPage(id, t - p, Refresh))));
        Top.Add(Ui.Btn("Partager la facture", () => Share.Default.RequestAsync(new ShareTextRequest { Text = $"FACTURE {S(i, "num")} du {Dt(S(i, "date"))}\nPatient : {S(i, "name")}\n{S(i, "label")}\nTotal : {Money(t)}\nPayé : {Money(p)}\nReste à payer : {Money(t - p)}" }), true)); }
}
public class PaymentPage : ContentPage {
    public PaymentPage(int id, double due, Action done) { BackgroundColor = Ui.Bg; var am = F.E("Montant reçu (FCFA)", Keyboard.Numeric); am.Text = due.ToString("0");
        Content = new VerticalStackLayout { Padding = 20, Spacing = 12, VerticalOptions = LayoutOptions.Center, Children = { F.T("Encaisser un paiement"), new Label { Text = "Reste à payer : " + Money(due), TextColor = Ui.Muted }, am,
            W.Cols(Ui.Btn("Tout", () => am.Text = due.ToString("0"), true), Ui.Btn("Moitié", () => am.Text = Math.Round(due / 2).ToString("0"), true)),
            Ui.Btn("Valider", async () => { if (!double.TryParse(am.Text, out var a)) return; if (await Need(this, Post($"/invoices/{id}/pay", new { amount = a })) != null) { await Navigation.PopModalAsync(); done(); } }), Ui.Btn("Annuler", () => Navigation.PopModalAsync(), true) } }; }
}
public class NewInvoicePage : ContentPage {
    readonly VerticalStackLayout rows = new() { Spacing = 8 }; readonly List<(Entry l, Entry a)> es = new(); readonly Label tot = new() { FontAttributes = FontAttributes.Bold, FontSize = 16 };
    void AddRow(string l = "", string a = "") { var le = F.E("Libellé"); le.Text = l; var ae = F.E("Montant", Keyboard.Numeric); ae.Text = a; ae.WidthRequest = 110; ae.TextChanged += (_, _) => Sum(); es.Add((le, ae));
        var g = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } }; g.Add(le, 0); g.Add(ae, 1); rows.Add(g); Sum(); }
    void Sum() => tot.Text = "Total : " + Money(es.Sum(x => double.TryParse(x.a.Text, out var v) ? v : 0));
    public NewInvoicePage(int pid, string name) { Title = "Nouvelle facture"; BackgroundColor = Ui.Bg; AddRow("Consultation", "3000");
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 16, Spacing = 10, Children = { F.T("Nouvelle facture"), new Label { Text = name, TextColor = Ui.Muted }, rows, Ui.Btn("+ Ajouter une ligne", () => AddRow(), true), tot,
            Ui.Btn("Créer la facture", async () => { var lines = es.Where(x => !string.IsNullOrWhiteSpace(x.l.Text) && double.TryParse(x.a.Text, out var v) && v > 0).Select(x => new { label = x.l.Text, amount = double.Parse(x.a.Text) }).ToList();
                var b = await Need(this, Post("/invoices", new { patientId = pid, lines })); if (b == null) return; await Navigation.PushAsync(new InvoicePage(int.Parse(S(b, "id")))); Navigation.RemovePage(this); }),
            Ui.Btn("Annuler", () => Navigation.PopAsync(), true) } } }; }
}
public class AccountPage : Base {
    public AccountPage() { Title = "Compte"; }
    static readonly string[] Roles = { "Accueil et caisse", "Médecin", "Administrateur" };
    protected override async void Refresh() { Top.Clear(); Top.Add(W.Head(Role == "Administrateur" ? "Comptes" : "Compte", Role == "Administrateur" ? "Administration" : Name));
        Top.Add(W.Card(new VerticalStackLayout { Children = { new Label { Text = Name, FontAttributes = FontAttributes.Bold }, new Label { Text = "Rôle : " + Role, TextColor = Ui.Muted } } }));
        Top.Add(Ui.Btn("Se déconnecter", () => { Token(null); Role = ""; F.Root(new NavigationPage(new StartPage())); }, true)); if (Role != "Administrateur") return;
        var b = await Need(this, Get("/accounts")); if (b == null) return; var all = b.AsArray().ToList(); var pend = all.Where(a => S(a, "status") == "pending").ToList();
        Top.Add(new Label { Text = $"En attente de validation ({pend.Count})", FontAttributes = FontAttributes.Bold, TextColor = Ui.Muted });
        foreach (var a in pend) { string chosen = ""; var chips = new List<Button>(); var rr = new Grid { ColumnSpacing = 6 };
            for (int k = 0; k < 3; k++) { var r = Roles[k]; var c = new Button { Text = r, FontSize = 11, CornerRadius = 12, BackgroundColor = Colors.White, TextColor = Ui.Ink, BorderColor = Ui.Line, BorderWidth = 1, Padding = 2 }; chips.Add(c); rr.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star)); rr.Add(c, k, 0);
                c.Clicked += (_, _) => { chosen = r; foreach (var x in chips) { var on = x.Text == chosen; x.BackgroundColor = on ? Ui.Pri : Colors.White; x.TextColor = on ? Colors.White : Ui.Ink; } }; }
            var id = S(a, "id"); Top.Add(W.Card(new VerticalStackLayout { Spacing = 6, Children = { new Label { Text = S(a, "name"), FontAttributes = FontAttributes.Bold }, new Label { Text = S(a, "email"), FontSize = 12, TextColor = Ui.Muted }, new Label { Text = "Invitation " + S(a, "code"), FontSize = 12, TextColor = Ui.Muted },
                new Label { Text = "Rôle attribué", FontSize = 12 }, rr, W.Cols(Ui.Btn("Refuser", async () => { var r = await Call(HttpMethod.Delete, "/accounts/" + id); if (!r.Ok) await DisplayAlert("Erreur", r.Err, "OK"); Refresh(); }, true),
                    Ui.Btn("Valider le compte", async () => { if (chosen == "") { await DisplayAlert("Compte", "Choisissez d’abord un rôle.", "OK"); return; } if (await Need(this, Post($"/accounts/{id}/validate", new { role = chosen })) != null) Refresh(); })) } })); }
        var act = all.Where(a => S(a, "status") == "active").ToList(); Top.Add(new Label { Text = $"Comptes actifs ({act.Count})", FontAttributes = FontAttributes.Bold, TextColor = Ui.Muted });
        foreach (var a in act) { var g = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } }; g.Add(new Label { Text = S(a, "name"), FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center }, 0); g.Add(W.Chip(S(a, "role"), Ui.Pri), 1); Top.Add(W.Card(g)); } }
}
