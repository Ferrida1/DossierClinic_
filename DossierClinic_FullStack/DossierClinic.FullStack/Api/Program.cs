using System.Text.Json; using System.IdentityModel.Tokens.Jwt; using System.Security.Claims; using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer; using Microsoft.AspNetCore.Identity; using Microsoft.EntityFrameworkCore; using Microsoft.IdentityModel.Tokens;
var b = WebApplication.CreateBuilder(args);
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(b.Configuration["Jwt:Key"] ?? "CHANGEZ-CETTE-CLE-SECRETE-AU-MOINS-32-CARACTERES!"));
b.Services.AddDbContext<Db>(o => o.UseSqlite("Data Source=clinic.db"));
b.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => o.TokenValidationParameters = new() { IssuerSigningKey = key, ValidateIssuer = false, ValidateAudience = false });
b.Services.AddAuthorization();
var app = b.Build(); app.UseAuthentication(); app.UseAuthorization();
const string ADM = "Administrateur", DOC = "Médecin", CAI = "Accueil et caisse";
var H = new PasswordHasher<Account>();
using (var s = app.Services.CreateScope()) { var d = s.ServiceProvider.GetRequiredService<Db>(); d.Database.EnsureCreated();
  if (!d.Accounts.Any()) { void A(string n, string e, string p, string r, string c) { var a = new Account { Name = n, Email = e, Role = r, Status = "active", Code = c }; a.Hash = H.HashPassword(a, p); d.Add(a); }
    A("Admin Clinique", "admin@clinique.cm", "admin123", ADM, "CL-0001"); A("Dr Jean Ndzana", "medecin@clinique.cm", "medecin123", DOC, "CL-0002"); A("Clarisse Mbock", "caisse@clinique.cm", "caisse123", CAI, "CL-0003"); d.SaveChanges(); } }
var api = app.MapGroup("/api"); var auth = api.MapGroup("").RequireAuthorization();
string Status(double t, double p) => t - p <= 0 ? "Soldée" : p > 0 ? "Partielle" : "Impayée";
// ---- comptes ----
api.MapPost("/signup", async (Db d, Reg r) => {
  if (string.IsNullOrWhiteSpace(r.Name) || string.IsNullOrWhiteSpace(r.Email) || (r.Password ?? "").Length < 6) return Results.BadRequest("Nom, e-mail et mot de passe (6 caractères min.) requis");
  var e = r.Email.Trim().ToLower(); if (await d.Accounts.AnyAsync(a => a.Email == e)) return Results.Conflict("Cet e-mail est déjà utilisé");
  var a = new Account { Name = r.Name.Trim(), Email = e, Status = "pending", Role = "", Code = "CL-" + Random.Shared.Next(1000, 9999) }; a.Hash = H.HashPassword(a, r.Password);
  d.Add(a); await d.SaveChangesAsync(); return Results.Ok(new { a.Code }); });
api.MapPost("/login", async (Db d, Log l) => {
  var e = (l.Email ?? "").Trim().ToLower(); var a = await d.Accounts.FirstOrDefaultAsync(x => x.Email == e);
  if (a == null || H.VerifyHashedPassword(a, a.Hash, l.Password ?? "") == PasswordVerificationResult.Failed) return Results.Unauthorized();
  if (a.Status != "active") return Results.StatusCode(403);
  var t = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(claims: new[] { new Claim(ClaimTypes.NameIdentifier, a.Id.ToString()), new Claim(ClaimTypes.Role, a.Role) }, expires: DateTime.UtcNow.AddDays(7), signingCredentials: new(key, SecurityAlgorithms.HmacSha256)));
  return Results.Ok(new { token = t, name = a.Name, role = a.Role }); });
api.MapPost("/reset", async (Db d, Rst r) => {
  var e = (r.Email ?? "").Trim().ToLower(); var a = await d.Accounts.FirstOrDefaultAsync(x => x.Email == e && x.Code == r.Code);
  if (a == null || (r.Password ?? "").Length < 6) return Results.BadRequest("E-mail ou code d’invitation incorrect, ou mot de passe trop court");
  a.Hash = H.HashPassword(a, r.Password); await d.SaveChangesAsync(); return Results.Ok(); });
var adm = api.MapGroup("/accounts").RequireAuthorization(p => p.RequireRole(ADM));
adm.MapGet("", async (Db d) => (await d.Accounts.ToListAsync()).Select(a => new { a.Id, a.Name, a.Email, a.Role, a.Status, a.Code }));
adm.MapPost("/{id:int}/validate", async (Db d, int id, Val v) => { var a = await d.Accounts.FindAsync(id); if (a == null || !new[] { ADM, DOC, CAI }.Contains(v.Role)) return Results.BadRequest("Rôle invalide"); a.Role = v.Role; a.Status = "active"; await d.SaveChangesAsync(); return Results.Ok(); });
adm.MapDelete("/{id:int}", async (Db d, int id) => { var a = await d.Accounts.FindAsync(id); if (a != null) { d.Remove(a); await d.SaveChangesAsync(); } return Results.Ok(); });
// ---- aujourd'hui / patients ----
auth.MapGet("/today", async (Db d) => { var v = await d.Visits.Where(x => x.Date == Dates.Today()).OrderBy(x => x.Time).ToListAsync(); var ps = await d.Patients.ToListAsync();
  return new { date = DateTime.Now.ToString("dddd d MMMM yyyy"), patients = v.Count, waiting = v.Count(x => x.Status == "En attente"), received = v.Count(x => x.Status == "Terminé"),
    visits = v.Select(x => { var p = ps.First(q => q.Id == x.PatientId); return new { x.Id, x.PatientId, name = p.Prenom + " " + p.Nom, x.Time, x.Motif, x.Status }; }) }; });
auth.MapGet("/patients", async (Db d, string q) => { q = (q ?? "").ToLower(); var inv = await d.Invoices.ToListAsync();
  return (await d.Patients.ToListAsync()).Where(p => (p.Nom + p.Prenom + p.Tel + p.Num).ToLower().Contains(q)).Select(p => new { p.Id, p.Num, p.Nom, p.Prenom, p.Sexe, p.Tel, balance = inv.Where(i => i.PatientId == p.Id).Sum(i => i.Total - i.Paid) }); });
auth.MapGet("/patients/{id:int}", async (Db d, int id) => { var p = await d.Patients.FindAsync(id); if (p == null) return Results.NotFound();
  var bal = (await d.Invoices.Where(i => i.PatientId == id).ToListAsync()).Sum(i => i.Total - i.Paid); var last = (await d.Consults.Where(c => c.PatientId == id).ToListAsync()).OrderBy(c => c.Date).ThenBy(c => c.Id).LastOrDefault();
  int? age = DateTime.TryParse(p.Naissance, out var n) ? (int)((DateTime.Today - n).TotalDays / 365.25) : null;
  return Results.Ok(new { p.Id, p.Num, p.Nom, p.Prenom, p.Sexe, p.Naissance, age, p.Tel, p.Quartier, p.Groupe, p.Allergies, p.Contact, balance = bal, last = last == null ? null : new { last.Diag, last.Date, last.Motif } }); });
auth.MapPost("/patients", async (Db d, Patient p) => { if (string.IsNullOrWhiteSpace(p.Nom) || string.IsNullOrWhiteSpace(p.Prenom)) return Results.BadRequest("Nom et prénom obligatoires");
  d.Add(p); await d.SaveChangesAsync(); p.Num = $"P-{p.Id:0000}"; d.Add(new Visit { PatientId = p.Id, Time = DateTime.Now.ToString("HH:mm"), Motif = p.Motif ?? "", Status = "En attente", Date = Dates.Today() }); await d.SaveChangesAsync(); return Results.Ok(new { p.Id }); });
// ---- consultations (médecin / admin) ----
var med = auth.MapGroup("/patients/{id:int}/consults").RequireAuthorization(p => p.RequireRole(DOC, ADM));
med.MapGet("", async (Db d, int id) => (await d.Consults.Where(c => c.PatientId == id).ToListAsync()).OrderByDescending(c => c.Date).ThenByDescending(c => c.Id));
med.MapPost("", async (Db d, int id, Consult c) => { if (string.IsNullOrWhiteSpace(c.Motif)) return Results.BadRequest("Indiquez le motif"); c.Id = 0; c.PatientId = id; c.Date = Dates.Today(); d.Add(c);
  foreach (var v in d.Visits.Where(v => v.PatientId == id && v.Date == Dates.Today() && v.Status != "Terminé")) v.Status = "Terminé"; await d.SaveChangesAsync(); return Results.Ok(); });
// ---- facturation (caisse / admin) ----
var cai = auth.MapGroup("/invoices").RequireAuthorization(p => p.RequireRole(CAI, ADM));
cai.MapGet("", async (Db d, string filter) => { var ps = await d.Patients.ToListAsync(); var all = await d.Invoices.OrderByDescending(i => i.Date).ThenByDescending(i => i.Id).ToListAsync();
  var l = all.Where(i => filter == "due" ? i.Total > i.Paid : filter == "ok" ? i.Total <= i.Paid : true);
  return new { paid = all.Sum(i => i.Paid), due = all.Sum(i => i.Total - i.Paid), items = l.Select(i => new { i.Id, i.Num, i.Date, i.Total, i.Paid, name = ps.First(p => p.Id == i.PatientId).Prenom + " " + ps.First(p => p.Id == i.PatientId).Nom, status = Status(i.Total, i.Paid) }) }; });
cai.MapGet("/{id:int}", async (Db d, int id) => { var i = await d.Invoices.FindAsync(id); if (i == null) return Results.NotFound(); var p = await d.Patients.FindAsync(i.PatientId);
  return Results.Ok(new { i.Id, i.Num, i.Date, i.Label, lines = JsonSerializer.Deserialize<List<LineIn>>(i.Lines ?? "[]", new JsonSerializerOptions { PropertyNameCaseInsensitive = true }), i.Total, i.Paid, name = p.Prenom + " " + p.Nom, patientNum = p.Num, status = Status(i.Total, i.Paid) }); });
cai.MapPost("", async (Db d, NewInv n) => { var ls = (n.Lines ?? new()).Where(l => !string.IsNullOrWhiteSpace(l.Label) && l.Amount > 0).ToList();
  if (!ls.Any() || await d.Patients.FindAsync(n.PatientId) == null) return Results.BadRequest("Ajoutez au moins une ligne avec un libellé et un montant");
  var i = new Invoice { PatientId = n.PatientId, Date = Dates.Today(), Label = string.Join(", ", ls.Select(l => l.Label)), Total = ls.Sum(l => l.Amount), Lines = JsonSerializer.Serialize(ls) };
  d.Add(i); await d.SaveChangesAsync(); i.Num = $"F-{DateTime.Now.Year}-{i.Id:000}"; await d.SaveChangesAsync(); return Results.Ok(new { i.Id }); });
cai.MapPost("/{id:int}/pay", async (Db d, int id, Pay p) => { var i = await d.Invoices.FindAsync(id); if (i == null) return Results.NotFound(); if (p.Amount <= 0 || p.Amount > i.Total - i.Paid) return Results.BadRequest("Montant invalide ou supérieur au reste à payer");
  i.Paid += p.Amount; await d.SaveChangesAsync(); return Results.Ok(); });
app.Run("http://0.0.0.0:5000");
record Reg(string Name, string Email, string Password); record Log(string Email, string Password); record Rst(string Email, string Code, string Password); record Val(string Role); record Pay(double Amount); record LineIn(string Label, double Amount); record NewInv(int PatientId, List<LineIn> Lines);
class Account { public int Id { get; set; } public string Name { get; set; } public string Email { get; set; } public string Hash { get; set; } public string Role { get; set; } public string Status { get; set; } public string Code { get; set; } }
class Patient { public int Id { get; set; } public string Num { get; set; } public string Nom { get; set; } public string Prenom { get; set; } public string Sexe { get; set; } public string Naissance { get; set; } public string Tel { get; set; } public string Quartier { get; set; } public string Groupe { get; set; } public string Allergies { get; set; } public string Contact { get; set; }
  [System.ComponentModel.DataAnnotations.Schema.NotMapped] public string Motif { get; set; } }
class Visit { public int Id { get; set; } public int PatientId { get; set; } public string Time { get; set; } public string Motif { get; set; } public string Status { get; set; } public string Date { get; set; } }
class Consult { public int Id { get; set; } public int PatientId { get; set; } public string Date { get; set; } public string Motif { get; set; } public string Temp { get; set; } public string Tension { get; set; } public string Poids { get; set; } public string Diag { get; set; } public string Ordo { get; set; } }
class Invoice { public int Id { get; set; } public string Num { get; set; } public int PatientId { get; set; } public string Date { get; set; } public string Label { get; set; } public string Lines { get; set; } public double Total { get; set; } public double Paid { get; set; } }
class Db(DbContextOptions<Db> o) : DbContext(o) { public DbSet<Account> Accounts { get; set; } public DbSet<Patient> Patients { get; set; } public DbSet<Visit> Visits { get; set; } public DbSet<Consult> Consults { get; set; } public DbSet<Invoice> Invoices { get; set; } }
static class Dates { public static string Today() => DateTime.Now.ToString("yyyy-MM-dd"); }
