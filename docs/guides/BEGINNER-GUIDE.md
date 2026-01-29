# Guia para Principiantes - SecureTransact API

> Una guia completa para desarrolladores junior y estudiantes que quieren entender como funciona este proyecto desde cero.

---

## Tabla de Contenido

1. [Introduccion](#1-introduccion)
2. [Conceptos Fundamentales](#2-conceptos-fundamentales)
3. [Recorrido Paso a Paso de una Transaccion](#3-recorrido-paso-a-paso-de-una-transaccion)
4. [La Seguridad Explicada](#4-la-seguridad-explicada)
5. [Como se Construyo el Proyecto Paso a Paso](#5-como-se-construyo-el-proyecto-paso-a-paso)
6. [Glosario Rapido](#6-glosario-rapido)

---

## 1. Introduccion

### Que es SecureTransact API?

SecureTransact API es una **implementacion de referencia** creada por **MancoMen Software Studio** que demuestra como construir un sistema de procesamiento de transacciones financieras seguro y de nivel empresarial.

Piensa en este proyecto como un **libro de texto con codigo real**: no es una aplicacion para produccion directa, sino un ejemplo de como se deberian construir sistemas financieros utilizando las mejores practicas de la industria.

### Por que existe este proyecto?

En el mundo real, los sistemas financieros necesitan cumplir requisitos muy estrictos:

- **Seguridad**: Cada transaccion debe estar protegida criptograficamente.
- **Auditoria**: Debe existir un registro completo e inmutable de todo lo que paso.
- **Integridad**: Nadie debe poder modificar un registro historico sin que se detecte.
- **Mantenibilidad**: El codigo debe estar organizado para que equipos grandes puedan trabajar sin pisarse.

Este proyecto muestra como lograr todo eso usando patrones de arquitectura modernos.

### Tecnologias que usa

| Tecnologia | Para que sirve | Version |
|---|---|---|
| **.NET** | Runtime y plataforma de desarrollo | 10.0 |
| **C#** | Lenguaje de programacion | Preview |
| **ASP.NET Core Minimal APIs** | Framework web para los endpoints HTTP | 10.0 |
| **Entity Framework Core** | ORM para acceso a base de datos | 10.0 |
| **PostgreSQL** | Base de datos relacional principal | 17 |
| **Redis** | Cache en memoria para rendimiento | 7.4 |
| **MediatR** | Mediador para implementar CQRS | 12.x |
| **FluentValidation** | Validacion declarativa de datos | 11.x |
| **OpenTelemetry** | Observabilidad (metricas, trazas, logs) | 1.11.x |
| **xUnit + NSubstitute + FluentAssertions** | Testing | Ultimas versiones |

---

## 2. Conceptos Fundamentales

No te preocupes si estos conceptos suenan complicados. Cada uno viene con una **analogia simple**, una **explicacion tecnica breve** y un **ejemplo real del proyecto**.

---

### 2.1 Clean Architecture (Arquitectura Limpia)

**Analogia**: Imagina un edificio de 4 pisos. El primer piso (la base) es la estructura mas importante: los cimientos. Cada piso superior depende de lo que esta abajo, pero **nunca al reves**. Los cimientos no saben nada sobre los pisos de arriba. Si remodelamos el cuarto piso, los cimientos no se ven afectados.

**Explicacion tecnica**: Clean Architecture organiza el codigo en capas concentricas donde las dependencias siempre apuntan hacia adentro. La capa mas interna (Domain) no conoce nada del mundo exterior.

**Como se ve en el proyecto**:

```
┌─────────────────────────────────────────────────┐
│  API Layer (Piso 4 - La entrada del edificio)   │
│  Endpoints HTTP, Middleware, Configuracion       │
└─────────────────────────────────────────────────┘
                      │ depende de
┌─────────────────────────────────────────────────┐
│  Application Layer (Piso 3 - Las oficinas)      │
│  Commands, Queries, Handlers, Validadores        │
└─────────────────────────────────────────────────┘
                      │ depende de
┌─────────────────────────────────────────────────┐
│  Domain Layer (Piso 1 - Los cimientos)          │
│  Aggregates, Value Objects, Events, Interfaces   │
│  ** CERO dependencias externas **                │
└─────────────────────────────────────────────────┘
                      │ implementado por
┌─────────────────────────────────────────────────┐
│  Infrastructure Layer (Las tuberias y cables)    │
│  Base de datos, Criptografia, Cache, EventStore  │
└─────────────────────────────────────────────────┘
```

Las reglas estrictas son:

1. **Domain no depende de nada externo** -- cero paquetes NuGet fuera de la biblioteca estandar de .NET.
2. **Application solo referencia a Domain** -- nunca a Infrastructure.
3. **Infrastructure implementa las interfaces** definidas en Domain y Application.
4. **API es el punto de entrada** que conecta todo.

---

### 2.2 Domain-Driven Design (DDD)

**Analogia**: Imagina que un equipo de desarrollo trabaja con un equipo de negocios de un banco. Si los desarrolladores dicen "entidad" y los del banco dicen "cuenta", van a haber malentendidos. DDD dice: **todos debemos hablar el mismo idioma**. Si el banco dice "transaccion", "cuenta" y "dinero", el codigo debe usar exactamente esas palabras.

**Explicacion tecnica**: Domain-Driven Design es una metodologia donde el modelo de codigo refleja directamente los conceptos del negocio. A este vocabulario compartido se le llama **Ubiquitous Language** (lenguaje ubicuo).

**Como se ve en el proyecto**: Los nombres de las clases reflejan exactamente los conceptos de negocio:

- `TransactionAggregate` -- una transaccion financiera.
- `Money` -- dinero con moneda y monto.
- `AccountId` -- identificador de una cuenta.
- `TransactionStatus` -- el estado de una transaccion (Initiated, Authorized, Completed, etc.).

---

### 2.3 Value Objects (Objetos de Valor)

**Analogia**: Piensa en un billete de $50,000 COP. Lo que importa es el **valor**: 50,000 pesos colombianos. Si tienes dos billetes de $50,000, son **intercambiables**. No te importa cual billete especifico es, solo que valga lo mismo. Si cambias algo del billete (por ejemplo, lo rompes), ya no es el mismo valor: necesitas uno nuevo.

**Explicacion tecnica**: Un Value Object es inmutable (no se puede modificar despues de crearse) y se compara por su valor, no por una identidad. Si dos Value Objects tienen los mismos datos, son iguales.

**Ejemplo real del proyecto** -- `Money`:

```csharp
public sealed record Money : IComparable<Money>
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    // Constructor privado: solo se puede crear a traves de Create()
    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    // Factory method con validacion
    public static Result<Money> Create(decimal amount, Currency currency)
    {
        if (currency is null)
        {
            return Result.Failure<Money>(MoneyErrors.InvalidCurrency);
        }

        if (amount < 0)
        {
            return Result.Failure<Money>(MoneyErrors.NegativeAmount);
        }

        decimal roundedAmount = Math.Round(amount, currency.DecimalPlaces, MidpointRounding.ToEven);
        return Result.Success(new Money(roundedAmount, currency));
    }
}
```

Observa estos detalles importantes:

- Es `sealed record` -- inmutable por naturaleza.
- El constructor es `private` -- no puedes crear un `Money` directamente con `new`.
- Para crear uno, usas `Money.Create(...)` que **valida** los datos y devuelve un `Result` (exito o error).
- Nunca existira un `Money` con monto negativo o sin moneda valida.

**Otros Value Objects del proyecto**: `Currency`, `TransactionId`, `AccountId`, `TransactionStatus`.

---

### 2.4 Entities (Entidades)

**Analogia**: Piensa en una persona con cedula de identidad. Aunque esa persona cambie de nombre, de direccion o de trabajo, sigue siendo **la misma persona** porque tiene un numero de cedula unico. Lo que importa es su **identidad**, no sus atributos.

**Explicacion tecnica**: Una Entity tiene un identificador unico (`Id`) y se compara por ese identificador, no por sus propiedades. Dos entidades con el mismo `Id` son la misma, sin importar si sus datos cambiaron.

**Ejemplo real del proyecto** -- `Entity<TId>`:

```csharp
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }
}
```

La clave esta en el metodo `Equals`: solo compara el `Id`, no las demas propiedades.

---

### 2.5 Aggregates (Agregados)

**Analogia**: Piensa en una familia. Los miembros de la familia cambian juntos (se mudan juntos, comparten reglas del hogar). Hay un "lider" que es responsable de aplicar las reglas de la familia y asegurar que todo sea consistente. Si alguien de afuera quiere interactuar con la familia, habla con el lider.

**Explicacion tecnica**: Un Aggregate es un grupo de objetos de dominio que se tratan como una unidad. Tiene un **Aggregate Root** (raiz) que es el unico punto de acceso desde afuera. El Aggregate Root garantiza que todas las reglas de negocio se cumplan.

**Ejemplo real del proyecto** -- `TransactionAggregate`:

```csharp
public sealed class TransactionAggregate : AggregateRoot<TransactionId>
{
    public AccountId SourceAccountId { get; private set; }
    public AccountId DestinationAccountId { get; private set; }
    public Money Amount { get; private set; } = null!;
    public TransactionStatus Status { get; private set; } = null!;

    // Solo se puede crear a traves del factory method
    public static Result<TransactionAggregate> Create(
        AccountId sourceAccountId,
        AccountId destinationAccountId,
        Money amount,
        string? reference = null)
    {
        if (sourceAccountId == destinationAccountId)
        {
            return Result.Failure<TransactionAggregate>(TransactionErrors.SameAccount);
        }

        if (amount.IsZero)
        {
            return Result.Failure<TransactionAggregate>(TransactionErrors.InvalidAmount);
        }

        TransactionAggregate transaction = new()
        {
            Id = TransactionId.New(),
            SourceAccountId = sourceAccountId,
            DestinationAccountId = destinationAccountId,
            Amount = amount,
            Reference = reference,
            Status = TransactionStatus.Initiated,
            InitiatedAtUtc = DateTime.UtcNow
        };

        // Levanta un evento de dominio
        transaction.RaiseDomainEvent(new TransactionInitiatedEvent
        {
            TransactionId = transaction.Id,
            SourceAccountId = sourceAccountId,
            DestinationAccountId = destinationAccountId,
            Amount = amount,
            Reference = reference
        });

        return Result.Success(transaction);
    }
}
```

Nota como el Aggregate Root:

- Tiene `private set` en todas las propiedades -- nadie externo puede cambiarlas directamente.
- Valida las reglas de negocio dentro de sus metodos (cuenta origen distinta a destino, monto no puede ser cero).
- Controla sus propias **transiciones de estado** (Initiated -> Authorized -> Completed).
- **Levanta Domain Events** cuando algo importante pasa.

---

### 2.6 Domain Events (Eventos de Dominio)

**Analogia**: Piensa en un recibo de compra. Cuando compras algo, te dan un recibo que dice "a las 3:15pm, compraste X por Y pesos". Ese recibo es un **registro de que algo paso**. No puedes cambiarlo, no puedes deshacerlo: simplemente registra un hecho.

**Explicacion tecnica**: Un Domain Event es un objeto inmutable que representa algo que ocurrio en el dominio. Se usa para comunicar cambios entre partes del sistema de manera desacoplada.

**Ejemplo real del proyecto** -- `TransactionInitiatedEvent`:

```csharp
public sealed record TransactionInitiatedEvent : DomainEventBase
{
    public TransactionId TransactionId { get; init; }
    public AccountId SourceAccountId { get; init; }
    public AccountId DestinationAccountId { get; init; }
    public Money Amount { get; init; } = null!;
    public string? Reference { get; init; }
}
```

**Los eventos del ciclo de vida de una transaccion son**:

| Evento | Cuando ocurre |
|---|---|
| `TransactionInitiatedEvent` | Se crea la transaccion |
| `TransactionAuthorizedEvent` | Se autoriza el pago |
| `TransactionCompletedEvent` | Se completa exitosamente |
| `TransactionFailedEvent` | Algo fallo |
| `TransactionReversedEvent` | Se revierte una transaccion completada |
| `TransactionDisputedEvent` | Se disputa la transaccion |

---

### 2.7 CQRS (Command Query Responsibility Segregation)

**Analogia**: Imagina un banco con ventanillas separadas. En la **ventanilla de operaciones** haces depositos, retiros y transferencias (operaciones que **cambian** algo). En la **ventanilla de consultas** preguntas tu saldo o pides un extracto (solo **lees** informacion). Separar estas responsabilidades hace que cada ventanilla sea mas eficiente y mas facil de manejar.

**Explicacion tecnica**: CQRS separa las operaciones de escritura (Commands) de las operaciones de lectura (Queries). Cada una puede tener su propio modelo, su propia validacion y su propia optimizacion.

**Ejemplo real del proyecto**:

**Commands** (cambian el estado):

```csharp
// Comando para procesar una transaccion nueva
public sealed record ProcessTransactionCommand : ICommand<TransactionResponse>
{
    public required Guid SourceAccountId { get; init; }
    public required Guid DestinationAccountId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public string? Reference { get; init; }
}
```

**Queries** (solo leen):

```
GetTransactionByIdQuery     --> Obtener una transaccion por su ID
GetTransactionHistoryQuery  --> Obtener el historial de transacciones de una cuenta
```

El **mediador** (MediatR) se encarga de enviar cada Command o Query al Handler correcto. El codigo que envia el comando no necesita saber quien lo procesa.

---

### 2.8 Event Sourcing

**Analogia**: Piensa en la diferencia entre **un saldo bancario** y **un extracto bancario completo**. Si solo guardas el saldo, sabes que tienes $100,000, pero no sabes como llegaste ahi. Con el extracto completo, tienes **cada deposito, cada retiro, cada transferencia** -- puedes reconstruir el saldo en cualquier punto del tiempo.

**Explicacion tecnica**: En vez de guardar solo el estado actual de un objeto, Event Sourcing guarda **todos los eventos que le ocurrieron**. Para obtener el estado actual, se "reproducen" todos los eventos desde el principio.

**Como se ve en el proyecto**: Cada transaccion se guarda como una secuencia de eventos en el Event Store. Para reconstruir una transaccion, se cargan todos sus eventos y se aplican en orden:

```csharp
// Reconstruir un aggregate desde su historial de eventos
public static TransactionAggregate LoadFromHistory(IEnumerable<IDomainEvent> events)
{
    TransactionAggregate aggregate = new();
    foreach (IDomainEvent @event in events)
    {
        aggregate.Apply(@event);
    }
    return aggregate;
}
```

Ademas, cada evento esta **encadenado criptograficamente** (hash chaining) para que si alguien modifica un evento pasado, la cadena se rompa y se detecte la alteracion. Esto se explica en detalle en la seccion de seguridad.

---

### 2.9 Result Pattern (Patron Resultado)

**Analogia**: Imagina un sobre que puede contener una de dos cosas: **el documento que pediste** O **una nota explicando por que no te lo pueden dar**. Nunca contiene ambos, y siempre contiene uno. No te lanza el sobre a la cara (excepcion) -- simplemente lo abres y verificas que hay adentro.

**Explicacion tecnica**: El Result Pattern reemplaza las excepciones para errores **esperados** del negocio. En vez de lanzar una excepcion cuando un monto es negativo, retorna un `Result.Failure` con un error descriptivo. Las excepciones se reservan para situaciones **inesperadas** (base de datos caida, error de red, etc.).

**Ejemplo real del proyecto**:

```csharp
public class Result<TValue> : Result
{
    public TValue Value { get; }  // Solo accesible si IsSuccess es true

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public DomainError Error { get; }
}
```

**Asi se usa en la practica**:

```csharp
// Crear dinero -- puede fallar si el monto es negativo o la moneda no existe
Result<Money> moneyResult = Money.Create(request.Amount, request.Currency);

if (moneyResult.IsFailure)
{
    // No se pudo crear: el error explica por que
    return Result.Failure<TransactionResponse>(moneyResult.Error);
}

// Todo bien: podemos usar el valor
Money money = moneyResult.Value;
```

Ventajas sobre excepciones:

- Es **explicito**: al ver `Result<Money>` como tipo de retorno, sabes que la operacion puede fallar.
- No hay "sorpresas": no necesitas adivinar que excepciones podria lanzar un metodo.
- Es mas rapido: crear un objeto `Result` es mucho mas barato que lanzar y capturar una excepcion.

---

### 2.10 Repository Pattern y Unit of Work

**Analogia**: El **Repository** es como un archivero donde guardas y buscas documentos. Le dices "guarda esta transaccion" o "dame la transaccion con este ID" y el se encarga de los detalles (en que cajon esta, como esta organizado). El **Unit of Work** es como un empleado que se asegura de que todos los archivos relacionados se guarden al mismo tiempo: o se guardan todos, o no se guarda ninguno.

**Explicacion tecnica**: El Repository abstrae el acceso a datos -- tu codigo de dominio no sabe si los datos estan en PostgreSQL, en memoria, o en un archivo. El Unit of Work agrupa multiples operaciones en una sola transaccion atomica.

**En el proyecto**:

- `ITransactionRepository` -- interfaz definida en la capa Application para guardar y buscar transacciones.
- `IUnitOfWork` -- interfaz definida en la capa Domain que garantiza la atomicidad.
- Las implementaciones reales (`TransactionRepository`, `UnitOfWork`) estan en la capa Infrastructure.

Esto significa que puedes cambiar de PostgreSQL a otra base de datos sin tocar el codigo de Domain ni de Application.

---

## 3. Recorrido Paso a Paso de una Transaccion

Vamos a seguir exactamente lo que pasa cuando un cliente envia una solicitud HTTP para crear una nueva transaccion. Cada paso esta numerado para que puedas seguir el flujo.

### Paso 1: La solicitud HTTP llega al endpoint

Un cliente envia un `POST /api/v1/transactions` con este cuerpo JSON:

```json
{
    "sourceAccountId": "a1b2c3d4-...",
    "destinationAccountId": "e5f6g7h8-...",
    "amount": 150000.00,
    "currency": "COP",
    "reference": "Pago de servicios"
}
```

El endpoint esta definido en `TransactionEndpoints.cs`:

```csharp
group.MapPost("/", ProcessTransaction)
    .WithName("ProcessTransaction")
    .Produces<TransactionResponse>(StatusCodes.Status201Created)
    .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest);
```

### Paso 2: Se crea el Command y se envia por MediatR

El endpoint convierte la solicitud HTTP en un `ProcessTransactionCommand` y lo envia a traves de MediatR:

```csharp
ProcessTransactionCommand command = new()
{
    SourceAccountId = request.SourceAccountId,
    DestinationAccountId = request.DestinationAccountId,
    Amount = request.Amount,
    Currency = request.Currency,
    Reference = request.Reference
};

Result<TransactionResponse> result = await sender.Send(command, cancellationToken);
```

### Paso 3: ValidationBehavior valida los datos

Antes de que el Handler reciba el comando, el `ValidationBehavior` intercepta la solicitud y ejecuta todas las validaciones definidas en `ProcessTransactionCommandValidator`:

```csharp
public sealed class ProcessTransactionCommandValidator
    : AbstractValidator<ProcessTransactionCommand>
{
    public ProcessTransactionCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .Must(BeASupportedCurrency)
            .WithMessage("Currency is not supported.");
    }
}
```

Si alguna validacion falla, se retorna un `Result.Failure` inmediatamente **sin ejecutar el Handler**. Esto es eficiente porque evita procesamiento innecesario.

### Paso 4: LoggingBehavior registra la solicitud

El `LoggingBehavior` registra que se esta procesando el comando, mide el tiempo de ejecucion y registra si fue exitoso o fallo:

```csharp
LogHandlingRequest(_logger, requestName);      // "Handling ProcessTransactionCommand"
Stopwatch stopwatch = Stopwatch.StartNew();

TResponse response = await next();             // Ejecuta el siguiente paso

stopwatch.Stop();
LogRequestSucceeded(_logger, requestName, stopwatch.ElapsedMilliseconds);
// "Handled ProcessTransactionCommand successfully in 45ms"
```

### Paso 5: El Handler ejecuta la logica de negocio

`ProcessTransactionCommandHandler` toma el control. Veamos paso a paso lo que hace:

#### 5a. Crear el Value Object Money

```csharp
Result<Money> moneyResult = Money.Create(request.Amount, request.Currency);
if (moneyResult.IsFailure)
{
    return Result.Failure<TransactionResponse>(moneyResult.Error);
}
```

`Money.Create(150000.00, "COP")` valida que el monto no sea negativo y que "COP" sea una moneda soportada. El monto se redondea segun los decimales de la moneda.

#### 5b. Crear el TransactionAggregate

```csharp
Result<TransactionAggregate> transactionResult = TransactionAggregate.Create(
    AccountId.From(request.SourceAccountId),
    AccountId.From(request.DestinationAccountId),
    moneyResult.Value,
    request.Reference);
```

Aqui se valida que la cuenta origen sea diferente a la destino y que el monto no sea cero. Si todo esta bien, se crea el aggregate con estado `Initiated` y se levanta un `TransactionInitiatedEvent`.

#### 5c. Autorizar la transaccion

```csharp
string authorizationCode = GenerateAuthorizationCode();
Result authorizeResult = transaction.Authorize(authorizationCode);
```

La transaccion cambia de estado `Initiated` a `Authorized`. El `TransactionStatus` valida que esta transicion sea permitida (un Smart Enum con transiciones definidas). Se levanta un `TransactionAuthorizedEvent`.

#### 5d. Completar la transaccion

```csharp
Result completeResult = transaction.Complete();
```

La transaccion cambia de `Authorized` a `Completed`. Se levanta un `TransactionCompletedEvent`.

### Paso 6: Persistir en el Event Store

```csharp
await _transactionRepository.AddAsync(transaction, cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);
```

El repositorio pasa los Domain Events al Event Store, donde cada evento es:

1. **Serializado** a JSON.
2. **Encriptado** con AES-256-GCM (cada evento recibe un nonce unico de 12 bytes).
3. **Encadenado** con HMAC-SHA512 -- el hash de cada evento incluye el hash del evento anterior.
4. **Guardado** en PostgreSQL con su version para control de concurrencia optimista.

### Paso 7: Retornar la respuesta

```csharp
return Result.Success(MapToResponse(transaction));
```

El endpoint convierte el resultado en una respuesta HTTP `201 Created`:

```csharp
if (result.IsFailure)
{
    return MapErrorToResult(result.Error);  // 400, 404, 409, etc.
}

return Results.Created($"/api/v1/transactions/{result.Value.TransactionId}", result.Value);
```

### Diagrama del flujo completo

```
Cliente HTTP
    │
    ▼
TransactionEndpoints.ProcessTransaction()
    │
    ▼
MediatR.Send(ProcessTransactionCommand)
    │
    ├──▶ ValidationBehavior ──▶ FluentValidation ──▶ Fallo? Return error
    │
    ├──▶ LoggingBehavior ──▶ Registra inicio, mide tiempo
    │
    ▼
ProcessTransactionCommandHandler.Handle()
    │
    ├──▶ Money.Create()                    ──▶ Valida monto y moneda
    ├──▶ TransactionAggregate.Create()     ──▶ Valida reglas, levanta evento
    ├──▶ transaction.Authorize()           ──▶ Cambia estado, levanta evento
    ├──▶ transaction.Complete()            ──▶ Cambia estado, levanta evento
    ├──▶ _transactionRepository.AddAsync() ──▶ Guarda en Event Store
    └──▶ _unitOfWork.SaveChangesAsync()    ──▶ Commit atomico
    │
    ▼
201 Created + TransactionResponse JSON
```

---

## 4. La Seguridad Explicada

La seguridad es fundamental en este proyecto. Aqui explicamos cada mecanismo de proteccion con analogias simples.

---

### 4.1 AES-256-GCM (Encriptacion de Eventos)

**Analogia**: Imagina una caja fuerte con una combinacion. Solo alguien que tenga la llave correcta puede abrir la caja y leer el contenido. Pero ademas, la caja tiene un **sello de seguridad**: si alguien intenta forzarla y modificar el contenido, el sello se rompe y sabes que fue alterada.

**Explicacion tecnica**: AES-256-GCM es un algoritmo de **encriptacion autenticada**. Esto significa que:

- **Encripta** los datos (nadie puede leerlos sin la llave).
- **Autentica** los datos (si alguien modifica el texto cifrado, la desencriptacion falla).

**Como funciona en el proyecto**:

```csharp
public byte[] Encrypt(ReadOnlySpan<byte> plaintext)
{
    // 1. Generar un nonce unico de 12 bytes (96 bits)
    byte[] nonce = new byte[NonceSize];
    RandomNumberGenerator.Fill(nonce);       // CSPRNG, nunca System.Random

    byte[] ciphertext = new byte[plaintext.Length];
    byte[] tag = new byte[TagSize];          // Tag de autenticacion de 16 bytes

    // 2. Encriptar con AES-256-GCM
    using AesGcm aes = new(_encryptionKey, TagSize);
    aes.Encrypt(nonce, plaintext, ciphertext, tag);

    // 3. Combinar: [nonce (12 bytes)] [tag (16 bytes)] [texto cifrado]
    byte[] result = new byte[NonceSize + TagSize + ciphertext.Length];
    nonce.CopyTo(result, 0);
    tag.CopyTo(result, NonceSize);
    ciphertext.CopyTo(result, NonceSize + TagSize);

    return result;
}
```

Puntos clave:

- Cada evento recibe un **nonce unico** generado con `RandomNumberGenerator` (generador criptograficamente seguro).
- **Nunca** se reutiliza un nonce -- reutilizarlo romperia la seguridad de GCM por completo.
- El **tag de autenticacion** (16 bytes) permite detectar cualquier modificacion del texto cifrado.

---

### 4.2 HMAC-SHA512 y Hash Chaining (Cadena de Hashes)

**Analogia**: Imagina que cada carta que envias tiene un **sello de cera** unico. Pero ademas, cada sello nuevo incluye una **impresion del sello anterior**. Si alguien abre una carta vieja y la modifica, cuando verifiques la cadena de sellos, la impresion no coincidira y sabras que hubo alteracion. Es como una cadena donde cada eslabon depende del anterior.

**Explicacion tecnica**: HMAC-SHA512 es un algoritmo que produce un "resumen" (hash) de los datos usando una llave secreta. Hash Chaining significa que el hash de cada evento se calcula incluyendo el hash del evento anterior, creando una cadena criptografica inalterable.

**Como funciona en el proyecto**:

```csharp
// Cada evento almacena su hash y el hash del evento anterior
public byte[] ComputeChainHash(byte[]? previousHash, ReadOnlySpan<byte> eventData)
{
    int previousHashLength = previousHash?.Length ?? 0;
    byte[] combined = new byte[previousHashLength + eventData.Length];

    if (previousHash != null)
    {
        previousHash.CopyTo(combined, 0);
    }

    eventData.CopyTo(combined.AsSpan(previousHashLength));

    return ComputeHmac(combined);  // HMAC-SHA512(previousHash + eventData)
}
```

Y cuando se leen los eventos, se verifica la cadena:

```csharp
// Al leer cada evento, recalcular el hash y comparar
byte[] expectedHash = _cryptoService.ComputeChainHash(previousHash, decryptedData);
if (!CryptographicOperations.FixedTimeEquals(expectedHash, storedEvent.ChainHash))
{
    throw new EventChainIntegrityException(
        $"Chain integrity violation detected for event {storedEvent.Id}");
}
```

Si alguien modifica el evento numero 5 de una cadena de 100 eventos, los hashes de los eventos 5 al 100 dejaran de coincidir.

---

### 4.3 RandomNumberGenerator (Generacion Segura de Numeros)

**Regla de oro**: **NUNCA uses `System.Random` para nada relacionado con seguridad**.

`System.Random` es predecible. Si alguien conoce unas pocas salidas, puede predecir las siguientes. En cambio, `RandomNumberGenerator` usa el generador criptografico del sistema operativo:

```csharp
// CORRECTO: Criptograficamente seguro
byte[] nonce = new byte[12];
RandomNumberGenerator.Fill(nonce);

// INCORRECTO: Predecible, NUNCA usar para seguridad
Random random = new();
random.NextBytes(nonce);  // NO HAGAS ESTO
```

---

### 4.4 Gestion de Llaves

En **produccion**, las llaves de encriptacion y HMAC se obtienen de **Azure Key Vault**, un servicio seguro en la nube que:

- Almacena las llaves en hardware especializado (HSM).
- Controla quien puede acceder a cada llave.
- Permite rotar llaves sin cambiar codigo.

En **desarrollo local**, las llaves vienen de la configuracion (`appsettings.json`) para facilitar las pruebas.

---

## 5. Como se Construyo el Proyecto Paso a Paso

El proyecto se construyo en 5 fases, cada una construyendo sobre la anterior. Si quieres replicar este proceso o entender por que las cosas estan donde estan, esta seccion es para ti.

---

### Fase 1: Estructura del Proyecto

**Objetivo**: Crear la base organizativa sin escribir logica de negocio.

**Que se hizo**:

- `Directory.Build.props` -- Configuracion centralizada de compilacion para todos los proyectos (warnings como errores, nullable habilitado, implicit usings deshabilitado).
- `Directory.Packages.props` -- Manejo centralizado de versiones de paquetes NuGet. En vez de que cada proyecto tenga su propia version de FluentValidation, se define una sola vez aqui.
- Archivos `.csproj` para cada capa (Domain, Application, Infrastructure, Api) con sus referencias correctas.
- `docker-compose.yml` para levantar PostgreSQL, Redis y Seq (observabilidad) en un solo comando.
- Pipeline de CI/CD en GitHub Actions.

**Leccion clave**: Invertir tiempo en la estructura inicial ahorra horas de refactorizacion despues.

---

### Fase 2: Domain Layer

**Objetivo**: Definir las reglas de negocio puras, sin dependencias externas.

**Que se creo (en orden)**:

1. **Abstractions**: Las interfaces y clases base que todo lo demas usara.
   - `IDomainEvent` -- Contrato para eventos de dominio.
   - `Entity<TId>` -- Clase base con identidad.
   - `IAggregateRoot` -- Interfaz para raices de agregado.
   - `AggregateRoot<TId>` -- Clase base con manejo de eventos.
   - `DomainError` -- Tipo para representar errores de dominio.
   - `Result` / `Result<T>` -- Patron resultado.
   - `IRepository<T>` -- Interfaz generica de repositorio.
   - `IEventStore` -- Interfaz del almacen de eventos.
   - `IUnitOfWork` -- Interfaz de unidad de trabajo.

2. **Value Objects**:
   - `TransactionId` -- ID fuertemente tipado (evita confundir con otros GUIDs).
   - `AccountId` -- ID fuertemente tipado para cuentas.
   - `Currency` -- Smart Enum con las monedas soportadas (USD, EUR, COP, etc.).
   - `Money` -- Monto + moneda con aritmetica segura.
   - `TransactionStatus` -- Smart Enum con transiciones validas.

3. **Domain Events**: Un evento por cada cambio de estado significativo de la transaccion.

4. **Domain Errors**: Errores especificos como `TransactionErrors.SameAccount` o `MoneyErrors.NegativeAmount`.

5. **Aggregates**: `TransactionAggregate` con toda la logica de estado y reconstruccion desde eventos.

6. **Tests**: Pruebas unitarias para cada componente. Cobertura minima del 90%.

---

### Fase 3: Application Layer

**Objetivo**: Orquestar los casos de uso usando CQRS.

**Que se creo**:

1. **Abstractions CQRS**:
   - `ICommand<TResponse>` / `ICommandHandler<TCommand, TResponse>` -- Para operaciones de escritura.
   - `IQuery<TResponse>` / `IQueryHandler<TQuery, TResponse>` -- Para operaciones de lectura.
   - `ITransactionRepository` -- Interfaz especifica para el repositorio de transacciones.
   - `ITransactionQueryService` -- Interfaz para consultas de solo lectura.

2. **Commands**:
   - `ProcessTransactionCommand` + `ProcessTransactionCommandHandler` + `ProcessTransactionCommandValidator`
   - `ReverseTransactionCommand` + `ReverseTransactionCommandHandler` + `ReverseTransactionCommandValidator`

3. **Queries**:
   - `GetTransactionByIdQuery` + `GetTransactionByIdQueryHandler`
   - `GetTransactionHistoryQuery` + `GetTransactionHistoryQueryHandler`

4. **Behaviors** (pipeline de MediatR):
   - `ValidationBehavior` -- Valida automaticamente cada request antes de llegar al handler.
   - `LoggingBehavior` -- Registra cada request con su tiempo de ejecucion.

5. **DTOs**: `TransactionResponse`, `TransactionHistoryResponse`.

6. **DependencyInjection.cs**: Extension method para registrar todos los servicios de la capa.

---

### Fase 4: Infrastructure Layer

**Objetivo**: Implementar todo lo que interactua con el mundo exterior.

**Que se creo**:

1. **Cryptography**:
   - `ICryptoService` -- Interfaz con operaciones de encriptacion, HMAC y hash.
   - `AesGcmCryptoService` -- Implementacion con AES-256-GCM y HMAC-SHA512.
   - `CryptoSettings` -- Configuracion de llaves.

2. **Event Store**:
   - `StoredEvent` -- Modelo de persistencia para eventos.
   - `PostgresEventStore` -- Implementacion del Event Store con hash chaining.
   - `EventSerializer` -- Serializacion/deserializacion de eventos a JSON.
   - `EventStoreSettings` -- Configuracion (verificar cadena al leer, etc.).
   - JsonConverters para `Money`, `TransactionId`, `AccountId`.

3. **Persistence**:
   - `EventStoreDbContext` -- DbContext de EF Core para el Event Store.
   - `TransactionDbContext` -- DbContext para el read model.
   - `TransactionRepository` -- Implementacion del repositorio.
   - `TransactionReadModel` -- Modelo optimizado para consultas.
   - `UnitOfWork` -- Implementacion de la unidad de trabajo.

4. **Query Services**:
   - `TransactionQueryService` -- Consultas directas al read model (bypasses el Event Store para lecturas rapidas).

---

### Fase 5: API Layer

**Objetivo**: Exponer todo como endpoints HTTP.

**Que se creo**:

1. **Endpoints** (Minimal APIs):
   - `TransactionEndpoints` -- CRUD completo de transacciones.
   - `HealthEndpoints` -- Health check para monitoreo.
   - `DemoEndpoints` -- Endpoints de demostracion.

2. **Middleware**:
   - `ExceptionHandlingMiddleware` -- Captura excepciones no manejadas y las convierte en respuestas HTTP consistentes.

3. **Extensions**:
   - `AuthenticationExtensions` -- Configuracion de JWT Bearer.
   - `LoggingExtensions` -- Configuracion de Serilog.
   - `OpenApiExtensions` -- Configuracion de documentacion OpenAPI.

4. **Contracts**: `ApiContracts.cs` con los request/response contracts de la API.

5. **Architecture Tests**: Pruebas con NetArchTest.Rules que verifican en compilacion que las capas respeten sus dependencias (por ejemplo, que Domain nunca referencie a Infrastructure).

---

## 6. Glosario Rapido

| Termino | Definicion |
|---|---|
| **Aggregate** | Grupo de objetos de dominio tratados como una unidad con una raiz que controla el acceso y las reglas de negocio. |
| **AES-256-GCM** | Algoritmo de encriptacion autenticada que protege la confidencialidad e integridad de los datos con una llave de 256 bits. |
| **Clean Architecture** | Patron de arquitectura donde las dependencias apuntan hacia adentro, manteniendo el dominio independiente de frameworks y tecnologias. |
| **CQRS** | Patron que separa las operaciones de escritura (Commands) de las operaciones de lectura (Queries) para optimizar cada una por separado. |
| **Domain Event** | Objeto inmutable que registra que algo significativo ocurrio en el dominio, como "TransactionCompleted". |
| **DDD** | Metodologia de diseno de software centrada en modelar el dominio del negocio usando un lenguaje compartido entre desarrolladores y expertos. |
| **Entity** | Objeto de dominio con identidad unica que persiste a traves de cambios en sus atributos. |
| **Event Sourcing** | Patron donde el estado se reconstruye a partir de una secuencia de eventos en vez de guardar solo el estado actual. |
| **FluentValidation** | Biblioteca que permite definir reglas de validacion de forma declarativa y legible en C#. |
| **Hash Chain** | Secuencia de hashes donde cada uno depende del anterior, creando una cadena inmutable que detecta alteraciones. |
| **HMAC-SHA512** | Algoritmo que calcula un "resumen" autenticado de datos usando una llave secreta, garantizando integridad y autenticidad. |
| **MediatR** | Biblioteca que implementa el patron Mediator para enviar Commands y Queries a sus Handlers sin acoplamiento directo. |
| **Nonce** | Numero usado una sola vez (Number used ONCE) para garantizar que cada operacion criptografica produzca un resultado diferente. |
| **Repository** | Abstraccion que encapsula el acceso a datos, permitiendo al dominio trabajar sin conocer la tecnologia de persistencia. |
| **Result Pattern** | Patron donde las operaciones retornan un objeto Result que contiene el valor exitoso o un error, en vez de lanzar excepciones. |
| **Smart Enum** | Patron donde un "enum" se implementa como una clase con instancias estaticas, permitiendo agregar comportamiento (como transiciones de estado). |
| **Unit of Work** | Patron que agrupa multiples operaciones de persistencia en una sola transaccion atomica: o se guardan todas o ninguna. |
| **Value Object** | Objeto inmutable sin identidad propia que se compara por sus valores. Dos objetos con los mismos datos son considerados iguales. |
| **Minimal APIs** | Estilo de ASP.NET Core que permite definir endpoints HTTP de forma concisa sin controladores. |
| **Pipeline Behavior** | Middleware de MediatR que intercepta cada request para agregar funcionalidad transversal como validacion o logging. |

---

> **Ultimo consejo**: La mejor forma de aprender este proyecto es ejecutarlo localmente y seguir el flujo de una transaccion paso a paso con un debugger. Levanta los servicios con `docker-compose up -d`, ejecuta el API con `dotnet run --project src/SecureTransact.Api` y envia una transaccion de prueba. Observa como cada capa hace su trabajo.
