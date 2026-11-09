# VBE Rusiuokle

VBE Rusiuokle yra ASP.NET Core MVC mokymosi platforma, skirta praktikuotis su ankstesniu metu VBE matematikos uzduotimis. Uzduotys suskirstytos pagal temas, metus ir egzamino sesija. Prie kiekvienos uzduoties galima perziureti oficialu atsakyma.

## Funkcijos

- Uzduociu perziura pagal temas.
- Uzduociu filtravimas pagal metus ir sesija.
- Sistemos statistika: temu ir uzduociu skaicius bei uzduociu pasiskirstymas pagal temas.
- Bandomojo egzamino generavimas is pasirinktu temu.
- Bandomojo egzamino laikmatis su pristabdymu.
- Savarankiskas atsakymu ivertinimas ir galutinio rezultato skaiciavimas.
- Prisijungimas kaip mokinys arba kaip administratorius
- Administravimo puslapis vartotojams perziureti, kurti ir istrinti.
- Slaptazodziai saugomi ne atviru tekstu, o naudojant ASP.NET Core `PasswordHasher`.

## Naudojamos technologijos

- ASP.NET Core 10 MVC.
- Supabase PostgreSQL nuotolinei duomenu bazei.
- Bootstrap vartotojo sasajai.
- Docker ir Render paleidimui.

## Duomenu baze

Aplikacija naudoja tik PostgreSQL.

Prisijungimo eilute paimama tokia tvarka:

1. `DATABASE_URL`
2. `ConnectionStrings__DefaultConnection`
3. `ConnectionStrings:DefaultConnection` is `appsettings.json`

`DATABASE_URL` gali buti PostgreSQL URL formatu, pavyzdziui:

```text
postgresql://vartotojas:slaptazodis@hostas:5432/postgres
```

Taip pat tinka standartine PostgreSQL connection string forma:

```text
Host=hostas;Port=5432;Database=postgres;Username=vartotojas;Password=slaptazodis;SSL Mode=Require;Trust Server Certificate=true
```

Paleidimo metu `DbInitializer` sukuria `Users` lentele, jei jos dar nera, ir tuscioje duomenu bazeje sukuria pradinius vartotojus

## Endpointai

Visi endpointai naudoja MVC marsruta:

```text
/{controller}/{action}/{id?}
```

### Pagrindiniai puslapiai

| Metodas | Kelias | Prieiga | Aprasymas |
|---|---|---|---|
| GET | `/` arba `/Home/Index` | Visi | Pagrindinis puslapis su temu sarasu. |
| GET | `/Home/Statistics` | Visi | Sistemos statistika: temos, uzduotys, metu apimtis ir uzduociu pasiskirstymas pagal temas. |
| GET | `/Home/Topic?name={tema}` | Prisijunge vartotojai | Parodo pasirinktos temos uzduotis. Jei tema neegzistuoja, grazinamas `404 Not Found`. |
| GET | `/Home/Error` | Visi | Bendras klaidos puslapis ne vystymo aplinkoje. |

### Bandomasis egzaminas

| Metodas | Kelias | Prieiga | Aprasymas |
|---|---|---|---|
| GET | `/Home/MockExamSetup` | Prisijunge vartotojai | Parodo egzamino nustatymu forma: temu pasirinkima ir uzduociu skaiciu. |
| POST | `/Home/MockExam` | Prisijunge vartotojai | Atsitiktinai parenka uzduotis is pasirinktu temu ir sugeneruoja egzamino puslapi. Uzduociu skaicius apribotas nuo 1 iki 50. |

Rezultatas skaiciuojamas narsykleje: vartotojas pazymi, kurios uzduotys issprestos teisingai, o puslapis parodo teisingu atsakymu skaiciu, procenta ir sprendimo laika. Rezultatai i duomenu baze nesaugomi.

### Prisijungimas

| Metodas | Kelias | Prieiga | Aprasymas |
|---|---|---|---|
| GET | `/Account/Login` | Visi | Prisijungimo forma. Gali priimti `returnUrl`, kad po prisijungimo vartotojas butu grazintas i pradini puslapi. |
| POST | `/Account/Login` | Visi | Patikrina vartotojo varda ir slaptazodi, sukuria cookie autentifikacijos sesija ir priskiria vartotojo role. |
| GET | `/Account/Logout` | Prisijunge vartotojai | Nutraukia autentifikacijos sesija ir grazina i pagrindini puslapi. |
| GET | `/Account/AccessDenied` | Visi | Parodo pranesima, kai vartotojas neturi reikiamu teisiu. |

### Administravimas

Visi administravimo endpointai reikalauja `Admin` roles.

| Metodas | Kelias | Prieiga | Aprasymas |
|---|---|---|---|
| GET | `/Admin` arba `/Admin/Index` | Admin | Parodo visu vartotoju sarasa ir paskyros kurimo forma. |
| POST | `/Admin/Create` | Admin | Sukuria vartotoja su `Mokinys` arba `Admin` role. Tikrinamas vartotojo vardo unikalumas, o slaptazodis suhashinamas. |
| POST | `/Admin/Delete?id={id}` | Admin | Istrina nurodyta vartotoja. Administratorius negali istrinti pats saves. |

`Create` ir `Delete` endpointai naudoja antiforgery token, todel juos iprastai reikia kviesti per administravimo formose pateiktus veiksmus.

### Statiniai failai

| Kelias | Aprasymas |
|---|---|
| `/surusiuotos/...` | Uzduociu paveiksleliai pagal temas. |
| `/atsakymai/...` | Atsakymu failai, naudojami uzduociu puslapiuose. |
| `/lib/...`, `/css/...`, `/js/...` | Bibliotekos ir vartotojo sasajos statiniai failai. |

## Duomenu failu struktura

Uzduotys skaitomos is `surusiuotos` katalogo, o atsakymai is `atsakymai` katalogo. Failu varduose naudojamas formatas su metais, sesija ir uzduoties numeriu, pavyzdziui:

```text
2024_pagr_12.png
2024_pak_3.png
```

`ExerciseService` paleidimo metu surenka failus i temas ir susieja juos su atsakymais. Bazinis katalogas nustatomas per `VbeConfig:BasePath` (pagal nutylejima aplikacijos katalogas)

## Paleidimas lokaliai

Reikalingas .NET 10 SDK ir veikianti PostgreSQL duomenu baze.

1. Nustatykite connection string viena is palaikomu budu:

   ```powershell
   $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=vbe;Username=postgres;Password=slaptazodis"
   ```

   arba irasykite ja i `appsettings.json`.

2. Paleiskite projekta:

   ```powershell
   dotnet run
   ```

3. Atidarykite terminale parodyta adresa. Lokalios paleidimo nuostatos yra `Properties/launchSettings.json`.

## Paleidimas su Docker

Projektas turi `Dockerfile`, kuris:

1. Atstato NuGet priklausomybes.
2. Sukompiliuoja ir publikuoja aplikacija.
3. Nukopijuoja `surusiuotos` ir `atsakymai` katalogus i galutini image.
4. Paleidzia aplikacija per 10000 porta.

Pavyzdys:

```powershell
docker build -t vbe-rusiuokle .
docker run -p 10000:10000 `
  -e ConnectionStrings__DefaultConnection="Host=hostas;Port=5432;Database=postgres;Username=vartotojas;Password=slaptazodis;SSL Mode=Require;Trust Server Certificate=true" `
  vbe-rusiuokle
```

Render konfiguracija yra faile `render.yaml`. Nuotoline connection string ten perduodama kaip slapta `ConnectionStrings__DefaultConnection` aplinkos reikšmė.
