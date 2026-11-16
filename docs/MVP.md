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
Teknologivalget er basert på Windows/offline-kravene.

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
- Iterasjon 2: operatørvindu med automatisk lagring, låst rundeoppsett, skjermvalg,
  gjenoppretting og rundekontroller.
- Iterasjon 3: ferdig. Publikumsvindu med skalerbart juletema, animerte
  talltromler som sakker av, bok-først-sekvens, konfigurerbar pause og lokal lyd.
  `Vis nå` hopper til vinneren fra begge deler og pausen. Lydløs stopper
  pågående lyd umiddelbart; volum brukes på neste lydsignal.
- Lukking av publikumsvinduet under animasjon setter trekningen på pause uten å
  forkaste vinneren. Lukking av operatørvinduet beholder samme vinner til omstart.
  Tidlig avslutning krever bekreftelse og beholder et allerede valgt vinnertall.
- Sekundærskjermen holdes over uavhengige lysbildefremvisninger. Hovedskjermen
  bruker ikke alltid-øverst, slik at operatøren fortsatt er tilgjengelig ved øving
  med én skjerm. Programmet støtter skjermenes individuelle DPI-skalering.
- Selvstendig Windows x64-MVP er publisert med profilen `Portable`
  i `artifacts\win-x64`. Ingen nettbaserte ressurser, mediefiler eller skrifter
  lastes ned ved kjøring. Lydene syntetiseres lokalt.

## Verifikasjon og versjonskontroll

Trekkmotoren har 29 automatiserte testtilfeller. En separat WPF-skjermtest bruker
midlertidige data og åpner ekte vinduer for å kontrollere validering, omvendt
gevinstrekkefølge, unike nummer, rundebytte, lukking av publikumsvindu og omstart.
Skjermtesten kontrollerer naturlig varighet, forskjellig tallbyttehastighet,
bokvisning før globalt loddnummer, bokpause, `Vis nå`, avvist/godkjent tidlig
avslutning, lukking under bokpause og omstart etter lukking av operatøren.
Lydkontrollen undersøker gyldig WAV-format, volumnivå og stillhet ved null volum,
og kaller den native avspillingen på lavt nivå før den stoppes.
Skjermtesten er også kjørt selvstendig med de nøyaktige app-/kjernedll-ene fra
den portable publiseringen. Publiseringsmappen inneholder .NET- og WPF-runtime.
Operatør- og publikumsskjermbilder er visuelt kontrollert.
Dette erstatter ikke sjekken av fysisk projektor, høyttalere, lesbarhet i rommet
og PowerPoint-fokus på arrangementets PC.

En utilgjengelig, allerede konfigurert pakkekilde ga feil ved første publisering.
Publiseringen lyktes med `-p:RestoreIgnoreFailedSources=true`, uten endring av
pakkekildene. Sårbarhetsoppslag rapporterte fortsatt NU1900-advarsler fra den
utilgjengelige kilden; disse er ikke applikasjonsfeil eller fullført
avhengighetssikkerhetskontroll.

Iterasjon 1 er committet som `29cbd5a` (`feat(core): add durable lottery engine and draw rules`).
Iterasjon 2 er committet som `3cf9a25`
(`feat(app): add Norwegian operator controls and round recovery`).
Iterasjon 3 følger med egen conventional commit for animasjon, lyd og tilhørende
tester og dokumentasjon.
