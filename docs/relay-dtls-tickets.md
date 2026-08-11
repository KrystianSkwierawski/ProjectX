# Relay, DTLS i jednorazowe tickety sesji

Ten dokument opisuje wdrożony przepływ bezpiecznego połączenia pomiędzy klientem Unity, Unity Dedicated Serverem i API ProjectX oraz ręczne kroki potrzebne do uruchomienia go w Unity Gaming Services. Kod runtime, API ticketów i lokalny tryb Direct są już zaimplementowane; produkcyjny Relay zacznie działać po połączeniu projektu z UGS.

## Cel i granice rozwiązania

Docelowe połączenie produkcyjne wygląda następująco:

```text
Client --DTLS--> Unity Relay --DTLS--> Dedicated Server
   |                                         |
   +--------------- HTTPS ------------------+--> ProjectX API
```

Relay pośredniczy w ruchu UDP, a DTLS szyfruje i chroni integralność każdego odcinka prowadzącego do Relay. Nie jest to szyfrowanie end-to-end, którego Unity Relay nie może zakończyć. ProjectX JWT nadal odpowiada za dostęp do API, ale nie powinien być przesyłany przez Netcode/RPC.

Jednorazowy ticket służy wyłącznie do wpuszczenia zalogowanego użytkownika do konkretnej sesji gry. Po jego realizacji Dedicated Server mapuje `NetworkClientId` na `PlayerSessionId` i używa własnego JWT serwera przy dalszych wywołaniach API.

Relay join code nie jest poświadczeniem użytkownika. Osoba znająca kod może dotrzeć do allocation, ale bez ważnego ticketu musi zostać odrzucona przez NGO Connection Approval.

## Wersje pakietów

Projekt używa obecnie:

- Unity `6000.1.15f1`,
- Netcode for GameObjects `2.4.4`,
- Unity Transport `2.5.3`,
- Multiplayer Play Mode `1.6.2`,
- Dedicated Server `1.6.2`.

W `Client/Packages/manifest.json` są przypięte dokładnie:

```json
"com.unity.dedicated-server": "1.6.2",
"com.unity.multiplayer.playmode": "1.6.2",
"com.unity.services.multiplayer": "1.2.0"
```

Wersja MPS `1.2.0` pozostaje zgodna z Unity Transport `2.5.x` i z używanym API Multiplayer Play Mode. MPS `2.1.2` ma wprawdzie zgodną zależność UTP, ale usuwa typy Multiplay nadal kompilowane przez Multiplayer Play Mode `1.6.x`, przez co blokuje kompilację Editora. Multiplayer Play Mode i Dedicated Server mają tę samą wersję, aby nie mieszać ich sprzężonych narzędzi edytorowych.

Pakiet należy zainstalować lub rozwiązać przez Unity Package Manager. Nie należy ręcznie edytować `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj` ani `Packages/packages-lock.json`; Unity odtworzy referencje i lock po imporcie. Jeśli powstają nowe skrypty Unity, trzeba zatwierdzić także ich pliki `.meta`.

Minimalne przestrzenie nazw dla ręcznej integracji Relay:

```csharp
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
```

W tym projekcie zalecane jest niższopoziomowe API `RelayService`, ponieważ istniejący `Bootstrap` jawnie kontroluje moment wywołania `StartServer()` i `StartClient()`. Wysokopoziomowe MPS Sessions może automatycznie uruchamiać Netcode, co utrudnia ustawienie ticketu i callbacku akceptacji przed rozpoczęciem połączenia.

## Jednorazowa konfiguracja Unity Gaming Services

### 1. Połącz projekt z UGS

W Unity Editor:

1. Zaloguj się do Unity Hub/Editor kontem mającym dostęp do właściwej organizacji.
2. Otwórz `Edit > Project Settings > Services`.
3. Połącz lokalny projekt z istniejącym projektem Unity Cloud albo utwórz nowy.
4. W Unity Dashboard sprawdź, czy projekt ma dostęp do Authentication i Relay oraz czy rozliczenia/limity Relay odpowiadają środowisku.
5. Utwórz i nazwij środowiska, np. `development`, `staging` i `production`. Nazwa przekazana do `SetEnvironmentName` musi odpowiadać nazwie w Dashboardzie.

Aktualnie `Client/ProjectSettings/ProjectSettings.asset` ma puste `cloudProjectId`, `projectName` i `organizationId`. `UnityServices.InitializeAsync` nie zadziała, dopóki projekt nie zostanie poprawnie połączony.

### 2. Sprawdź profil Dedicated Server

`Client/Assets/Settings/Build Profiles/DedicatedServer.asset` przechowuje własny snapshot `PlayerSettings`, który obecnie również zawiera puste pola Unity Cloud. Po połączeniu projektu należy:

1. Otworzyć profil `DedicatedServer` w oknie Build Profiles.
2. Zresetować lub zsynchronizować jego nadpisania Player Settings z ustawieniami globalnymi.
3. Zbudować serwer i sprawdzić podczas startu, że `Application.cloudProjectId` nie jest puste.

Nie należy wpisywać identyfikatorów ręcznie do YAML. `cloudProjectId` identyfikuje projekt i nie zastępuje poświadczeń, ale ręczna edycja łatwo pozostawia niespójne ustawienia profilu.

### 3. Uwierzytelnienie UGS

Unity Authentication oraz ProjectX JWT są niezależnymi mechanizmami:

- Unity Authentication daje procesowi dostęp do Relay.
- ProjectX JWT uwierzytelnia użytkownika albo Dedicated Server względem ProjectX API.
- Ticket łączy tożsamość ProjectX z konkretnym połączeniem NGO.

Pierwsza iteracja może korzystać z anonimowego UGS sign-in. Każdy równoległy proces powinien dostać osobny profil, ponieważ Authentication przechowuje token sesji w `PlayerPrefs`:

```csharp
var options = new InitializationOptions()
    .SetEnvironmentName(environmentName)
    .SetProfile(ugsProfile);

await UnityServices.InitializeAsync(options);

if (!AuthenticationService.Instance.IsSignedIn)
{
    await AuthenticationService.Instance.SignInAnonymouslyAsync();
}
```

Przykładowe profile to `server-local`, `client-main`, `client-secondary` i `client`. Nazwy muszą być stabilne, mieć maksymalnie 30 znaków i zawierać tylko obsługiwane znaki alfanumeryczne, `-` lub `_`. Przy wielu serwerach na jednej maszynie każdy proces potrzebuje odrębnego profilu lub docelowej tożsamości serwisowej.

Nie wolno umieszczać sekretu konta serwisowego w buildzie klienta.

## Tryby transportu

### Produkcja: Relay + DTLS, fail closed

Brak argumentu lokalnego oznacza tryb produkcyjny:

- UGS musi się zainicjalizować,
- Authentication musi się zalogować,
- allocation Relay musi zostać utworzone lub dołączone,
- `SetRelayServerData(..., "dtls")` musi zakończyć się powodzeniem,
- dopiero później wolno uruchomić Netcode.

Jeżeli którykolwiek krok się nie powiedzie, Dedicated Server powinien zakończyć proces z niezerowym kodem, a klient powinien wrócić do ekranu logowania z ogólnym komunikatem. Nie wolno automatycznie przechodzić z Relay/DTLS na zwykłe UDP, ponieważ byłby to downgrade bezpieczeństwa.

### Lokalnie: jawny Direct

Wyłącznie lokalny development może ominąć UGS i użyć istniejącego adresu `127.0.0.1:7777`. Jawny argument to:

```text
-projectx-direct
```

`Client/Automation/run.ps1` przekazuje go uruchamianemu lokalnie procesowi Dedicated Server. Runtime powinien rozpoznawać Direct tylko wtedy, gdy:

- proces otrzymał `-projectx-direct`, albo
- klient działa bezpośrednio w Unity Editor w lokalnym scenariuszu deweloperskim.

Standalone/release bez argumentu wybiera Relay. Direct powinien słuchać wyłącznie na loopbackie; nie należy używać tego przełącznika na publicznym serwerze. Parser argumentów znajduje się w `GameSessionManager`.

API dodatkowo odrzuca rejestrację `UsesRelay = false` poza środowiskiem `Development`. Ewentualny override `GameSessionSettings:AllowDirectTransport` jest przeznaczony wyłącznie dla kontrolowanych środowisk lokalnych; nie należy włączać go w produkcji.

Przykłady:

```powershell
# Lokalny pełny stack; run.ps1 dodaje argument serwerowi automatycznie.
Client\Automation\run.bat

# Ręczny lokalny serwer Direct.
Client\Builds\Server\ProjectXServer.exe -projectx-direct

# Produkcja Relay + DTLS: bez lokalnego przełącznika.
ProjectXServer.exe
```

Klient i serwer muszą wybrać ten sam tryb. Nie należy próbować połączenia Direct po nieudanym `JoinAllocationAsync`.

### Konfiguracja procesu

Poza lokalnym Editorem i jawnym trybem Direct trzeba podać bazowy adres API, łącznie z segmentem `/api`:

```powershell
$env:PROJECTX_API_URL = "https://api.example.com/api"
```

Można też użyć argumentu `-projectx-api-url https://api.example.com/api`. Runtime wymaga HTTPS; HTTP jest akceptowane wyłącznie dla adresu loopback podczas developmentu.

Dedicated Server pobiera konto ProjectX wyłącznie ze zmiennych środowiskowych, aby poświadczenia nie trafiały do builda ani argumentów procesu:

```powershell
$env:PROJECTX_SERVER_USERNAME = "server@example.com"
$env:PROJECTX_SERVER_PASSWORD = "<sekret ze secret store>"
```

Lokalny `run.ps1` ustawia wartości seedowe tylko wtedy, gdy te zmienne nie są już ustawione. W produkcji należy wstrzyknąć je z magazynu sekretów procesu lub platformy wdrożeniowej.

Każdy równocześnie aktywny proces Dedicated Servera potrzebuje osobnego konta ProjectX z rolą `Server`. Ponowna rejestracja tym samym kontem celowo zastępuje jego poprzednią sesję i unieważnia powiązane tickety oraz `PlayerSessionId`.

Opcjonalne argumenty UGS to `-projectx-ugs-environment development` oraz `-projectx-ugs-profile unikalny-profil`. `-projectx-relay` wymusza Relay także w Editorze i ma pierwszeństwo przed trybem Direct.

## Konfiguracja Relay + DTLS

### Dedicated Server

Po załadowaniu `MainScene`, ale przed `NetworkManager.Singleton.StartServer()`:

```csharp
var allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
transport.SetRelayServerData(
    AllocationUtils.ToRelayServerData(allocation, "dtls"));
```

`SetRelayServerData` sam przełącza `UnityTransport` na protokół Relay. Pole `Use Encryption` sceny nie włącza DTLS dla allocation i nie zastępuje wartości `"dtls"`.

Rekomendowana kolejność startu serwera:

1. Zaloguj Dedicated Server do ProjectX API i uzyskaj JWT z rolą `Server`.
2. Załaduj `MainScene` oraz sceny serwerowe.
3. W trybie Relay zainicjalizuj UGS i profil Authentication.
4. Utwórz allocation, pobierz join code i skonfiguruj `UnityTransport` jako `dtls`.
5. Włącz i zarejestruj NGO Connection Approval.
6. Uruchom `StartServer()`.
7. Dopiero po sukcesie zarejestruj aktywną sesję oraz join code w ProjectX API.
8. Po rejestracji odnawiaj domyślnie 90-sekundowy lease; normalny heartbeat następuje nie później niż po 30 sekundach i nie później niż w połowie pozostałego czasu.
9. Jeżeli rejestracja API się nie powiedzie albo lease nie może zostać bezpiecznie odnowiony, wykonaj `Shutdown()` i zakończ proces. Serwer, którego klient nie może odnaleźć, nie powinien pozostawać aktywny.

Join code jest ważny tylko tak długo, jak allocation i serwer pozostają aktywne. Wdrożony endpoint `POST /api/GameSessions/Heartbeat` odnawia lease według czasu UTC. API pozwala skonfigurować lease w zakresie 30-600 sekund, a klient wylicza harmonogram z `ExpiresAtUtc`, ponawia błędy przejściowe maksymalnie co 10 sekund i przed każdym żądaniem rezerwuje pełny 15-sekundowy timeout HTTP oraz pięć sekund marginesu. Błąd terminalny `4xx` kończy go od razu. API usuwa wygasłą sesję wraz z jej ticketami i `PlayerSessionId`; awaria samego transportu NGO również kończy Dedicated Server, aby supervisor mógł go uruchomić ponownie.

### Klient

Po otrzymaniu join code i ticketu z ProjectX API, po załadowaniu `MainScene`, ale przed `StartClient()`:

```csharp
var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

var networkManager = NetworkManager.Singleton;
var transport = networkManager.GetComponent<UnityTransport>();

transport.SetRelayServerData(
    AllocationUtils.ToRelayServerData(allocation, "dtls"));

networkManager.NetworkConfig.ConnectionData =
    System.Text.Encoding.UTF8.GetBytes(ticket);

networkManager.StartClient();
```

Wywołanie API zwracające `ticket` i `joinCode` musi mieć wyłączone logowanie odpowiedzi (`log: false`). Obecny `UnityWebRequestHelper` loguje body dla domyślnego `log: true`.

## Kontrakt ticketów

### Wydanie ticketu

Zalogowany klient wywołuje przez HTTPS przykładowo:

```http
POST /api/GameSessions/Ticket
Authorization: Bearer <client-jwt>
```

API sprawdza JWT klienta oraz wybiera aktywną sesję serwera z ważnym lease. Odpowiedź zawiera:

```json
{
  "gameSessionId": "a14c4298-4195-45b4-a54d-d601be09ee5c",
  "usesRelay": true,
  "relayJoinCode": "AB12CD",
  "ticket": "<32 losowe bajty zakodowane base64url>",
  "expiresAtUtc": "2026-08-10T14:01:00Z"
}
```

Ticket powinien być:

- wygenerowany przez `RandomNumberGenerator.GetBytes(32)`,
- ważny obecnie przez 60 sekund,
- przypisany do `UserId`, `GameSessionId` i `ServerId`,
- jednorazowy,
- przechowywany po stronie API wyłącznie jako SHA-256 hash,
- datowany przez `TimeProvider.GetUtcNow()` jako `DateTimeOffset` UTC,
- nieobecny w logach, komunikatach błędów i telemetrii.

API utrzymuje najwyżej jeden aktywny ticket dla pary klient/sesja; wydanie następnego unieważnia poprzedni. Endpoint ma limit 20 żądań na minutę na uwierzytelnione konto klienta i odpowiada `429` po przekroczeniu limitu. Dzięki partycjonowaniu po `NameIdentifier` gracze za wspólnym NAT-em lub reverse proxy nie współdzielą tego limitu.

Przykładowy rekord wewnętrzny:

```text
TicketHash
UserId
GameSessionId
ServerId
ExpiresAtUtc
```

### Realizacja ticketu

Klient wysyła surowy ticket w NGO `ConnectionData`. Dedicated Server ustawia odpowiedź approval jako oczekującą i realizuje ticket przez HTTPS swoim JWT:

```http
POST /api/GameSessions/Redeem
Authorization: Bearer <server-jwt>
Content-Type: application/json

{
  "ticket": "...",
  "gameSessionId": "a14c4298-4195-45b4-a54d-d601be09ee5c"
}
```

API musi atomowo znaleźć i usunąć hash ticketu. Dwa równoległe żądania realizacji tego samego ticketu mogą dać najwyżej jeden sukces. Poprawna odpowiedź może zawierać:

```json
{
  "userId": "user-123",
  "playerSessionId": "player-session-456"
}
```

`PlayerSessionId` jest przypisany do serwera i sesji gry. Disconnect wywołuje revoke z trzema próbami, a utrata lease sesji unieważnia wszystkie jej poświadczenia nawet wtedy, gdy revoke nie dotrze. Każda ponowna próba połączenia wymaga nowego ticketu.

### NGO Connection Approval

W obu buildach należy ustawić `NetworkConfig.ConnectionApproval = true`. Na serwerze callback powinien:

1. Natychmiast ustawić `response.Pending = true`.
2. Odczytać ticket z `request.Payload`.
3. Zrealizować go w ProjectX API własnym JWT serwera.
4. Zapisać mapowanie `request.ClientNetworkId -> PlayerSessionId/UserId`.
5. Ustawić `Approved = true`, `CreatePlayerObject = true`, a na końcu `Pending = false`.
6. Przy błędzie ustawić ogólną przyczynę, `Approved = false`, `CreatePlayerObject = false` i `Pending = false`.

Callback musi żyć dłużej niż `Bootstrap`, ponieważ serwer ładuje `MainScene` w trybie `Single`. Powinien należeć do trwałego serwisu/singletonu, a nie do niszczonego `Bootstrap` MonoBehaviour.

Runtime ustawia `ClientConnectionBufferTimeout` na 20 sekund przy 15-sekundowym limicie żądania API, aby NGO nie odrzuciło klienta przed zakończeniem kontrolowanego żądania HTTP. Rozłączenie podczas oczekującego `Redeem` oznacza approval jako odrzucone i natychmiast próbuje unieważnić ewentualnie wydany `PlayerSessionId`.

Po rozłączeniu serwer obsługuje `NetworkManager.OnClientDisconnectCallback`, usuwa mapowanie oraz zamyka `PlayerSessionId`.

## Usunięcie JWT z RPC

Ticket spełnia swój cel dopiero wtedy, gdy pełny JWT klienta przestaje przechodzić przez Netcode. Migracja obejmuje:

1. Usunięcie parametru JWT z `LoadCharacterServerRpc` oraz całego `UpdateSessionTokenServerRpc(string clientToken)`.
2. Usunięcie `_clientTokens` i metod `SetClientToken`/`GetClientToken` z `UserManager`.
3. Usunięcie argumentów `clientToken`/`token` ze wszystkich pozostałych `ServerRpc`.
4. Identyfikowanie prawdziwego wywołującego przez serwerowy `OwnerClientId` dla RPC wymagających ownership albo przez `ServerRpcParams.Receive.SenderClientId`; nie wolno ufać identyfikatorowi przesłanemu przez klienta.
5. Rozwiązywanie `PlayerSessionId` po `SenderClientId` w trwałym rejestrze serwera.
6. Wywoływanie ProjectX API z `Authorization: Bearer <server-jwt>` oraz ograniczonym identyfikatorem sesji gracza, a nie z JWT klienta.

Godzinny JWT oraz refresh po 55 minutach nadal obowiązują dla klienta i Dedicated Servera w ich bezpośredniej komunikacji z API. API odrzuca próbę refreshu przed końcowym pięciominutowym oknem ważności bieżącego JWT. Odświeżony JWT klienta nie musi być synchronizowany z serwerem gry.

## Ograniczenie pierwszej implementacji in-memory

Obecna pierwsza wersja trzyma tickety, sesje serwerów i `PlayerSessionId` w pamięci API, dlatego jest poprawna wyłącznie przy jednej instancji procesu API:

- restart API unieważnia wszystkie oczekujące tickety i aktywne mapowania,
- przy load balancerze ticket może zostać wydany przez instancję A i zrealizowany w instancji B, która go nie zna,
- lokalne blokady nie zapewniają atomowego single-use pomiędzy procesami,
- sticky sessions nie rozwiązują restartów ani awarii instancji.
- endpoint ticketu wybiera obecnie ostatnio zarejestrowaną sesję; wiele serwerów, regiony i matchmaking wymagają jawnego wyboru sesji albo osobnego allocatora.

Przed uruchomieniem wielu instancji należy przenieść stan do współdzielonego magazynu, np. Redis, i realizować ticket atomowo (`GETDEL`, skrypt Lua albo transakcja). Alternatywą jest wspólna baza z unikalnym hashem i warunkową aktualizacją/usunięciem. API nadal powinno używać `TimeProvider` oraz UTC.

Deployment musi pozostać single-instance i nie może skalować API horyzontalnie, dopóki magazyn nie zostanie wymieniony.

## Obsługa błędów i bezpieczeństwo

- Błąd UGS/Relay w produkcji: przerwij start, bez Direct fallback.
- Nieważny, przeterminowany lub zużyty ticket: odmów połączenia bez ujawniania, który warunek zawiódł.
- Błąd API podczas approval: odmów połączenia; klient może pobrać nowy ticket i ponowić cały reconnect.
- Utrata Relay przez serwer: zamknij Netcode i pozwól supervisorowi zrestartować proces.
- Nie loguj JWT, ticketów, UGS access tokenów ani pełnych odpowiedzi endpointów sesji.
- Wydawanie ticketów jest limitowane per uwierzytelnione konto; przed ekspozycją na duży ruch warto dodać także niezależny globalny limit pamięci lub ochronę per IP.
- Ogranicz długość `ConnectionData` i odrzucaj pusty lub nadmiernie długi payload przed wywołaniem API.
- Nie traktuj DTLS jako zabezpieczenia przed przejęciem tokenu z pamięci procesu, logów lub zainfekowanego urządzenia.

## Stan wdrożenia i kolejność uruchomienia

W kodzie są już gotowe: MPS `1.2.0`, parser trybu, Relay allocation/join z `"dtls"`, brak fallbacku, bezpieczna konfiguracja API i sekretów serwera, API sesji i jednorazowych ticketów, lease/heartbeat, limit wydawania ticketów, NGO Connection Approval, mapowanie `NetworkClientId -> PlayerSessionId`, usunięcie JWT z RPC oraz cleanup po disconnect.

Do wykonania w środowisku pozostaje:

1. Połącz projekt i profil Dedicated Server z UGS.
2. Ustaw produkcyjne `PROJECTX_API_URL`, sekrety konta serwera i właściwe środowisko/profile UGS.
3. Przetestuj Direct lokalnie, a Relay/DTLS w środowisku UGS `development`: poprawne połączenie, odrzucenie ticketu, ponowne użycie, expiry, ręczne ponowienie połączenia z nowym ticketem oraz awarię Relay/API.
4. Przed skalowaniem API zastąp magazyn in-memory magazynem współdzielonym.
5. Jeżeli gra ma automatycznie wracać po utracie już zestawionego połączenia sieciowego, dodaj osobny kontroler runtime reconnectu. Obecna implementacja zabezpiecza admission każdej próby, ale nie uruchamia samoczynnie nowego `StartClient()` po utracie Relay lub serwera.

## Kryteria akceptacji

- Produkcyjny build bez `-projectx-direct` nie uruchamia zwykłego UDP.
- Dedicated Server i klient konfigurują Relay z `"dtls"` przed startem Netcode.
- Klient nie może połączyć się bez ważnego jednorazowego ticketu.
- Ponowne użycie tego samego ticketu jest odrzucane także przy równoległych żądaniach.
- Żaden `ServerRpc` nie przyjmuje ProjectX JWT klienta.
- Po refreshu JWT klient nie wysyła nowego tokenu do Dedicated Servera.
- Tokeny i tickety nie pojawiają się w logach.
- Local automation uruchamia Dedicated Server z `-projectx-direct` i nadal łączy się wyłącznie przez loopback.
- Każda nowa lub ponowiona próba połączenia pobiera świeży ticket; ticket użyty wcześniej nie jest ponownie wykorzystywany.
- Lease martwego serwera wygasa i unieważnia jego tickety oraz `PlayerSessionId`.
- Ograniczenie single-instance magazynu in-memory jest udokumentowane jako wymóg wdrożeniowy.
