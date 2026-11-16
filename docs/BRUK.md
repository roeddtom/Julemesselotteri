# Brukerveiledning for forhåndsversjonen

Dette er en fungerende forhåndsversjon med **statisk vinnervisning**.
Slot-animasjon, bok-først-sekvens og lyd gjenstår før planlagt MVP er ferdig.
Test med de faktiske skjermene før programmet brukes på et arrangement.

## Starte

1. Pakk ut hele den portable mappen på en Windows x64-PC. Ikke flytt bare EXE-filen.
2. Koble til projektoren. Velg **Utvid** med `Win+P`, ikke Dupliser.
3. Start `Julemesselotteri.App.exe`. Programmet trenger ikke internett.
4. Velg projektoren under **Visning og lyd**. Bruk **Oppdater skjermer** ved behov.

Operatørvinduet er ikke fullskjerm. Publikumsvinduet åpnes separat på valgt
skjerm når en runde starter. På en PC med bare én skjerm vises begge vinduer der.
PowerPoint kjøres uavhengig; programmet starter, stopper eller bytter ikke lysbilder.
Ingen garanti gis for fokusrekkefølgen mellom PowerPoint og andre åpne programmer.

## Gevinster

Skriv én gevinst per linje for hvert lotteri. Hovedgevinsten skal stå øverst.
Programmet trekker nederste linje først. Tomme linjer ignoreres.
Endringer lagres automatisk etter en kort pause; se meldingen nederst i vinduet.
Ugyldige felt må rettes før en ny runde kan startes.

## Hjertelodd

1. Velg **Ny hjerterunde**. Gevinstlisten låses for runden.
2. Velg **Trekk neste gevinst** for hver gevinst. Nummeret blir vist direkte.
3. Samme nummer kan ikke trekkes igjen innen runden. Ulike nummer på samme
   fysiske hjerte kan vinne; appen har ikke en kartlegging fra nummer til hjerter.
4. Siste vinner blir stående. Velg **Avslutt runde** når dere er klare.
   Publikumsvinduet lukkes, og PowerPoint kan vises igjen.
5. Neste hjerterunde bruker samme gevinster, men alle nummer 1–1000 er ledige igjen.

En uferdig runde kan avsluttes tidlig, med bekreftelse.
Det er også mulig å starte en ny hjerterunde fra en aktiv hjerterunde, med bekreftelse.
Gevinster som ikke ble trukket, blir ikke loggført som utdelt.

## Boklodd

1. Sjekk gevinstlisten. Angi første og siste **globale** loddnummer, begge inkludert.
2. Registrer hele usolgte bøker, for eksempel `2, 5-7`.
3. Registrer usolgte globale loddnummer, for eksempel `12-18, 83`.
   Komma, semikolon, mellomrom og linjeskift kan skille intervallene.
4. Kontroller antall ledige nummer. Velg **Start boklotteri**.
5. Trekk hver gevinst. Appen viser både loddnummer og boknummer.
   Slå opp navn/telefon i papirboka; opplysningene skal ikke legges inn i appen.
6. Avslutt runden etter siste gevinst. Bokvinnere beholdes som utelukket i hele
   arrangementet, også etter omstart og ved oppstart av en ny bokrunde.

Bok 1 har 1–50, bok 2 har 51–100, osv. Lodd 73 vises som lodd 73 i bok 2.
En delvis solgt bok gir ikke dårligere vinnersjanse per solgte lodd enn en full bok.
Det velges ett lodd fra alle kvalifiserte lodd, ikke først en tilfeldig bok.

## Omstart og filer

- Tilstand: `%LOCALAPPDATA%\Julemesselotteri\state.json`.
- Forrige lagrede tilstand: `state.json.bak` i samme mappe.
- Logger: `Dokumenter\Julemesselotteri`, én tidsstemplet TXT-fil per runde.
- **Åpne logger** åpner loggmappen i Filutforsker.

Et vinnertall lagres før det vises. Etter et avbrudd tilbys **Fortsett trekning**
eller **Vis nå** med samme lagrede vinner. Det trekkes ikke et nytt nummer.
Hvis bare publikumsvinduet lukkes, kan det åpnes igjen fra operatørvinduet.
`Esc` i publikumsvinduet lukker det uten å avslutte runden.

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
   etter at publikumsvinduet lukkes.
3. Kontroller at loggmappen kan skrives til og at TXT-filen inneholder riktig
   gevinst og nummer. Ta sikkerhetskopi til en annen fysisk enhet.
4. Gjennomfør en omstart i øvingsarrangementet og kontroller samme vinnerhistorikk.
5. Start et nytt arrangement for de virkelige trekningene. Dobbeltsjekk usolgte
   bøker/lodd sammen med en annen person.
6. Slå av hvilemodus og varsler. Ha papirnotater som uavhengig reserve.
