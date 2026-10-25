using Microsoft.Maui.Controls.Shapes;
namespace DossierClinic;

public record Row(string Title, string Sub, string Chip, string Id, Color Color);

public static class Ui {
    public static readonly Color Pri = Color.FromArgb("#1A9A8B"), Bg = Color.FromArgb("#F4F5F5"), Ink = Color.FromArgb("#12292B"),
        Muted = Color.FromArgb("#5B7274"), Line = Color.FromArgb("#DCE6E6"), Red = Color.FromArgb("#C2412D"), Green = Color.FromArgb("#1E7A4B"), Amber = Color.FromArgb("#9A6B00");
    public static Color Of(string status) => status == "Soldée" ? Green : status == "Partielle" ? Amber : Red;
    public static Label Title(string t, string sub = null) => new() { Text = t + (sub != null ? "\n" + sub : ""), FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Ink };
    public static Label Kv(string k, string v) => new() { Text = $"{k} : {(string.IsNullOrWhiteSpace(v) ? "—" : v)}", TextColor = Ink };
    public static Border Alert(string t, bool red = false) => new() { BackgroundColor = Color.FromArgb(red ? "#FBE9E5" : "#FFF3D6"), StrokeThickness = 0, Padding = 10,
        StrokeShape = new RoundRectangle { CornerRadius = 10 }, Content = new Label { Text = t, TextColor = red ? Red : Amber, FontAttributes = FontAttributes.Bold } };
    public static Button Btn(string t, Action a, bool secondary = false) {
        var b = new Button { Text = t, CornerRadius = 14, BackgroundColor = secondary ? Colors.White : Pri, TextColor = secondary ? Pri : Colors.White, BorderColor = Line, BorderWidth = secondary ? 1 : 0 };
        b.Clicked += (_, _) => a(); return b; }
    public static CollectionView List(Action<string> tap) {
        var cv = new CollectionView { SelectionMode = SelectionMode.Single, ItemTemplate = new DataTemplate(() => {
            var t = new Label { FontAttributes = FontAttributes.Bold, TextColor = Ink }; t.SetBinding(Label.TextProperty, "Title");
            var s = new Label { TextColor = Muted, FontSize = 13 }; s.SetBinding(Label.TextProperty, "Sub");
            var c = new Label { FontAttributes = FontAttributes.Bold, FontSize = 12, VerticalOptions = LayoutOptions.Center }; c.SetBinding(Label.TextProperty, "Chip"); c.SetBinding(Label.TextColorProperty, "Color");
            var g = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) }, Padding = 14, BackgroundColor = Colors.White };
            g.Add(new VerticalStackLayout { Children = { t, s } }, 0); g.Add(c, 1);
            return new Border { Stroke = Line, StrokeShape = new RoundRectangle { CornerRadius = 14 }, Margin = new Thickness(0, 0, 0, 8), Content = g }; }) };
        cv.SelectionChanged += (_, e) => { if (e.CurrentSelection.FirstOrDefault() is Row r) { cv.SelectedItem = null; tap(r.Id); } };
        return cv; }
}

public abstract class Base : ContentPage {
    protected VerticalStackLayout Top = new() { Spacing = 8 }; protected CollectionView List;
    protected Base() {
        BackgroundColor = Ui.Bg; List = Ui.List(Tap);
        var g = new Grid { RowDefinitions = { new(GridLength.Auto), new(GridLength.Star) }, Padding = 16, RowSpacing = 10 };
        g.Add(new ScrollView { Content = Top }, 0, 0); g.Add(List, 0, 1); Content = g; }
    protected virtual void Tap(string id) { }
    protected abstract void Refresh();
    protected override void OnAppearing() { base.OnAppearing(); Refresh(); }
}
