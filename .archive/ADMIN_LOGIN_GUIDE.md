# Admin Login Guide - Aristocratic Artwork Sale

## 🔐 Přihlašovací údaje

### Development prostředí:
- **Email:** `admin@localhost`
- **Heslo:** `Admin123!@#$`

### Požadavky na heslo:
- Minimálně 12 znaků
- Musí obsahovat velké písmeno
- Musí obsahovat speciální znak
- Musí obsahovat číslo

## 📍 Jak se přihlásit

### Krok 1: Přejděte na přihlašovací stránku
Klikněte na **"Login"** v pravém horním rohu navigace

Nebo přímo: `https://your-domain.com/Identity/Account/Login`

### Krok 2: Zadejte údaje
```
Email: admin@localhost
Password: Admin123!@#$
```

### Krok 3: Po přihlášení
Po úspěšném přihlášení se v navigaci objeví tlačítko **"✦ Admin Panel"**

## 🛠️ Admin Panel - Funkce

### Přístup k admin panelu:
- URL: `/Admin/Collections`
- Zobrazí se automaticky v navigaci po přihlášení

### Funkce admin panelu:

#### 1. **Přehled kolekcí (Index)**
- Zobrazí všechny kolekce
- Ukáže počet obrázků u každé kolekce
- Řazeno od nejnovějších

#### 2. **Přidání nové kolekce (Create)**
URL: `/Admin/Collections/Create`

**Povinné pole:**
- **Title** - Název kolekce
- **Description** - Popis
- **Category** - Kategorie (Paintings, Jewelry, Watches, Statues, Other)
- **Images** - Minimálně 1 obrázek

**Volitelné:**
- **Audio** - MP3 soubor (max 15MB)

**Omezení:**
- Max velikost obrázku: 10MB
- Povolené formáty obrázků: JPG, JPEG, PNG, WEBP
- Celková velikost uploadu: max 100MB

#### 3. **Editace kolekce (Edit)**
URL: `/Admin/Collections/Edit/{id}`

**Můžete upravit:**
- Title (automaticky aktualizuje slug)
- Description
- Category
- Přidat nové obrázky

**Poznámka:** Existující obrázky nelze smazat z edit formu (bezpečnostní důvod)

#### 4. **Automatické funkce:**
- **Slug generování** - Automaticky z názvu
- **Image varianty** - Automaticky vytvoří 3 velikosti:
  - 480px (thumbnail)
  - 960px (medium)
  - 1600px (large)
- **Transakce** - Pokud selže upload obrázků, kolekce se neuloží

## 🔧 Konfigurace

### Nastavení admin účtu:

#### Production prostředí:
Nastavte environment variables:
```bash
ADMIN_EMAIL=your-email@example.com
ADMIN_PASSWORD=YourSecurePassword123!@#
```

#### Development prostředí:
Upravte `appsettings.Development.json`:
```json
{
  "Admin": {
    "Email": "admin@localhost",
    "Password": "Admin123!@#$"
  }
}
```

## 🐛 Řešení problémů

### Problém 1: Nelze se přihlásit
**Řešení:**
1. Zkontrolujte heslo - musí splňovat požadavky (12+ znaků, velké písmeno, speciální znak)
2. Zkontrolujte, jestli je admin účet vytvořen:
   - Podívejte se do logů při startu aplikace
   - Měli byste vidět: "Admin account created successfully: admin@localhost"

### Problém 2: Admin Panel se nezobrazuje
**Řešení:**
1. Ujistěte se, že jste přihlášeni pod admin účtem
2. Zkontrolujte, že účet má roli "Admin"
3. Odhlaste se a přihlaste znovu

### Problém 3: Nelze nahrát obrázky
**Řešení:**
1. Zkontrolujte velikost souboru (max 10MB na obrázek)
2. Zkontrolujte formát (jen JPG, PNG, WEBP)
3. Ujistěte se, že máte práva k zápisu do `wwwroot/uploads/images`

### Problém 4: Database connection error
**Řešení:**
1. Zkontrolujte connection string v `appsettings.Development.json`
2. Ujistěte se, že PostgreSQL běží
3. Zkontrolujte credentials

## 📝 Databázové modely

### Collection
```csharp
- Id (int, auto)
- Title (string, required)
- Slug (string, unique)
- Description (string)
- Category (enum)
- AudioPath (string, nullable)
- CreatedUtc (DateTime)
- Images (List<CollectionImage>)
```

### CollectionImage
```csharp
- Id (int, auto)
- CollectionId (int, FK)
- FileName (string) - bez přípony
- Width (int)
- Height (int)
- Bytes (long)
- SortOrder (int)
```

## 🎯 Workflow pro přidání kolekce

1. Přihlaste se jako admin
2. Klikněte na "✦ Admin Panel" v navigaci
3. Klikněte "Create New"
4. Vyplňte formulář:
   - Zadejte název (Title)
   - Napište popis (Description)
   - Vyberte kategorii (Category)
   - Nahrajte obrázky (min. 1)
   - Volitelně: nahrajte audio
5. Klikněte "Create"
6. Kolekce se objeví na veřejné stránce `/Collections`

## 🔒 Bezpečnost

### Implementované funkce:
- ✅ Role-based authorization
- ✅ AntiForgery tokens na všech formech
- ✅ File type validation
- ✅ File size limits
- ✅ Transaction rollback při chybě
- ✅ SQL injection protection (EF Core)
- ✅ XSS protection
- ✅ CSRF protection

### Doporučení:
1. V produkci použijte silné heslo (20+ znaků)
2. Nikdy nesdílejte admin credentials
3. Pravidelně měňte hesla
4. Monitorujte admin panel aktivity

## 📧 Kontakt

Pokud máte problémy s přihlášením nebo admin funkcemi, zkontrolujte:
1. Logy aplikace
2. Database connection
3. Admin credentials v config

---

**Poznámka:** Tento admin panel je plně funkční a připraven k použití!
