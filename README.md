# Julemesselotteri

Frakoblet Windows-program for boklodd og hjertelodd på julemessa.
Operatøren styrer trekningen på PC-en; publikum ser et separat projektorvindu.

Avklarte krav og iterasjonsplan: [MVP](docs/MVP.md).

## Utvikling

Krever .NET 10 SDK. Fra prosjektroten:

```powershell
dotnet test Julemesselotteri.sln
```

Kjernen håndterer gevinster i omvendt rekkefølge, usolgte lodd, unike vinnere,
atomisk lagring før visning og lesbare logger. WPF-grensesnittet bygges i neste
iterasjon.
