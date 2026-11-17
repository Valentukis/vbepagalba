# VBE Rūšiuoklė

**VBE Rūšiuoklė** yra ASP.NET Core 10 MVC mokymosi platforma, skirta praktikuotis su 2014–2026 m. matematikos valstybinio brandos egzamino (VBE) užduotimis. Užduotys suskirstytos pagal temas, metus ir egzamino sesiją, prie kiekvienos užduoties pateikiamas oficialus atsakymas bei galimybė spręsti ant interaktyvios sprendimo lentos.

## Funkcijos

- **Užduočių peržiūra pagal temas (`/`, `/Home/Topic`):**
  - 17 VBE temų ir >460 užduočių iš 2014–2026 m. pagrindinės bei pakartotinės sesijų.
  - Filtravimas pagal metus, sesiją ir mokinio sprendimo būseną (*Nespręstos*, *Išspręstos*, *Atidėtos pakartoti*) bei atsitiktinės užduoties parinkimas.
- **Tikslių proporcijų klausimų ir atsakymų paveikslėliai:**
  - `PngDimensionReader` paleidimo metu nuskaito kiekvieno PNG failo matmenis iš `IHDR` antraštės, todėl paveikslėliai niekada neišsitempia virš savo tikrojo pločio.
  - Kelių dalių užduotys ir atsakymai (`(2)`, `(3)`, `(4)`) rikiuojami natūralia eilės tvarka.
  - Paspaudus ant bet kurio klausimo ar atsakymo paveikslėlio atsidaro pilno ekrano peržiūra (šviesdėžė) su mastelio keitimu.
- **Sprendimo lenta (juodraštis):**
  - Prie kiekvienos užduoties ir bandomajame egzamine mokinys gali atsidaryti braižymo lentą su langelių tinkleliu, spalvos pasirinkimu, trintuku, išvalymu ir keičiamu lentos aukščiu.
- **Pilnų egzaminų ir bandomųjų testų sprendimas (`/Home/MockExamSetup`, `/Home/MockExam`):**
  - Galimybė pasirinkti **pilną konkrečių metų ir sesijos egzaminą** (užkraunamos visos tos sesijos užduotys eilės tvarka `#1, #2, #3...`).
  - Galimybė generuoti bandomąjį egzaminą iš pasirinktų temų, tik iš dar nespręstų užduočių arba tik iš mokinio **atidėtų pakartojimui užduočių**.
  - Laikmatis su pristabdymu, savarankiškas įsivertinimas ir rezultato išsaugojimas mokinio profilyje.
- **Sesijos egzamino PDF generavimas (`/Home/ExamPdf`):**
  - `ExamPdfService` grynu C# kodu iš pasirinktų metų ir sesijos užduočių paveikslėlių suformuoja pilną A4 formato egzamino užduočių PDF dokumentą (be atsakymų).
- **Užduočių kartojimas ir mokinio profilis (`/Home/Practice`, `/Profile`):**
  - Užduočių žymėjimas (*Pažymėti išspręsta* / *Pakartoti vėliau*) ir asmeninės pastabos prie užduočių.
  - Kartojimo puslapis su 3 atskirais sąrašais: *Atidėtos pakartoti*, *Nespręstos*, *Išspręstos*.
  - Mokinio profilyje rodoma bendra pažanga, progresas pagal temas, bandomųjų egzaminų istorija, išsaugotos pastabos ir slaptažodžio keitimas.
- **Formulynas (`/Home/Formulas`):**
  - Tiesioginės nuorodos į oficialius Nacionalinės švietimo agentūros (NŠA) **B lygio** ir **A lygio** matematikos VBE formulių rinkinius (PDF).
- **Sistemos statistika ir administravimas (`/Home/Statistics`, `/Admin`):**
  - Temų ir užduočių pasiskirstymo statistika.
  - Vartotojų registracija, prisijungimas ir administratoriaus valdymo skydelis (vartotojų sąrašas, naujų paskyrų kūrimas, slaptažodžio atstatymas, trynimas).

## Naudojamos technologijos

- **ASP.NET Core 10 MVC** (C#)
- **Entity Framework Core 10** (PostgreSQL / Supabase ir SQLite palaikymas)
- **Bootstrap 5** ir individualus „Horizontai“ stiliaus CSS/JS
- **xUnit** automatizuotiems testams
- **Docker** ir **Render** diegimui

## Duomenų bazė

Prisijungimo eilutė nuskaitoma tokia tvarka:
1. `DATABASE_URL`
2. `ConnectionStrings__DefaultConnection`
3. `ConnectionStrings:DefaultConnection` iš `appsettings.json`

Paleidimo metu `DbInitializer` automatiškai sukuria ir atnaujina lenteles:
- `Users` – vartotojų paskyros ir rolės (`Student`, `Admin`).
- `ExerciseProgresses` – mokinio pažymėtos užduotys (*Išspręsta* / *Pakartoti vėliau*) ir asmeninės pastabos.
- `ExamAttempts` – atliktų bandomųjų egzaminų rezultatai.

## Pagrindiniai maršrutai (Endpoints)

| Metodas | Kelias | Prieiga | Aprašymas |
|---|---|---|---|
| GET | `/` arba `/Home/Index` | Visi | Pagrindinis puslapis su temų sąrašu ir paieška. |
| GET | `/Home/Topic?name={tema}` | Prisijungę | Pasirinktos temos užduotys su filtrais ir sprendimo lenta. |
| GET | `/Home/Practice?mode={due\|unseen\|solved}` | Prisijungę | Užduočių sąrašas pagal būseną (*Atidėtos pakartoti*, *Nespręstos*, *Išspręstos*). |
| GET | `/Home/MockExamSetup` | Prisijungę | Egzamino nustatymų forma (pilnas egzaminas pagal metus/sesiją arba pagal temas). |
| POST | `/Home/MockExam` | Prisijungę | Sugeneruoja ir atidaro bandomojo arba pilno egzamino sprendimo puslapį. |
| GET | `/Home/ExamPdf?year={m}&session={pagr\|pak}` | Visi | Sugeneruoja ir atidaro pasirinktos sesijos egzamino užduočių PDF dokumentą. |
| GET | `/Home/Formulas` | Visi | Nuorodos į oficialius NŠA B lygio ir A lygio VBE formulynus (PDF). |
| GET | `/Home/Statistics` | Visi | Sistemos statistika pagal temas ir metus. |
| GET | `/Profile` | Prisijungę | Mokinio profilis, temų progresas, egzaminų istorija, pastabos ir slaptažodžio keitimas. |
| POST | `/Profile/ChangePassword` | Prisijungę | Pakeičia prisijungusio vartotojo slaptažodį. |
| POST | `/Api/Progress` | Prisijungę | Išsaugo užduoties būseną (*Išspręsta* / *Pakartoti*) ir pastabą. |
| POST | `/Api/ExamAttempt` | Prisijungę | Išsaugo baigto bandomojo egzamino rezultatą. |
| GET/POST | `/Account/Login`, `/Account/Register` | Visi | Prisijungimas ir naujo mokinio registracija. |
| GET/POST | `/Admin`, `/Admin/Create`, `/Admin/ResetPassword`, `/Admin/Delete` | Admin | Paskyrų administravimas. |

## Paleidimas lokaliai ir testavimas

```powershell
# Paleisti aplikaciją lokaliai (http://localhost:5277)
dotnet run --launch-profile http

# Paleisti automatizuotus xUnit testus
dotnet test tests/VbeSort.Tests/VbeSort.Tests.csproj
```
