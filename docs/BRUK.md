# Brukerveiledning

MVP for hjertelodd og boklodd med animert vinnervisning og lyd.
Test med de faktiske skjermene før programmet brukes på et arrangement.

Begge vinduer bruker Ungdomssenterets logo og fargeprofil fra designmanualen.
Logoen følger med innebygd, også ved frakoblet drift. Profilskriftene Rebond
Grotesque og Work Sans brukes hvis de finnes på PC-en; ellers brukes Arial
som manualens systemreserve. Ingen ekstra skrifter må installeres.

## Starte

1. Pakk ut hele den portable mappen på en Windows 10/11 x64-PC. Ikke flytt bare EXE-filen.
2. Koble til projektoren. Velg **Utvid** med `Win+P`, ikke Dupliser.
3. Start `Julemesselotteri.App.exe`. Programmet trenger ikke internett.
4. Velg projektoren under **Visning og lyd**. Bruk **Oppdater skjermer** ved behov.

Operatørvinduet er ikke fullskjerm og åpnes på hovedskjermen angitt i Windows.
Når publikumsvinduet åpnes eller plasseres på nytt, flyttes operatørvinduet
tilbake til hovedskjermen hvis det står på en annen skjerm.
Publikumsvinduet åpnes separat på en annen skjerm når en runde starter.
Med flere tilkoblede skjermer viser skjermvalget bare sekundærskjermer.
Et lagret valg av hovedskjermen eller en frakoblet skjerm erstattes automatisk
med en tilgjengelig sekundærskjerm. Med bare én tilkoblet skjerm åpnes det som et vanlig,
ikke-maksimert vindu med tittellinje, som kan flyttes og endres i størrelse.
Vinduets plassering og størrelse beholdes mellom trekningene i samme runde.
Med flere tilkoblede skjermer fyller publikumsvinduet den valgte skjermen.
**Oppdater skjermer** oppdaterer valgene og plasserer åpne vinduer på nytt.
PowerPoint kjøres uavhengig; programmet starter, stopper eller bytter ikke lysbilder.
På en sekundærskjerm holdes publikumsvinduet øverst, over lysbildefremvisningen.
På hovedskjermen holdes det ikke alltid øverst, slik at operatørvinduet er
tilgjengelig også ved øving uten projektor. Kontroller fokusrekkefølgen med
PowerPoint på den faktiske PC-en.

## Gevinster

Skriv én gevinst per linje for hvert lotteri. Hovedgevinsten skal stå øverst.
Programmet trekker nederste linje først. Tomme linjer ignoreres.
Endringer lagres automatisk etter en kort pause; se meldingen nederst i vinduet.
Ugyldige felt må rettes før en ny runde kan startes.

## Hjertelodd

1. Velg **Ny hjerterunde**. Gevinstlisten låses for runden.
2. Velg **Trekk neste gevinst** for hver gevinst. Tromlene viser vinneren når
   animasjonen er ferdig; **Vis nå** hopper direkte til resultatet.
3. Samme nummer kan ikke trekkes igjen innen runden. Ulike nummer på samme
   fysiske hjerte kan vinne; appen har ikke en kartlegging fra nummer til hjerter.
4. Siste vinner blir stående. Velg **Avslutt runde** når dere er klare.
   Publikumsvinduet lukkes, og PowerPoint kan vises igjen.
5. Neste hjerterunde bruker samme gevinster, men alle nummer 1–1000 er ledige igjen.

En uferdig runde kan avsluttes tidlig, med bekreftelse. Et allerede valgt nummer
beholdes som vinner selv om animasjonen ikke ble fullført.
En ny hjerterunde kan startes direkte etter en ferdig runde uten å endre gevinster.
Hvis forrige hjerterunde er uferdig, kreves bekreftelse.
Gevinster som ikke ble trukket, blir ikke loggført som utdelt.

## Boklodd

1. Sjekk gevinstlisten. Angi første og siste **globale** loddnummer, begge inkludert.
2. Registrer hele usolgte bøker, for eksempel `2, 5-7`.
3. Registrer usolgte globale loddnummer, for eksempel `12-18, 83`.
   Komma, semikolon, mellomrom og linjeskift kan skille intervallene.
4. Kontroller antall ledige nummer. Velg **Start boklotteri**.
5. Trekk hver gevinst. Appen viser først boknummeret med bokas nummerintervall,
   for eksempel **Bok 2 · Lodd 51–100**, venter den innstilte pausen og animerer
   deretter bare loddnummer i denne boka som er innenfor arrangementets intervall,
   solgt og ikke allerede trukket i runden. **Vis nå** avslutter hele
   sekvensen, også hvis bokdelen eller pausen fortsatt pågår.
   Slå opp navn/telefon i papirboka; opplysningene skal ikke legges inn i appen.
6. Avslutt runden etter siste gevinst. Bokvinnere beholdes som utelukket i hele
   arrangementet, også etter omstart og ved oppstart av en ny bokrunde.

Bok 1 har 1–50, bok 2 har 51–100, osv. Lodd 73 vises som lodd 73 i bok 2.
En delvis solgt bok gir ikke dårligere vinnersjanse per solgte lodd enn en full bok.
Det velges ett lodd fra alle kvalifiserte lodd, ikke først en tilfeldig bok.

## Animasjon og lyd

Under **Visning og lyd**, ved innstillingene for boklodd, kan du slå på
**Vis animert loddbok (eksempel)**. Valget lagres og er avslått som standard,
slik at eksisterende oppsett beholder talltromlene. Endringer gjelder neste
trekning; de endrer ikke en animasjon som allerede pågår.

Talltromlene vises gjennom hele bok- og loddtrekningen, også i bokpausen.
Først når det endelige loddnummeret er avslørt, viser eksemplet den medfølgende
forsiden i et kort øyeblikk, åpner boka og
blar i rekonstruerte tabellsider med ti lodd på hver side. Riktig side og rad
markeres til slutt. For lodd 73 vises bok 2, tabellsiden 71–80 og tredje rad.
Hver side har stående A5-proporsjoner. Tabellsiden er like bred som forsiden;
et åpent oppslag består av to slike sider.
Navn og telefon er tomme; programmet leser ikke håndskrift fra papirbøkene.
Boknummer og antall bøker på forsiden beregnes fra trekkeoppsettet.
**Vis nå** hopper direkte til riktig side og vinnerrad. Uten valget brukes
den vanlige tallanimasjonen. Hjertelodd påvirkes ikke.

Under **Visning og lyd** har hvert lotteri egne innstillinger:

- Varighet: 0,5–60 sekunder. For boklodd gjelder dette hver av de to delene;
  samlet tid er omtrent to ganger varigheten pluss bokpausen. Med animert
  loddbok kommer forside og sidevending i tillegg etter at tallet er avslørt
  (omtrent 1,7–2,9 sekunder, inkludert et kort opphold med vinnertallet synlig).
- Tallbytte: 30–500 millisekunder. Lavere verdi gir raskere tromler.
  Tromlene sakker gradvis av mot slutten uten å endre valgt vinner.
- Bokpause: 0–30 sekunder mellom ferdig boknummer og loddnummer.
- Volum: 0–100. Bruk **Lydløs** for å stoppe lyd umiddelbart.
  Volumendringer brukes på neste lydsignal; de kan gjøres mens trekningen pågår.
- **Test lyd** spiller vinnersignalet med valgt volum/lydløs når ingen animasjon pågår.

Tidsendringer gjelder neste trekning. Vinnertallet bestemmes og lagres før
animasjonen; de tilfeldige sifrene underveis påvirker ikke utfallet.
Ingen ekstern musikk eller nettforbindelse brukes. Windows må ha en fungerende
lydutgang; sjekk også Windows-volum, programvolum og eventuell HDMI-lydutgang.

## Omstart og filer

- Tilstand: `%LOCALAPPDATA%\Julemesselotteri\state.json`.
- Forrige lagrede tilstand: `state.json.bak` i samme mappe.
- Logger: `Dokumenter\Julemesselotteri`, én tidsstemplet TXT-fil per runde.
- **Åpne logger** åpner loggmappen i Filutforsker.

Et vinnertall lagres før det vises. Etter et avbrudd tilbys **Fortsett trekning**
eller **Vis nå** med samme lagrede vinner. Det trekkes ikke et nytt nummer.
Hvis bare publikumsvinduet lukkes, kan det åpnes igjen fra operatørvinduet.
Under en animasjon settes den da på pause med samme lagrede vinnertall.
Velg **Fortsett trekning** for ny visning av animasjonen, eller **Vis nå** for
direkte visning. `Esc` i publikumsvinduet lukker det uten å avslutte runden.

Logger kan gjenskapes ved neste programstart fra gjeldende arrangementstilstand.
Et loggproblem vises som advarsel; selve vinneren er fremdeles lagret i tilstandsfilen.
Ikke start et nytt arrangement før loggene er kontrollert.

Ved ugyldig tilstandsfil stopper oppstarten. **Ikke slett filen for å fortsette.**
Ta kopi av både tilstand, sikkerhetskopi og logger. Kontroller alle vinnere mot
papirnotater før eventuell manuell gjenoppretting fra `.bak`, som kan være eldre
enn siste trekning. Programmet gjenoppretter ikke en eldre kopi automatisk.

**Nytt arrangement** bekrefter en nullstilling av vinnerhistorikken; oppsett og
eksisterende TXT-logger beholdes. Dette er ikke knappen for neste hjerterunde.

## Sjekkliste før arrangement

1. Kjør en øvingsrunde og bekreft lesbarhet fra bakerst i rommet.
2. Kontroller skjermvalg, Windows-skalering og at PowerPoint fortsatt er synlig
   etter at publikumsvinduet lukkes. Bruk **Test lyd** og kontroller både
   tallbyttelyden og vinnersignalet på den faktiske lydutgangen.
3. Kontroller at loggmappen kan skrives til og at TXT-filen inneholder riktig
   gevinst og nummer. Ta sikkerhetskopi til en annen fysisk enhet.
4. Gjennomfør en omstart i øvingsarrangementet og kontroller samme vinnerhistorikk.
5. Start et nytt arrangement for de virkelige trekningene. Dobbeltsjekk usolgte
   bøker/lodd sammen med en annen person.
6. Slå av hvilemodus og varsler. Ha papirnotater som uavhengig reserve.
