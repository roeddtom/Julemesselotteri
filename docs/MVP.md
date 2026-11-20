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
  Publikum ser et stort vektorhjerte i profilens rødfarger rundt tromlene fra
  trekningsstart og rundt det ferdige vinnertallet. Tre tromler spinner;
  en fjerde vises først hvis 1000 trekkes. Alle fire får plass i hjertet. Boklodd beholder vanlig
  trommelvisning, og nye runder viser fortsatt ikke tomme tromler.
- Gevinster: én per linje, hovedgevinst øverst. Trekkes nedenfra og opp.
- Én trekk-knapp. Separat hastighet og varighet per lotteri.
- Nye runder skjuler gevinst og tomme talltromler til første trekning starter.
  Et ferdig resultat beholdes fram til neste trekning eller avsluttet runde.
- Alle talltromlene spinner kontinuerlig gjennom sifrene 0–9 med litt ulik fart.
  Etter konfigurert varighet sakker alle av samtidig og låses fra venstre mot høyre.
  Bevegelsen beregnes baklengs
  fra den lagrede vinneren uten hopp til slutt. Mellomtallene er dekorative
  og trenger ikke være kvalifiserte lodd. Dette endrer ikke vinnersjansene.
- Boklodd animerer globalt loddnummer direkte, uten egen boktrekning eller pause
  først. Boknummer vises under det ferdige resultatet, uten loddintervall.
  Vinnerlistene viser loddnummer og gevinst, uten boknummer.
- Tromlene beholder antall sifre, ledende nuller og plassering etter stopp og
  oppdatering fra lagret tilstand, slik at resultatet ikke hopper eller endrer størrelse.
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
  talltromler som sakker av, direkte loddtrekning, valgfri bokvisning og lokal lyd.
  `Vis nå` hopper til vinneren fra tromlene og bokvisningen. Lydløs stopper
  pågående lyd umiddelbart; volum kan endres mens tromlene spinner.
- Lukking av publikumsvinduet under animasjon setter trekningen på pause uten å
  forkaste vinneren. Lukking av operatørvinduet beholder samme vinner til omstart.
  Tidlig avslutning krever bekreftelse og beholder et allerede valgt vinnertall.
- Sekundærskjermen holdes over uavhengige lysbildefremvisninger. Hovedskjermen
  bruker ikke alltid-øverst, slik at operatøren fortsatt er tilgjengelig ved øving
  med én skjerm. Med bare én tilkoblet skjerm brukes et vanlig, flyttbart og
  størrelsesjusterbart vindu i stedet for fullskjerm. Nye trekninger beholder
  plassering og størrelse i samme runde. Programmet støtter individuell DPI-skalering.
- Operatørvinduet plasseres innenfor hovedskjermens arbeidsområde ved oppstart.
  Før publikumsvinduet vises eller plasseres på nytt, sikres hovedskjermen for
  operatøren. Publikum velges bare blant sekundærskjermer når de finnes, også
  når tidligere lagret skjermvalg peker på hovedskjermen eller en frakoblet skjerm.
  Et eksplisitt valg blant flere sekundærskjermer beholdes.
  Skjermoppdatering plasserer også et åpent publikumsvindu på nytt.
- Selvstendig Windows x64-MVP er publisert med profilen `Portable`
  i `artifacts\win-x64`. Ingen nettbaserte ressurser, mediefiler eller skrifter
  lastes ned ved kjøring. Lydklipp fra YleArkistos opptak av en mekanisk
  spilleautomat er innebygd; lisens og kreditering følger den portable utgaven.

## Visuell profil

### Valgfri virtuell loddbok

Et eksempel kan aktiveres under **Visning og lyd** med
**Vis animert loddbok (eksempel)**. Innstillingen lagres, er avslått som standard
og leses ved starten av neste trekning. Den medfølgende `design\Forside loddbok.png`
er innebygd som forside. PDF-en `design\BookLottery_2026_Book_number_1.pdf`
er referanse for ti-raders tabeller, ikke et dokument som må rendres ved kjøring.
Programmet viser en stilisert bokåpning og sidevending, ikke fysisk papirkurl.
Forsidens boknummer og sluttmarkering genereres dynamisk.
Talltromlene beholdes ved bokvisning; boka vises under vinnerlisten.
Etter avslørt loddnummer beholdes vinnertallet gjennom den konfigurerte
bokpausen, så vises forsiden i 1600 ms,
før boka åpnes og blar til riktig
tabellside med 600 ms per side. Forsidens innfading og åpning tar 500 ms,
og sidebevegelsen tar 440 ms, dobbelt så lenge som før.
Vinnerraden markeres etter sidevendingen.
Det rekonstruerte oppslaget viser bare «Vinner» og loddnummer på venstresiden,
og den aktuelle tabellen sentrert vertikalt på høyresiden,
ikke en nøyaktig gjengivelse av duplex-arkenes trykkoppsett.
Forsiden og begge oppslagssidene har stående A5-proporsjoner (148:210).
Oppslaget er to forsidebredder, og tabellkolonnene tilpasses én side.
Publikumsvinduets nederste slagord er fjernet. Vinnerlistens vertikale
rullefelt er skjult, men programmatisk rulling til nyeste vinner beholdes.
Tekstene øverst til høyre og sluttmeldingen «Gratulerer» er fjernet.
Venstresidens vinnertekst bruker 72 WPF-enheter og tallet 104, tilpasset sidebredden
med proporsjonal nedskalering for å unngå avkutting av lange loddnummer.
De 40 transparente PNG-radene i `design\bokloddrader` bygges inn i appen.
Hver av bokas 50 rader får et tilfeldig valgt dekorbilde med tilbakelegging,
vist proporsjonalt over navn- og telefonkolonnene uten å dekke loddnummeret.
Bildet beholdes ved sidevending og sluttmarkering i samme bokvisning.
Dette bruker `Random.Shared`, separat fra den kryptografiske vinnergeneratoren;
radbildene påvirker aldri trekningen og er ikke deltakerdata.

Valgt vinner, lagring, bokpause, tidsinnstillinger, avbrytelse og omstart bruker
samme trekkmotor som før. `Vis nå` og gjenopptatt vinner viser riktig side/rad.
Tabellsider beregnes fra fem grupper med ti nummer per bok, også ved
bokgrenser og lodd 4000 i bok 80. Skjermtesten dekker på/av, lagring av valget,
skjult bok under trekning, forside etter avslørt loddnummer, sidevending,
vinnerrad, sidemapping og avbrudd/gjenoppretting med funksjonen på.
Testvalget `--headless-booklet` kontrollerer sidemapping, A5-proporsjoner,
sidebredder, tabelltekstens plass, innebygde radbilder, kolonnedekning,
stabile radbilder ved sidevending og tallanimasjoner uten å vise vinduer.
Det kontrollerer også sentrert tabell, forenklet vinnerside, fjernet slagord
og automatisk rulling i vinnerlisten uten synlige rullefelt.
Ny-runde-visning, kontinuerlig spinn, felles nedbremsing, forskjøvet sifferstopp,
direkte loddtrekning, bokpause før valgfri bokvisning,
tregere sidevending, korrekt sluttall, avbrytelse og `Vis nå` kontrolleres også
uten vinduer. En regresjonstest kontrollerer at trommelelementer, posisjon og
størrelse beholdes etter stopp og oppdatering av vinnerlisten.
Hjerterammen kontrolleres ved trekningsstart og etter stopp, inkludert geometrisk
plass til fire sifre, skjermgrenser og at boklodd ikke beholder hjertet.
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

Trekkmotoren har 30 automatiserte testtilfeller. En separat WPF-skjermtest bruker
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

Fordelingstesten kjører 10 000 ekte `SelectWinner`-kall med standardgeneratoren
(`RandomNumberGenerator.GetInt32`) over 1–4000. Nytt arrangement mellom hvert
trekk gir trekking med tilbakelegging uten å endre reglene for faktiske lotterier.
Testutdata viser treffhistogram, maksimum, antall nummer over fem treff og
Pearson-statistikk. Konservative varslingsgrenser er 4800 for 4000 enkeltnummer
(3999 frihetsgrader), 120 for 40 sammenhengende grupper med 100 nummer
(39 frihetsgrader), og under 20 treff for hvert enkeltnummer.
Grupperingen gir 250 forventede treff per gruppe; enkeltnummertesten fanger
også skjevhet innad i gruppene. Grensene er diagnostiske, ikke en lovnad om
perfekt fordeling. Testen bruker ekte tilfeldighet og kan i sjeldne tilfeller
feile selv med en korrekt generator; et avvik skal undersøkes, ikke omgås
med gjentatte kjøringer til testen blir grønn.

Kjør bare fordelingstesten uten appvinduer:
`dotnet test tests\Julemesselotteri.Core.Tests --filter FullyQualifiedName~CryptographicDraws --logger "console;verbosity=detailed"`.
Første kontroll 17.11.2026 bestod: 10 000 trekk, maksimum 10 treff,
177 nummer med mer enn fem treff, Pearson 3968,80 for enkeltnummer og
52,39 for grupper. Alle 30 kjernetester bestod. Dette er ett utvalg, ikke
faste forventede resultater for neste kjøring.

`--headless-displays` kontrollerer skjermvalg med simulerte oppsett og
plassering i fysiske skjermkoordinater med skjulte vinduer. Det dekker
hovedskjerm, valgt sekundærskjerm, frakoblet valg og én-skjermsmodus, og
bekrefter at plasseringskallene ikke viser vinduene. Ved vanlig kjøring
plasseres nye vinduer før de vises, slik at de ikke først blinker på feil skjerm.

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
