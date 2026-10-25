using System.Globalization; using System.Net.Http.Headers; using System.Net.Http.Json; using System.Text.Json.Nodes;
namespace DossierClinic;
public class R { public bool Ok; public int Code; public JsonNode Body; public string Err; }
public static class Api {
    public static string Base => Preferences.Get("api", DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5000" : "http://localhost:5000");
    static readonly HttpClient H = new() { Timeout = TimeSpan.FromSeconds(15) }; public static string Role = "", Name = "";
    public static void Token(string t) => H.DefaultRequestHeaders.Authorization = t == null ? null : new AuthenticationHeaderValue("Bearer", t);
    public static async Task<R> Call(HttpMethod m, string path, object body = null) {
        try { var q = new HttpRequestMessage(m, Base + "/api" + path); if (body != null) q.Content = JsonContent.Create(body);
            var res = await H.SendAsync(q); var s = await res.Content.ReadAsStringAsync(); var ok = res.IsSuccessStatusCode;
            return new R { Ok = ok, Code = (int)res.StatusCode, Body = ok && s != "" ? JsonNode.Parse(s) : null, Err = ok ? null : (s.Trim('"') is { Length: > 0 } e ? e : $"Erreur {(int)res.StatusCode}") };
        } catch { return new R { Err = "Serveur injoignable. Vérifiez l’adresse de l’API." }; } }
    public static Task<R> Get(string p) => Call(HttpMethod.Get, p); public static Task<R> Post(string p, object b = null) => Call(HttpMethod.Post, p, b ?? new { });
    public static string S(JsonNode n, string k) => n?[k]?.ToString() ?? ""; public static double D(JsonNode n, string k) => (double?)n?[k] ?? 0;
    static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");
    public static string Money(double n) => n.ToString("N0", Fr).Replace('\u202f', ' ').Replace('\u00a0', ' ') + " FCFA";
    public static string Dt(string d) => DateTime.TryParse(d, out var x) ? x.ToString("d MMM yyyy", Fr) : d;
    public static async Task<JsonNode> Need(Page p, Task<R> t) { var r = await t; if (!r.Ok) { await p.DisplayAlert("Erreur", r.Err, "OK"); return null; } return r.Body ?? new JsonObject(); }
}
