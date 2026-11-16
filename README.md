# Julemesselotteri

Frakoblet Windows-program for boklodd og hjertelodd på julemessa.
Operatøren styrer trekningen på PC-en; publikum ser et separat projektorvindu.

Avklarte krav og iterasjonsplan: [MVP](docs/MVP.md).
Veiledning og arrangementssjekkliste: [Bruk](docs/BRUK.md).

**Status etter iterasjon 2:** Forhåndsversjon med statisk vinnervisning.
Animasjon, bok-først-sekvens og lyd ferdigstilles i iterasjon 3.

## Utvikling

Krever .NET 10 SDK. Fra prosjektroten:

```powershell
dotnet test Julemesselotteri.sln
dotnet run --project tests\Julemesselotteri.App.SmokeTests
dotnet run --project src\Julemesselotteri.App
```

Skjermtesten åpner midlertidige operatør- og publikumsvinduer og bruker egne
testdata i en midlertidig mappe, ikke arrangementets data.

Kjernen håndterer gevinster i omvendt rekkefølge, usolgte lodd, unike vinnere,
atomisk lagring før visning og lesbare logger. Operatørvinduet har egne gevinstlister,
utelatelser, skjermvalg og innstillinger per lotteri.

Tilstand: `%LOCALAPPDATA%\Julemesselotteri\state.json` (forrige versjon: `.bak`).
Logger: `Dokumenter\Julemesselotteri`. Ta kopi av begge før arrangementet.

## Portabel Windows-versjon

```powershell
dotnet publish src\Julemesselotteri.App -p:PublishProfile=Portable
```

Resultat: `artifacts\win-x64\Julemesselotteri.App.exe`. Kopier **hele mappen** til
en Windows x64-PC. .NET følger med; event-PC-en trenger ikke SDK, separat
runtimeinstallasjon eller internett. Byggemaskinen trenger tilgang til konfigurerte
NuGet-kilder første gang avhengigheter og publiseringspakker hentes.

Hvis en ubrukt, konfigurert pakkekilde er utilgjengelig, kan samme kommando kjøres
med `-p:RestoreIgnoreFailedSources=true`. Dette endrer ikke pakkekildene.
