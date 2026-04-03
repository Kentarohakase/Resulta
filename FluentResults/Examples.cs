using System;
using System.Threading.Tasks;

using FluentResults;

// ═══════════════════════════════════════════════════════════════════════════════
// FluentResults – Beispiele & Verwendung
// ═══════════════════════════════════════════════════════════════════════════════

class Examples
{
  // ── 1. Einfaches Ok / Fail ───────────────────────────────────────────────

  static Result<int> Dividieren(int a, int b)
  {
    if (b == 0)
      return Result.Fail<int>(new Error("Division durch Null!").WithCode("DIV_ZERO"));

    return Result.Ok(a / b);
  }

  static void BeispielEinfach()
  {
    var result = Dividieren(10, 2);

    // Match – erzwingt beide Fälle zu behandeln
    string ausgabe = result.Match(
        onSuccess: wert => $"Ergebnis: {wert}",
        onFailure: err => $"Fehler: {err.Message}"
    );

    Console.WriteLine(ausgabe); // "Ergebnis: 5"

    // Oder klassisch:
    if (result.IsSuccess)
      Console.WriteLine($"Wert: {result.Value}");
    else
      Console.WriteLine($"Fehler [{result.Error.Code}]: {result.Error.Message}");
  }

  // ── 2. Map & Bind – Verketten ────────────────────────────────────────────

  record User(int Id, string Name, string Email);
  record UserDto(string Name, string Email);

  static Result<User> UserLaden(int id)
  {
    if (id <= 0) return Error.Validation("id", "Muss größer als 0 sein");

    // Simulierter DB-Aufruf
    return new User(id, "Max Mustermann", "max@beispiel.at");
  }

  static Result<UserDto> UserZuDtoKonvertieren(User user)
  {
    if (string.IsNullOrEmpty(user.Email))
      return Error.Validation("email", "E-Mail darf nicht leer sein");

    return new UserDto(user.Name, user.Email);
  }

  static void BeispielKetten()
  {
    var dto = UserLaden(1)
        .Bind(UserZuDtoKonvertieren)         // nur ausgeführt wenn Ok
        .Ensure(d => d.Email.Contains('@'), "Keine gültige E-Mail")
        .Map(d => d with { Name = d.Name.Trim() }); // Wert transformieren

    dto.Match(
        onSuccess: d => Console.WriteLine($"User: {d.Name} <{d.Email}>"),
        onFailure: err => Console.WriteLine($"Fehler: {err.ToDetailedString()}")
    );
  }

  // ── 3. Tap / OnSuccess / OnFailure ───────────────────────────────────────

  static void BeispielTap()
  {
    UserLaden(42)
        .OnSuccess(user => Console.WriteLine($"✅ Geladen: {user.Name}"))
        .OnFailure(err => Console.WriteLine($"❌ Fehler: {err.Message}"))
        .Map(user => user.Email); // Weiter mappen
  }

  // ── 4. Try – Exceptions abfangen ────────────────────────────────────────

  static void BeispielTry()
  {
    var result = ResultExtensions.Try(() =>
    {
      // Hier könnte eine Exception fliegen
      return int.Parse("keine zahl");
    }, ex => new Error($"Parsing fehlgeschlagen: {ex.Message}").WithCode("PARSE_ERROR"));

    Console.WriteLine(result); // "Result<Int32> { Fail: Parsing fehlgeschlagen... }"
  }

  // ── 5. Combine – Mehrere Results zusammenführen ──────────────────────────

  static void BeispielCombine()
  {
    var results = new[]
    {
            Result.Ok<int>(1),
            Result.Fail<int>("Fehler A"),
            Result.Ok<int>(3),
            Result.Fail<int>("Fehler B"),
        };

    var combined = ResultExtensions.Combine(results);

    combined.Match(
        onSuccess: werte => Console.WriteLine($"Alle Werte: {string.Join(", ", werte)}"),
        onFailure: err => Console.WriteLine($"Fehler: {err.ToDetailedString()}")
    );
  }

  // ── 6. Async ─────────────────────────────────────────────────────────────

  static async Task<Result<string>> EmailLadenAsync(int userId)
  {
    await Task.Delay(10); // Simulierter async DB-Aufruf
    if (userId == 0) return Error.NotFound("User");
    return "max@beispiel.at";
  }

  // NEU (richtig) ✅
  static async Task BeispielAsync()
  {
    // EmailLadenAsync gibt schon Result<string> zurück → direkt awaiten
    var result = await EmailLadenAsync(1);

    var laenge = await result.MapAsync(async email =>
    {
      await Task.Delay(5);
      return email.Length;  // email ist jetzt string ✅
    });

    Console.WriteLine(laenge); // Result<Int32> { Ok: 16 }
  }

  // ── 7. Vordefinierte Fehler-Factories ────────────────────────────────────

  static void BeispielFehlerFactories()
  {
    var notFound = Error.NotFound("Produkt");
    var unauth = Error.Unauthorized("Kein Admin-Zugriff");
    var validation = Error.Validation("preis", "Muss größer als 0 sein");
    var conflict = Error.Conflict("Produkt mit diesem Namen existiert bereits");

    Console.WriteLine(notFound);   // "'Produkt' wurde nicht gefunden. [NOT_FOUND]"
    Console.WriteLine(validation); // "Validierungsfehler bei 'preis': ... [VALIDATION_ERROR]"
  }

  // ── 8. Implicit Conversions ───────────────────────────────────────────────

  // Wert direkt zurückgeben ohne Result.Ok() zu schreiben
  static Result<string> GetName() => "Max Mustermann";

  // Fehler direkt zurückgeben ohne Result.Fail() zu schreiben
  static Result<string> GetNameFehler() => Error.NotFound("Name");

  // ── 9. ASP.NET Core Controller Beispiel ──────────────────────────────────
  /*
  [ApiController]
  [Route("api/users")]
  public class UserController : ControllerBase
  {
      [HttpGet("{id}")]
      public IActionResult GetUser(int id)
      {
          return UserLaden(id).Match<IActionResult>(
              onSuccess: user => Ok(user),
              onFailure: err  => err.Code switch
              {
                  "NOT_FOUND"        => NotFound(err.Message),
                  "VALIDATION_ERROR" => BadRequest(err.Message),
                  "UNAUTHORIZED"     => Unauthorized(err.Message),
                  _                  => StatusCode(500, err.Message)
              }
          );
      }
  }
  */

  static async Task Main()
  {
    Console.WriteLine("=== FluentResults Beispiele ===\n");

    Console.WriteLine("── 1. Einfach ──");
    BeispielEinfach();

    Console.WriteLine("\n── 2. Ketten ──");
    BeispielKetten();

    Console.WriteLine("\n── 3. Tap ──");
    BeispielTap();

    Console.WriteLine("\n── 4. Try ──");
    BeispielTry();

    Console.WriteLine("\n── 5. Combine ──");
    BeispielCombine();

    Console.WriteLine("\n── 6. Async ──");
    await BeispielAsync();

    Console.WriteLine("\n── 7. Fehler-Factories ──");
    BeispielFehlerFactories();
  }
}
