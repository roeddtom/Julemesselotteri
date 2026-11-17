# Julemesselotteri

Frakoblet Windows-program for boklodd og hjertelodd på julemessa.
Operatøren styrer trekningen på PC-en; publikum ser et separat projektorvindu.
Operatørvinduet plasseres på Windows-hovedskjermen. Publikum bruker en annen
skjerm når en er tilkoblet, ellers et vanlig separat vindu på samme skjerm.

Avklarte krav og iterasjonsplan: [MVP](docs/MVP.md).
Veiledning og arrangementssjekkliste: [Bruk](docs/BRUK.md).

**Status:** MVP med animert vinnervisning i Ungdomssenterets visuelle profil, lokal lyd og
separate operatør-/publikumsvinduer. Boklotteriet viser bok først, deretter
loddnummer, med justerbar pause. Hvert lotteri har egen hastighet og varighet;
operatøren kan vise vinneren straks eller avslutte runden.

Et valgfritt eksempel med virtuell loddbok kan slås på under **Visning og lyd**:
Etter at både bok og loddnummer er ferdig trukket, kommer forsiden fram,
boka åpnes og riktig ti-raders side og vinnerrad markeres.
Hver side har stående A5-proporsjoner, med samme bredde som forsiden.
Valget er avslått som standard og påvirker ikke hjertelodd.

Begge vinduer følger designmanualen i `design`: mørk rød, rød og krem,
geometriske dekorelementer og den uendrede Ungdomssenteret-logoen. Typografien
bruker lokalt installert Rebond Grotesque, Work Sans eller manualens systemreserve
Arial. Logoen er innebygd i appen; designmappen trenger ikke følge med
den portable utgaven.

## Utvikling

Krever Windows og .NET 10 SDK. Fra prosjektroten:

```powershell
dotnet test Julemesselotteri.sln
dotnet run --project tests\Julemesselotteri.App.SmokeTests
dotnet run --project src\Julemesselotteri.App
```

Skjermtesten åpner midlertidige operatør- og publikumsvinduer og bruker egne
testdata i en midlertidig mappe, ikke arrangementets data. Den kontrollerer også
animasjon, hastighet/varighet, bokpause, avbrudd, omstart og generert lyd.
Appskjermbilder kan lagres med
`dotnet run --project tests\Julemesselotteri.App.SmokeTests -- artifacts\ui-check`.

Uten synlige vinduer kan boklayout, sidemapping og tallanimasjoner kontrolleres med:
`dotnet run --project tests\Julemesselotteri.App.SmokeTests -- --headless-booklet artifacts\portrait-booklet`.
Dette lagrer et bilde av bokoppslaget uten å åpne appvinduene; full skjermtest
over er fortsatt nødvendig for hele operatørflyten.

Skjermvalg og vindusplassering kan kontrolleres uten synlige vinduer med
`dotnet run --project tests\Julemesselotteri.App.SmokeTests -- --headless-displays`.
Testen bruker skjulte vinduer og simulerte skjermlister; fysisk projektor og
fokusrekkefølge må fortsatt kontrolleres manuelt før arrangementet.

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
en Windows 10/11 x64-PC. .NET følger med; event-PC-en trenger ikke SDK, separat
runtimeinstallasjon eller internett. Byggemaskinen trenger tilgang til konfigurerte
NuGet-kilder første gang avhengigheter og publiseringspakker hentes.

Hvis en ubrukt, konfigurert pakkekilde er utilgjengelig, kan samme kommando kjøres
med `-p:RestoreIgnoreFailedSources=true`. Dette endrer ikke pakkekildene.

Før arrangementet: kontroller lesbarhet, lyd og PowerPoint med den faktiske
projektoren. Se sjekklisten i brukerveiledningen.
