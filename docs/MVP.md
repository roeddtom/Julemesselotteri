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
- Etter bokvisning står bokas fulle 50-nummersintervall ved boknummeret, også
  under og etter loddvisning. Animasjonen velger hele lodd fra den aktuelle boka,
  begrenset til arrangementets intervall og uten usolgte lodd eller rundens
  tidligere vinnere. Den lagrede sluttvinneren og like vinnersjanser er uendret.
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
  med én skjerm. Med bare én tilkoblet skjerm brukes et vanlig, flyttbart og
  størrelsesjusterbart vindu i stedet for fullskjerm. Nye trekninger beholder
  plassering og størrelse i samme runde. Programmet støtter individuell DPI-skalering.
- Selvstendig Windows x64-MVP er publisert med profilen `Portable`
  i `artifacts\win-x64`. Ingen nettbaserte ressurser, mediefiler eller skrifter
  lastes ned ved kjøring. Lydene syntetiseres lokalt.

## Visuell profil

### Valgfri virtuell loddbok

Et eksempel kan aktiveres under **Visning og lyd** med
**Vis animert loddbok (eksempel)**. Innstillingen lagres, er avslått som standard
og leses ved starten av neste trekning. Den medfølgende `design\Forside loddbok.png`
er innebygd som forside. PDF-en `design\BookLottery_2026_Book_number_1.pdf`
er referanse for ti-raders tabeller, ikke et dokument som må rendres ved kjøring.
Programmet viser en stilisert bokåpning og sidevending, ikke fysisk papirkurl.
Forsidens boknummer/intervall og sluttmarkering genereres dynamisk.
Talltromlene beholdes gjennom begge trekkdelene og bokpausen. Først etter
avslørt loddnummer beholdes vinnertallet i 600 ms, så vises forsiden i 800 ms,
før boka åpnes og blar til riktig
tabellside med 300 ms per side. Vinnerraden markeres etter sidevendingen.
Det rekonstruerte oppslaget viser en informasjonsside og aktuell tabellside,
ikke en nøyaktig gjengivelse av duplex-arkenes trykkoppsett.
Forsiden og begge oppslagssidene har stående A5-proporsjoner (148:210).
Oppslaget er to forsidebredder, og tabellkolonnene tilpasses én side.

Valgt vinner, lagring, bokpause, tidsinnstillinger, avbrytelse og omstart bruker
samme trekkmotor som før. `Vis nå` og gjenopptatt vinner viser riktig side/rad.
Tabellsider beregnes fra fem grupper med ti nummer per bok, også ved
bokgrenser og lodd 4000 i bok 80. Skjermtesten dekker på/av, lagring av valget,
skjult bok under trekning, forside etter avslørt loddnummer, sidevending,
vinnerrad, sidemapping og avbrudd/gjenoppretting med funksjonen på.
Testvalget `--headless-booklet` kontrollerer sidemapping, A5-proporsjoner,
sidebredder, tabelltekstens plass og tallanimasjoner uten å vise vinduer.
Det kan lagre et offscreen-bilde av oppslaget. Dette erstatter ikke full
interaktiv skjermtest av plassering, operatørflyt og fysisk projektor.

Begge vinduer følger den medfølgende
`design\design_manual_misjonssambandet_versjon_1.3.pdf`.
Logoen fra `design\ungdomssenteret_logo.png` er innebygd uendret som WPF-ressurs.
Den står venstrestilt på mørk rød bakgrunn, med luft rundt, opprinnelige
proporsjoner og full opasitet (manualens sider 6–10). Ingen omfarging,
skygge, rotasjon eller andre effekter påføres logoen.

Felles farger er mørk rød `#420024`, rød `#FF004A`, dyp rød `#AB0038`,
krem `#F0ECD9`, mørk grå `#383838` og nøytral kant `#C1BFB1` (side 20).
Små opplysninger i vinnerlisten og operatørens statusfelt har mørk tekst på lys
bakgrunn. Store vinnertall har mørk rød tekst på krem. Ringer og kvartbuer
erstatter julepynt og gradienter, inspirert av formelementene på sidene 24–25.
Den separate ung-paletten er ikke brukt.

Skriftrekkefølgen er Rebond Grotesque, Work Sans, Arial (sidene 14–18).
Ingen lisensierte skrifter er kopiert fra PDF-en eller lastet ned.
Når profilskriftene ikke er installert, brukes Arial, slik manualen tillater.
Operatørens høyre panel kan rulles på små vinduer slik at kontroller ikke skjules.
Skjermtesten åpner begge vinduer, kontrollerer innebygd logo og lagrer valgfrie
skjermbilder, også med minste operatørstørrelse.

## Verifikasjon og versjonskontroll

Trekkmotoren har 29 automatiserte testtilfeller. En separat WPF-skjermtest bruker
midlertidige data og åpner ekte vinduer for å kontrollere validering, omvendt
gevinstrekkefølge, unike nummer, rundebytte, lukking av publikumsvindu og omstart.
Skjermtesten kontrollerer naturlig varighet, forskjellig tallbyttehastighet,
bokvisning før globalt loddnummer, bokpause, `Vis nå`, avvist/godkjent tidlig
avslutning, lukking under bokpresentasjonen og omstart etter lukking av operatøren.
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
