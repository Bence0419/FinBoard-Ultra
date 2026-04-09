Pénzügyi Dashboard rendszerterv

# 1\. A rendszer célja

A rendszer célja egy személyes pénzügyi dashboard biztosítása, ahol a felhasználó biztonságos bejelentkezés után egy helyen tudja kezelni és áttekinteni:

- bevételeit,
- kiadásait,
- befektetéseit.

A rendszer elsődleges feladata nem könyvelési pontosság vagy banki integráció, hanem a személyes pénzügyi helyzet átlátható követése, rögzítése és elemzése.

# 2\. Funkcionális specifikáció

## 2.1. Fő funkciók

### 2.1.1. Felhasználói autentikáció

A felhasználó email-cím és jelszó segítségével tud bejelentkezni. A rendszer opcionális, de támogatott többtényezős hitelesítést (MFA) is biztosít telefonszám alapú megerősítéssel.

A funkció részei:

- regisztráció email + jelszó megadásával
- bejelentkezés email + jelszó párossal
- MFA-kód küldése telefonszámra
- MFA-kód ellenőrzése
- kijelentkezés
- elfelejtett jelszó kezelése
- jelszó módosítása

### 2.1.2. Dashboard megjelenítés

Sikeres bejelentkezés után a felhasználó a saját pénzügyi dashboardjára kerül. Itt összesítve látja a saját adatait.

A dashboard fő elemei:

- aktuális egyenleg
- havi összes bevétel
- havi összes kiadás
- befektetések összértéke
- legutóbbi tranzakciók
- kategóriák szerinti bontás
- időszakos statisztikák

### 2.1.3. Pénzügyi tételek kezelése

A felhasználó új pénzügyi tételt tud felvenni. Egy tétel lehet:

- bevétel
- kiadás
- befektetés

A felvétel során a rendszer bekéri a legfontosabb adatokat, például:

- típus
- összeg
- dátum
- kategória
- leírás
- pénznem
- opcionális megjegyzés

Befektetés esetén további mezők is szükségesek lehetnek, például:

- befektetés típusa
- darabszám
- egységár
- aktuális érték
- platform/szolgáltató

A felhasználó a meglévő tételeket is tudja:

- megtekinteni
- módosítani
- törölni
- szűrni
- rendezni

### 2.1.4. Kimutatások és szűrések

A felhasználó a pénzügyi adatait több szempont szerint elemezheti.

Lehetséges szűrési és riport funkciók:

- dátumintervallum szerinti szűrés
- kategória szerinti szűrés
- típus szerinti szűrés
- havi / heti / éves bontás
- bevétel-kiadás összehasonlítás
- befektetések teljesítményének áttekintése

### 2.1.5. Profilkezelés

A felhasználó kezelni tudja a saját profiladatait.

A funkció részei:

- név módosítása
- telefonszám beállítása vagy módosítása
- jelszó módosítása
- MFA be- és kikapcsolása
- preferált pénznem beállítása

## 3\. Nem funkcionális követelmények

### 3.1. Biztonság

Mivel pénzügyi adatokat kezel a rendszer, a biztonság kiemelten fontos.

Elvárások:

- jelszó titkosított tárolása hash formában
- MFA támogatás SMS vagy telefonos kód segítségével
- session vagy token alapú hitelesítés
- HTTPS kötelező használata
- jogosultságkezelés: a felhasználó csak a saját adatait láthatja
- input validáció minden adatbeviteli ponton
- rate limiting a bejelentkezésnél
- audit log a kritikus műveletekről

### 3.2. Használhatóság

- egyszerű, átlátható dashboard
- gyors adatfelvitel
- reszponzív felület
- könnyen érthető kategorizálás

### 3.3. Teljesítmény

- a dashboard betöltése legyen gyors
- listázás és szűrés nagyobb adatmennyiségnél is működjön
- aggregált adatok lekérdezése optimalizált módon történjen

### 3.4. Bővíthetőség

A rendszer később könnyen továbbfejleszthető legyen, például:

- banki API integráció
- automatikus tranzakcióimport
- több felhasználós családi pénzügyi mód
- költségkeret funkció
- értesítések és figyelmeztetések

## 4\. Szerepkörök

### 4.1. Elsődleges szereplő

**Felhasználó**  
A rendszer fő használója, aki a saját pénzügyi adatait kezeli.

### 4.2. Másodlagos szereplők

**Hitelesítési szolgáltatás**  
A belépés és MFA kezelés technikai hátterét biztosítja.

**SMS szolgáltató**  
Az MFA-kód kiküldését végzi.

## 5\. Részletes use case terv

Most szöveges use case formában is leírom.

### UC-01 - Regisztráció

**Cél:**  
Új felhasználó létrehozása.

**Elsődleges szereplő:**  
Felhasználó

**Előfeltétel:**  
A felhasználó még nem rendelkezik fiókkal.

**Utófeltétel:**  
A rendszer létrehozza a felhasználói fiókot.

**Alapfolyamat:**

- A felhasználó megnyitja a regisztrációs oldalt.
- Megadja az email-címét, jelszavát, nevét és opcionálisan a telefonszámát.
- A rendszer ellenőrzi az adatok helyességét.
- A rendszer ellenőrzi, hogy az email-cím még nem foglalt-e.
- A rendszer eltárolja a felhasználót.
- A rendszer visszajelzi a sikeres regisztrációt.

**Alternatív folyamatok:**

- 3a. Hibás email-formátum → a rendszer hibaüzenetet ad.
- 3b. Gyenge jelszó → a rendszer új jelszó megadását kéri.
- 4a. Az email már foglalt → a rendszer hibaüzenetet ad.

### UC-02 - Bejelentkezés emaillel és jelszóval

**Cél:**  
A felhasználó belép a rendszerbe.

**Elsődleges szereplő:**  
Felhasználó

**Előfeltétel:**  
A felhasználó már regisztrált.

**Utófeltétel:**  
A felhasználó hitelesített munkamenetet kap.

**Alapfolyamat:**

- A felhasználó megnyitja a bejelentkezési oldalt.
- Megadja email-címét és jelszavát.
- A rendszer ellenőrzi a hitelesítő adatokat.
- Ha az MFA nincs bekapcsolva, a rendszer bejelentkezteti a felhasználót.
- A felhasználó a dashboardra jut.

**Alternatív folyamatok:**

- 3a. Hibás email vagy jelszó → hibaüzenet.
- 3b. Túl sok sikertelen próbálkozás → ideiglenes zárolás.

### UC-03 - MFA ellenőrzés telefonszámmal

**Cél:**  
A rendszer második hitelesítési lépésben ellenőrizze a felhasználót.

**Elsődleges szereplő:**  
Felhasználó

**Másodlagos szereplő:**  
SMS szolgáltató

**Előfeltétel:**  
A felhasználónál az MFA engedélyezve van.

**Utófeltétel:**  
Sikeres ellenőrzés után a felhasználó beléphet.

**Alapfolyamat:**

- A rendszer a helyes email+jelszó ellenőrzés után MFA-kódot generál.
- A rendszer elküldi a kódot a felhasználó telefonszámára.
- A felhasználó megadja az érkezett kódot.
- A rendszer ellenőrzi a kódot.
- A rendszer bejelentkezteti a felhasználót.
- A dashboard megjelenik.

**Alternatív folyamatok:**

- 2a. SMS küldési hiba → a rendszer tájékoztatja a felhasználót.
- 4a. Hibás kód → újrapróbálkozás engedélyezett.
- 4b. Lejárt kód → új kód igénylése szükséges.

### UC-04 - Dashboard megtekintése

**Cél:**  
A felhasználó áttekinti a pénzügyi helyzetét.

**Elsődleges szereplő:**  
Felhasználó

**Előfeltétel:**  
A felhasználó be van jelentkezve.

**Utófeltétel:**  
A dashboard adatainak megtekintése megtörténik.

**Alapfolyamat:**

- A felhasználó belép a rendszerbe.
- A rendszer lekéri a felhasználó pénzügyi adatait.
- A rendszer kiszámolja az összesített értékeket.
- Megjelennek a fő mutatók és a legutóbbi tételek.
- A felhasználó áttekinti az adatokat.

**Alternatív folyamatok:**

- 2a. Nincs még rögzített adat → a rendszer üres dashboardot jelenít meg.
- 2b. Adatbetöltési hiba → a rendszer hibaüzenetet jelenít meg.

### UC-05 - Új pénzügyi tétel rögzítése

**Cél:**  
A felhasználó új bevételt, kiadást vagy befektetést vesz fel.

**Elsődleges szereplő:**  
Felhasználó

**Előfeltétel:**  
A felhasználó be van jelentkezve.

**Utófeltétel:**  
Az új tétel mentésre kerül.

**Alapfolyamat:**

- A felhasználó rákattint az „Új tétel hozzáadása" gombra.
- A rendszer megjeleníti az űrlapot.
- A felhasználó kiválasztja a típust: bevétel / kiadás / befektetés.
- A rendszer a típusnak megfelelő mezőket jeleníti meg.
- A felhasználó kitölti az adatokat.
- A rendszer ellenőrzi a mezőket.
- A rendszer elmenti a tételt.
- A dashboard frissül.

**Alternatív folyamatok:**

- 5a. Kötelező mező hiányzik → a rendszer figyelmeztet.
- 6a. Hibás összeg vagy dátum → hibaüzenet.
- 7a. Mentési hiba → a rendszer sikertelen műveletet jelez.

### UC-06 - Pénzügyi tétel módosítása

**Cél:**  
Egy meglévő tétel adatainak szerkesztése.

**Előfeltétel:**  
A felhasználó rendelkezik legalább egy rögzített tétellel.

**Alapfolyamat:**

- A felhasználó kiválaszt egy meglévő tételt.
- A rendszer megjeleníti a részleteket.
- A felhasználó módosítja az adatokat.
- A rendszer validálja az új értékeket.
- A rendszer menti a módosításokat.
- A dashboard és a lista frissül.

**Alternatív folyamatok:**

- 4a. Érvénytelen adat → a rendszer elutasítja a mentést.
- 5a. A tétel időközben nem elérhető → hibaüzenet.

### UC-07 - Pénzügyi tétel törlése

**Cél:**  
A felhasználó eltávolít egy rögzített tételt.

**Alapfolyamat:**

- A felhasználó kiválaszt egy tételt.
- Rákattint a törlés gombra.
- A rendszer megerősítést kér.
- A felhasználó megerősíti a törlést.
- A rendszer törli a tételt.
- A dashboard frissül.

**Alternatív folyamatok:**

- 4a. A felhasználó megszakítja a műveletet.
- 5a. Törlési hiba történik.

### UC-08 - Tételek listázása és szűrése

**Cél:**  
A felhasználó gyorsan megtalálja a releváns pénzügyi tételeket.

**Alapfolyamat:**

- A felhasználó megnyitja a tételek listáját.
- Beállítja a szűrési feltételeket.
- A rendszer lekéri a megfelelő rekordokat.
- A rendszer megjeleníti a szűrt listát.
- A felhasználó rendezheti az eredményeket.

### UC-09 - Befektetések áttekintése

**Cél:**  
A felhasználó külön nézetben lássa a befektetési adatait.

**Alapfolyamat:**

- A felhasználó megnyitja a befektetések nézetet.
- A rendszer megjeleníti a befektetések listáját.
- A rendszer kiszámítja az összértéket és a teljesítményt.
- A felhasználó áttekinti a portfólióját.

### UC-10 - Profil és biztonsági beállítások kezelése

**Cél:**  
A felhasználó karbantartja a fiókját.

**Alapfolyamat:**

- A felhasználó belép a profiloldalra.
- Módosítja a kívánt adatokat.
- A rendszer ellenőrzi az adatokat.
- A rendszer elmenti a változásokat.
- A rendszer visszajelzi a sikeres módosítást.

# 6\. Backend modell terv

Itt azt javaslom, hogy a backendben ne három teljesen külön táblával indulj, hanem legyen egy közös pénzügyi tételmodell, amit szükség esetén specializálsz. Ez egyszerűbb és tisztább indulásnak.

## 6.1. Fő entitások

### 6.1.1. User

A rendszer felhasználója.

**Fő attribútumok:**

- id
- email
- password_hash
- full_name
- phone_number
- mfa_enabled
- preferred_currency
- created_at
- updated_at
- last_login_at
- status

**Feladata:**  
Autentikáció, profiladatok, jogosultságok.

### 6.1.2. FinancialEntry

Minden pénzügyi tétel közös modellje.

**Fő attribútumok:**

- id
- user_id
- entry_type (income, expense, investment)
- amount
- currency
- category_id
- title
- description
- entry_date
- created_at
- updated_at
- is_deleted

**Feladata:**  
A bevételek, kiadások és befektetések közös tárolása.

### 6.1.3. Category

A tételek kategorizálására szolgál.

**Fő attribútumok:**

- id
- user_id
- name
- category_type (income, expense, investment, generic)
- created_at

**Példák:**

- Bevétel: fizetés, bónusz
- Kiadás: étel, lakbér, utazás
- Befektetés: részvény, ETF, crypto

### 6.1.4. InvestmentDetail

Csak a befektetés típusú tételekhez tartozó extra adatok.

**Fő attribútumok:**

- id
- financial_entry_id
- asset_name
- asset_type
- quantity
- buy_price
- current_price
- platform
- notes

**Feladata:**  
A befektetések specializált adatainak tárolása.

### 6.1.5. MfaChallenge

Az MFA-folyamat nyomon követésére.

**Fő attribútumok:**

- id
- user_id
- code_hash
- expires_at
- used_at
- created_at
- delivery_channel

**Feladata:**  
Az egyszer használatos MFA-kódok kezelése.

### 6.1.6. UserSession / RefreshToken

Ha tokenes autentikáció van.

**Fő attribútumok:**

- id
- user_id
- refresh_token_hash
- device_info
- ip_address
- expires_at
- revoked_at
- created_at

### 6.1.7. AuditLog

Biztonsági és üzleti szempontból fontos események naplózása.

**Fő attribútumok:**

- id
- user_id
- action_type
- entity_type
- entity_id
- metadata
- created_at
- ip_address

### 7\. Javasolt adatbázis-kapcsolatok

**Kapcsolatok**

- **User 1 - N FinancialEntry**
- **User 1 - N Category**
- **FinancialEntry 1 - 0..1 InvestmentDetail**
- **User 1 - N MfaChallenge**
- **User 1 - N UserSession**
- **User 1 - N AuditLog**

Ez egy jól normalizált, tiszta induló modell.

### 8\. Backend végpontterv

Ez már egy használható API-vázlat.

**Auth modul**

- POST /api/auth/register
- POST /api/auth/login
- POST /api/auth/mfa/verify
- POST /api/auth/logout
- POST /api/auth/forgot-password
- POST /api/auth/reset-password

**User modul**

- GET /api/users/me
- PUT /api/users/me
- PUT /api/users/me/password
- PUT /api/users/me/mfa

**Financial entry modul**

- GET /api/entries
- POST /api/entries
- GET /api/entries/{id}
- PUT /api/entries/{id}
- DELETE /api/entries/{id}

**Category modul**

- GET /api/categories
- POST /api/categories
- PUT /api/categories/{id}
- DELETE /api/categories/{id}

**Dashboard modul**

- GET /api/dashboard/summary
- GET /api/dashboard/charts
- GET /api/dashboard/recent-entries

**Investment modul**

- GET /api/investments
- GET /api/investments/summary

# 9\. Üzleti szabályok

Ez nagyon fontos része a specifikációnak.

- Egy felhasználó csak a saját rekordjait láthatja és módosíthatja.
- Minden pénzügyi tételhez kötelező a típus, összeg, dátum és kategória.
- Az összeg csak pozitív szám lehet; a típus határozza meg, hogy bevételről vagy kiadásról van szó.
- Befektetés típusnál további részletek is megadhatók.
- MFA csak érvényes telefonszámmal kapcsolható be.
- Egy MFA-kód időkorlátos és egyszer használható.
- Törlésnél célszerű soft delete-et használni, hogy az adatok visszakereshetők maradjanak.
- A dashboard aggregált adatokat számol a tételekből, nem külön tárolt kézi egyenleget használ.

# 10\. Javasolt technikai architektúra

Otthoni projektként ezt javaslom:

**Backend**

- Java Spring Boot vagy C# ASP.NET Core
- REST API
- JWT + refresh token
- PostgreSQL
- ORM: JPA/Hibernate vagy Entity Framework Core

**Frontend**

- React
- dashboard komponensek
- chart library a grafikonokhoz

**Biztonság**

- BCrypt / Argon2 jelszóhash
- JWT access token
- refresh token tárolás
- MFA service adapter

# 11\. Fejlesztési fázisokra bontott terv

**1\. MVP**

- regisztráció
- login
- dashboard alap nézet
- bevétel / kiadás / befektetés CRUD
- kategóriák
- alap statisztikák

**2\. Security bővítés**

- MFA telefonnal
- audit log
- rate limit
- jelszó reset

**3\. Analitika bővítés**

- grafikonok
- részletes szűrések
- befektetési teljesítmény

**4\. Haladó funkciók**

- export
- költségkeret
- értesítések
- külső integrációk

# 12\. Rövid összegzés

Ez a rendszer egy személyes pénzügyi nyilvántartó és elemző alkalmazás, amely:

- biztonságos bejelentkezést biztosít email/jelszó + MFA segítségével,
- lehetővé teszi bevételek, kiadások és befektetések kezelését,
- dashboardon összesített pénzügyi képet mutat,
- tiszta backend modellel és jól szétválasztott modulokkal felépíthető.

Szakmailag ez már egy teljesen vállalható beadandó-alap, mert van benne:

- funkcionális specifikáció,
- use case terv,
- backend domain modell,
- API terv,
- üzleti szabályok.
