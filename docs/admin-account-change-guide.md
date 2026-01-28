# Návod: Změna Admin Účtu

## Přehled
Tento dokument popisuje postup pro nahrazení výchozího admin účtu `admin@localhost` novým účtem s vlastním emailem a silným heslem na produkčním serveru.

## Předpoklady
- Přístup k serveru přes SSH nebo konzoli
- PostgreSQL databázový klient (`psql`) nebo jiný DB nástroj
- Přístup k aplikačním konfiguračním souborům

---

## Postup

### Krok 1: Přihlaste se do databáze

```bash
# Přihlašte se jako postgres uživatel
sudo -u postgres psql -d aas_db
```

### Krok 2: Najděte stávajícího admin uživatele

```sql
SELECT "Id", "Email", "UserName" FROM "AspNetUsers" WHERE "Email" = 'admin@localhost';
```

### Krok 3: Aktualizujte email a uživatelské jméno

```sql
UPDATE "AspNetUsers" 
SET "Email" = 'vas-email@domena.cz',
    "NormalizedEmail" = 'VAS-EMAIL@DOMENA.CZ',
    "UserName" = 'vas-email@domena.cz',
    "NormalizedUserName" = 'VAS-EMAIL@DOMENA.CZ'
WHERE "Email" = 'admin@localhost';
```

### Krok 4: Změňte heslo (vyžaduje hash)

Pro změnu hesla máte dvě možnosti:

#### Možnost A: Použijte "Zapomenuté heslo" funkci
1. Přejděte na `/Identity/Account/ForgotPassword`
2. Zadejte nový email
3. Klikněte na reset link v emailu
4. Nastavte nové heslo

#### Možnost B: Vygenerujte nový hash hesla
V ASP.NET Core Identity se hesla hashují pomocí PBKDF2. Pro generování nového hashe můžete vytvořit jednoduchý skript:

```csharp
// Spusťte tento kód v dotnet konzoli
using Microsoft.AspNetCore.Identity;

var hasher = new PasswordHasher<object>();
var hash = hasher.HashPassword(null, "VaseNoveSilneHeslo123!");
Console.WriteLine(hash);
```

Poté aktualizujte databázi:
```sql
UPDATE "AspNetUsers" 
SET "PasswordHash" = '<vygenerovaný-hash>'
WHERE "Email" = 'vas-email@domena.cz';
```

### Krok 5: Aktualizujte konfiguraci (volitelné)

Pokud chcete, aby se nový admin účet vytvářel automaticky při dalším nasazení, aktualizujte `appsettings.Production.json`:

```json
{
  "Admin": {
    "Email": "vas-email@domena.cz",
    "Password": "VaseNoveSilneHeslo123!"
  }
}
```

> ⚠️ **Bezpečnostní upozornění**: Nikdy neukládejte hesla do konfiguračních souborů v čitelné podobě. Použijte environment variables nebo secret manager.

### Krok 6: Ověřte změny

1. Odhlaste se z aplikace
2. Přihlaste se s novým emailem a heslem
3. Ověřte, že máte admin oprávnění (vidíte "Admin Panel" v navigaci)

---

## Bezpečnostní doporučení

1. **Silné heslo**: Použijte minimálně 12 znaků, kombinaci velkých/malých písmen, číslic a speciálních znaků
2. **Unikátní email**: Použijte email, který aktivně monitorujete
3. **Dvoufaktorová autentizace**: Zvažte implementaci 2FA pro admin účty
4. **Pravidelná změna hesel**: Měňte admin heslo každé 3-6 měsíců
5. **Audit log**: Monitorujte přihlášení do admin sekce

---

## Řešení problémů

### "Invalid login attempt"
- Zkontrolujte, zda je email správně normalizovaný (velká písmena)
- Ověřte, že hash hesla je správný

### "User not found"
- Zkontrolujte, zda UPDATE příkaz proběhl úspěšně
- Ověřte hodnoty v databázi: `SELECT * FROM "AspNetUsers" WHERE "Email" ILIKE '%vas-email%'`

### Zapomenuté admin heslo bez přístupu k emailu
- Použijte možnost B výše pro přímý reset hesla v databázi

---

*Dokument vytvořen: Leden 2026*
