using System;
using System.Threading.Tasks;

using Resulta;
using Resulta.Extensions;

namespace Resulta.Samples;

internal static class Examples
{
  private sealed record User(int Id, string Name, string Email, bool IsActive);
  private sealed record UserDto(string Name, string Email);

  private static Result<int> Divide(int a, int b)
  {
    if (b == 0)
      return Result.Fail<int>(new Error("Division durch Null!").WithCode("DIV_ZERO"));

    return Result.Ok(a / b);
  }

  private static void ExampleBasic()
  {
    var result = Divide(10, 2);

    string output = result.Match(
        onSuccess: value => $"Ergebnis: {value}",
        onFailure: err => $"Fehler: {err.Message}"
    );

    Console.WriteLine(output);

    if (result.IsSuccess)
      Console.WriteLine($"Wert: {result.Value}");
    else
      Console.WriteLine($"Fehler [{result.Error.Code}]: {result.Error.Message}");
  }

  private static Result<User> LoadUser(int id)
  {
    if (id <= 0)
      return Error.Validation("id", "Muss größer als 0 sein");

    return new User(id, "Max Mustermann", "max@beispiel.at", true);
  }

  private static Result<UserDto> ConvertUserToDto(User user)
  {
    if (string.IsNullOrWhiteSpace(user.Email))
      return Error.Validation("email", "E-Mail darf nicht leer sein");

    return new UserDto(user.Name, user.Email);
  }

  private static void ExampleChain()
  {
    var dto = LoadUser(1)
        .Bind(ConvertUserToDto)
        .Ensure(d => d.Email.Contains('@'), "Keine gültige E-Mail")
        .Map(d => d with { Name = d.Name.Trim() });

    dto.Match(
        onSuccess: d => Console.WriteLine($"User: {d.Name} <{d.Email}>"),
        onFailure: err => Console.WriteLine($"Fehler: {err.ToDetailedString()}")
    );
  }

  private static void ExampleTap()
  {
    var emailResult = LoadUser(42)
        .OnSuccess(user => Console.WriteLine($"Geladen: {user.Name}"))
        .OnFailure(err => Console.WriteLine($"Fehler: {err.Message}"))
        .Map(user => user.Email);

    Console.WriteLine(emailResult);
  }

  private static void ExampleTry()
  {
    var result = ResultExtensions.Try(
        () => int.Parse("keine zahl"),
        ex => new Error($"Parsing fehlgeschlagen: {ex.Message}").WithCode("PARSE_ERROR")
    );

    Console.WriteLine(result);
  }

  private static async Task ExampleTryAsync()
  {
    var result = await ResultExtensions.TryAsync(
        async () =>
        {
          await Task.Delay(10);
          return int.Parse("123");
        },
        ex => new Error($"Async parsing fehlgeschlagen: {ex.Message}").WithCode("PARSE_ERROR")
    );

    Console.WriteLine(result);
  }

  private static void ExampleCombine()
  {
    Result<int>[] results =
    {
          Result.Ok(1),
          Result.Fail<int>("Fehler A"),
          Result.Ok(3),
          Result.Fail<int>("Fehler B")
      };

    var combined = ResultExtensions.Combine(results);

    combined.Match(
        onSuccess: values => Console.WriteLine($"Alle Werte: {string.Join(", ", values)}"),
        onFailure: err => Console.WriteLine($"Fehler: {err.ToDetailedString()}")
    );
  }

  private static async Task<Result<string>> LoadEmailAsync(int userId)
  {
    await Task.Delay(10);

    if (userId == 0)
      return Error.NotFound("User");

    return "max@beispiel.at";
  }

  private static async Task ExampleAsync()
  {
    var result = await LoadEmailAsync(1);

    var lengthResult = await result.MapAsync(async email =>
    {
      await Task.Delay(5);
      return email.Length;
    });

    Console.WriteLine(lengthResult);
  }

  private static void ExampleErrorFactories()
  {
    var notFound = Error.NotFound("Produkt");
    var unauthorized = Error.Unauthorized("Kein Admin-Zugriff");
    var validation = Error.Validation("preis", "Muss größer als 0 sein");
    var conflict = Error.Conflict("Produkt mit diesem Namen existiert bereits");

    Console.WriteLine(notFound);
    Console.WriteLine(unauthorized);
    Console.WriteLine(validation);
    Console.WriteLine(conflict);
  }

  private static void ExampleValidation()
  {
    var input = new UserDto("", "ungueltig");

    var validation = Validator<UserDto>
        .For(input)
        .Must(x => !string.IsNullOrWhiteSpace(x.Name), Error.Validation("name", "Name ist erforderlich"))
        .Must(x => x.Email.Contains("@"), Error.Validation("email", "E-Mail ist ungültig"))
        .Validate();

    validation.Match(
        onSuccess: value =>
        {
          Console.WriteLine($"Validiert: {value.Name} <{value.Email}>");
          return 0;
        },
        onFailure: errors =>
        {
          Console.WriteLine($"Validierungsfehler: {errors.Count}");
          foreach (var error in errors)
            Console.WriteLine($" - {error.Message}");
          return 0;
        }
    );
  }

  private static Result<User> EnsureActive(User user)
  {
    if (!user.IsActive)
      return Error.Unauthorized("Benutzer ist nicht aktiv");

    return user;
  }

  private static Result<string> CreateToken(User user)
  {
    if (user.Id <= 0)
      return new Error("Ungültiger Benutzerzustand").WithCode("UNEXPECTED_ERROR");

    return $"token-for-user-{user.Id}";
  }

  private static void ExamplePipeline()
  {
    var message = Pipeline<string>
        .Start("max")
        .Validate(x => !string.IsNullOrWhiteSpace(x), "Username darf nicht leer sein")
        .Then(username => username.Trim())
        .Then(_ => LoadUser(1))
        .Then(EnsureActive)
        .Tap(user => Console.WriteLine($"Pipeline User: {user.Name}"))
        .Then(CreateToken)
        .Finally(
            onSuccess: token => $"Token erstellt: {token}",
            onFailure: err => $"Pipeline-Fehler: {err.Message}"
        );

    Console.WriteLine(message);
  }

  private static async Task<Result<User>> LoadUserAsync(int id)
  {
    await Task.Delay(10);
    return LoadUser(id);
  }

  private static async Task<Result<User>> EnsureActiveAsync(User user)
  {
    await Task.Delay(10);
    return EnsureActive(user);
  }

  private static async Task<Result<string>> CreateTokenAsync(User user)
  {
    await Task.Delay(10);
    return CreateToken(user);
  }

  private static async Task ExampleAsyncPipeline()
  {
    var message = await AsyncPipeline<User>
        .Start(() => LoadUserAsync(1))
        .ThenAsync(EnsureActiveAsync)
        .ThenAsync(CreateTokenAsync)
        .Finally(
            onSuccess: token => $"Async Token erstellt: {token}",
            onFailure: err => $"Async Pipeline-Fehler: {err.Message}"
        );

    Console.WriteLine(message);
  }

  internal static async Task RunAll()
  {
    Console.WriteLine("=== Resulta Beispiele ===");

    Console.WriteLine("\n1. Basic");
    ExampleBasic();

    Console.WriteLine("\n2. Chain");
    ExampleChain();

    Console.WriteLine("\n3. Tap");
    ExampleTap();

    Console.WriteLine("\n4. Try");
    ExampleTry();

    Console.WriteLine("\n5. TryAsync");
    await ExampleTryAsync();

    Console.WriteLine("\n6. Combine");
    ExampleCombine();

    Console.WriteLine("\n7. Async");
    await ExampleAsync();

    Console.WriteLine("\n8. Error Factories");
    ExampleErrorFactories();

    Console.WriteLine("\n9. Validation");
    ExampleValidation();

    Console.WriteLine("\n10. Pipeline");
    ExamplePipeline();

    Console.WriteLine("\n11. Async Pipeline");
    await ExampleAsyncPipeline();
  }
}
