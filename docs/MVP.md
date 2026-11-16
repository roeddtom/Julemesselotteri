# MVP: avklaringer og plan

## Avtalte regler

- Windows, frakoblet drift, operatørvindu og separat fullskjerm på projektor.
- Norsk bokmål. Ingen personopplysninger, billettsalg eller PowerPoint-avspilling.
- Hvert hefte har 50 globale loddnummer: bok 1 = 1–50, bok 2 = 51–100.
- Boklodd: konfigurerbart loddintervall, utelat usolgte bøker og nummerintervaller.
  Hvert gjenværende lodd har samme vinnersjanse. Boknummer er avledet av vinnertallet,
  ikke trukket separat. Vinnertall brukes bare én gang per arrangement.
- Hjertelodd: 1–1000, unike nummer innen runden. Samme hjerte kan vinne flere
  ganger via forskjellige nummer. Ny runde nullstiller nummer og beholder gevinster.
- Gevinster: én per linje, hovedgevinst øverst. Trekkes nedenfra og opp.
- Én trekk-knapp. Separat hastighet og varighet per lotteri.
- Boklodd viser bok først, pause, deretter globalt loddnummer. Ingen radnummer.
- Vis nå avslutter animasjonen. Avslutt runde stopper videre trekking og lukker
  publikumsvinduet. Siste vinner beholdes til operatøren avslutter.
- Automatisk vinnerliste etter visning; stort, varmt juletema; lyd med volum/lydløs.
- PowerPoint kjører separat og vises igjen når publikumsvinduet lukkes.
- Valgt vinner lagres før animasjonen starter. Omstart gjenopptar samme trekning.
- Lesbare, tidsstemplede TXT-logger i `Dokumenter\Julemesselotteri`.
- Ingen omtrekning i MVP. Nytt arrangement krever bekreftelse og beholder oppsett.

## Teknologi og leveranser

C# / .NET 10 LTS med WPF, uten eksterne kjøretidstjenester. Selvstendig,
portabel Windows-publisering. Testbar kjernelogikk uavhengig av skjermene.
Teknologivalget er en implementasjonsbeslutning basert på Windows/offline-kravene;
brukeren var ikke tilgjengelig for siste plangodkjenning.

1. **Trekkmotor og gjenoppretting:** regler, rettferdig utvalg, atomisk tilstand,
   lesbare logger og automatiserte tester.
2. **Operatør:** gevinstlister, utelatelser, innstillinger, rundekontroller og omstart.
3. **Publikum:** juletema, animasjon, lyd, skjermvalg og portabel distribusjon.

Hver vellykket iterasjon oppdaterer dokumentasjonen og får en conventional commit
med beskrivende meldingskropp.

## Driftssikkerhet

Tilstand er autoritativ; logger kan gjenskapes fra tilstanden. Endringer skrives
til en midlertidig fil, tømmes til disk og erstatter forrige fil med sikkerhetskopi.
Mislykket lagring får ikke fremdriften i minnet til å gå videre. Korrupte data
stopper oppstart fremfor å nullstille vinnerhistorikken. En eldre sikkerhetskopi
gjenopprettes aldri automatisk, fordi dette kunne gjort et vinnertall tilgjengelig igjen.

En uferdig runde fryser gevinster og loddutvalg. Innstillingsendringer gjelder neste
runde. Gevinstmengden kan ikke overstige tilgjengelige lodd. Øvre grense er
1 000 000 lodd, som en praktisk valideringsgrense for MVP.

## Fremdrift

- Iterasjon 1: implementert trekkmotor, transaksjonell tilstand og TXT-logger.
- Iterasjon 2: planlagt.
- Iterasjon 3: planlagt.
