# VBE Rūšiuoklė (VbeSort)

VBE Rūšiuoklė yra interaktyvi mokymosi platforma, skirta praktikuotis praėjusių metų VBE (Valstybinio Brandos Egzamino) matematikos užduotis pagal temas ir iškart pasitikrinti su oficialiais atsakymų lapais.

## Naujos Funkcijos

- **Visiškai Lietuviška Sąsaja**: Visi mygtukai, aprašymai ir meniu punktai yra išversti į lietuvių kalbą.
- **Autentifikacijos Sistema**: 
  - Sistema naudoja ASP.NET Core slapukų autentifikaciją (Cookie Authentication).
  - Vartotojų registracija išjungta – mokytojas (administratorius) suteikia prisijungimo duomenis savo mokiniams.
  - Vartotojų sąrašas konfigūruojamas `appsettings.json` faile. Standartiniai prisijungimai:
    - Prisijungimo vardas: `admin` | Slaptažodis: `admin123`
    - Prisijungimo vardas: `mokinys1` | Slaptažodis: `slaptazodis123`
- **Bandomasis Egzaminas (Mock Exam)**: 
  - Leidžia vartotojams pasirinkti norimas temas ir sugeneruoti atsitiktinį nurodyto ilgio (pvz., 10 klausimų) bandomąjį testą.
  - Generuojant egzaminą išrenkamos atsitiktinės užduotys iš pasirinktų temų, jos sumaišomos ir atvaizduojamos patogioje vieno puslapio sąsajoje kartu su paslėptais atsakymais, taip leidžiant mokiniui išspręsti testą ir iškart pasitikrinti klaidas.

## Architektūra 

- **Technologijos**: ASP.NET Core MVC (C#). Visiškai standartizuotas MVC karkasas su minimaliu kodo kiekiu (~800-1100 eilučių).
- **Duomenų struktūra**: Nėra jokios relacinės duomenų bazės. Sistema protingai (naudojant Regex ir modelius) nuskaito failų sistemą (`surusiuotos` ir `Atsakymai` aplankus) kiekvieną kartą paleidžiant programą.

## Kaip Paleisti Sistemą

1. Atidarykite terminalą ir pereikite į `vbesort` direktoriją:
   `cd c:\Users\VSama\OneDrive\Desktop\vbe\vbesort`
2. Paleiskite programą:
   `dotnet run`
3. Eikite į `http://localhost:<port>` (nuoroda bus matoma terminale).
4. Prisijunkite naudodami viršuje esančius duomenis, kad galėtumėte spręsti testus ar generuoti bandomąjį egzaminą!

## Diegimas į Render

Šiame kataloge esantis `Dockerfile` sukuria konteinerį su programa, `surusiuotos`
užduotimis ir `atsakymai` atsakymų paveikslėliais. `render.yaml` aprašo nemokamą
Render Web Service.

Prisijungimo duomenis nustatykite kaip Render aplinkos kintamuosius:
`Users__lukre`, `Users__ema`, `Users__karina`, `Users__tadas` ir
`Users__valentinas`. Jų nelaikykite viešame GitHub faile.
