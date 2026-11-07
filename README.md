# VBE Rūšiuoklė (VbeSort)

VBE Rūšiuoklė yra interaktyvi mokymosi platforma, skirta praktikuotis praėjusių metų VBE (Valstybinio Brandos Egzamino) matematikos užduotis pagal temas ir pasitikrinti su oficialiais atsakymų lapais.

## Pagrindinės Funkcijos

- **Lietuviška Sąsaja**: Visi mygtukai, aprašymai ir meniu punktai yra lietuvių kalba.
- **Autentifikacijos ir Paskyrų Valdymo Sistema (CRUD)**:
  - ASP.NET Core Cookie autentifikacija su saugiu slaptažodžių šifravimu (`PasswordHasher`).
  - Mokytojo (Administratoriaus) skydelis (`/Admin`): mokinių peržiūra, naujų paskyrų kūrimas ir šalinimas.
  - Pradiniai vartotojai automatiškai užpildomi starto metu:
    - Administratorius: `admin` | Slaptažodis: `admin123`
    - Mokiniai: `lukre`, `ema`, `karina`, `tadas`, `valentinas` | Slaptažodis: `vbe2027`
- **Bandomasis Egzaminas (Mock Exam)**:
  - Atsitiktinių užduočių generavimas iš pasirinktų temų.
  - Integruotas **laikmatis (stopwatch)** su galimybe pristabdyti.
  - **Savęs įsivertinimo sistema**: mokinys pažymi atsakymus (Teisingai / Neteisingai), o sistema apskaičiuoja rezultatą, procentą ir sprendimo trukmę.
- **Užduočių filtravimas**: temos puslapyje galima filtruoti pagal metus (2014–2026 m.) ir sesiją (pagrindinė / pakartotinė).
- **Sistemos Statistika**: atskiras puslapis su užduočių pasiskirstymu pagal temas ir egzaminų aprėptį.

## Architektūra ir Technologijos

- **Karkasas**: ASP.NET Core 10.0 MVC (C#).
- **ORM / Duomenų Bazė**: Entity Framework Core.
  - Naudojamas **PostgreSQL** per `ConnectionStrings:DefaultConnection` arba `DATABASE_URL`.
- **Kodo apimtis**: kompaktiška ir švari architektūra (~850–900 kodo eilučių).

## Kaip Paleisti Vietoje

1. Atsidarykite terminalą projekto aplanke:
   ```bash
   dotnet run
   ```
2. Naršyklėje atidarykite terminale nurodytą adresą (pvz., `http://localhost:5000`).
3. Prisijunkite kaip `admin` (slaptažodis: `admin123`) arba kaip mokinys.
