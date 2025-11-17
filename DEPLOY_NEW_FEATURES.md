# Deployment Instructions for New Features

## 🚀 Implemented Features

1. **AAS Verified Badge** - Ikona autenticity na kolekcích
2. **User Comments** - Uživatelé mohou komentovat kolekce
3. **Blog System** - Kompletní blog s TinyMCE editorem a automatickým překladem
4. **"How To" Pages** - Průvodce jak koupit/prodat (již existoval, rozšířen)
5. **DeepL Integration** - Automatický překlad blog článků do všech jazyků

## 📋 Požadavky před deploymentem

✅ DeepL API klíč (již nastaven v `.env.production`)  
✅ Docker a Docker Compose nainstalován  
✅ Přístup k serveru přes SSH

## 🔧 Deployment Steps

### Krok 1: Zastavení stávajících služeb

```bash
cd /app
sudo docker compose -f docker-compose.prod.yml down
```

### Krok 2: Rebuild Docker Image

**KRITICKY DŮLEŽITÉ:** Protože jsme změnili kód aplikace, musíme přestavět Docker image:

```bash
sudo docker compose -f docker-compose.prod.yml build --no-cache web
```

⏱️ Toto může trvat 3-5 minut.

### Krok 3: Spuštění služeb

```bash
sudo docker compose -f docker-compose.prod.yml up -d
```

### Krok 4: Kontrola logů

Zkontrolujte, že aplikace běží správně a migrace proběhla:

```bash
# Sledujte logy aplikace
sudo docker logs -f aas-web-prod

# Měli byste vidět:
# - "Applying migration '20241116_AddCommentsAndBlog'..."
# - "Application started. Press Ctrl+C to shut down."
# - Žádné errory
```

Stiskněte `Ctrl+C` pro zastavení sledování logů.

### Krok 5: Kontrola stavu služeb

```bash
sudo docker ps
```

Měli byste vidět 3 běžící kontejnery:
- `aas-web-prod` (ASP.NET aplikace)
- `aas-db-prod` (PostgreSQL databáze)
- `aas-nginx-prod` (Nginx reverse proxy)

## ✅ Verifikace Features

### 1. Blog
```bash
# Přístup jako admin:
https://aristocraticartworksale.com/Admin/Blog

# Vytvoření nového článku:
1. Klikněte na "New Post"
2. Zadejte název a obsah (v češtině)
3. Volitelně nahrajte hlavní obrázek
4. Zaškrtněte "Published"
5. Klikněte "Create"
6. Počkejte ~ 30 sekund (DeepL překládá do všech jazyků)

# Veřejný blog:
https://aristocraticartworksale.com/Blog
```

### 2. Komentáře
```bash
# Přejděte na detail libovolné kolekce:
https://aristocraticartworksale.com/collections/<slug-kolekce>

# Měli byste vidět:
- Sekci "Komentáře" na konci stránky
- Formulář pro přidání komentáře (pokud jste přihlášeni)
- Seznam existujících komentářů
```

### 3. AAS Verified Badge
```bash
# V admin panelu:
1. Upravte libovolnou kolekci
2. Zaškrtněte checkbox "AAS Verified"
3. Uložte

# Na veřejné straně:
- Na seznamu kolekcí by měla být viditelná zelená ikona s fajfkou
- Na detailu kolekce by měl být viditelný badge s tooltipem
```

### 4. "How To" Page
```bash
# Přístup:
https://aristocraticartworksale.com/HowTo

# Měli byste vidět:
- Průvodce nákupem
- Průvodce prodejem
- Vysvětlení AAS Verified badge
```

## 🔍 Troubleshooting

### Problém: Aplikace se nespustí

```bash
# Zkontrolujte logy:
sudo docker logs aas-web-prod

# Pokud vidíte chybu s migrací:
sudo docker compose -f docker-compose.prod.yml down
sudo docker compose -f docker-compose.prod.yml up -d
```

### Problém: DeepL překlady nefungují

```bash
# Zkontrolujte, že API klíč je nastaven:
grep DEEPL_API_KEY /app/.env.production

# Mělo by vrátit:
# DEEPL_API_KEY=844c4481-fc11-4f31-994b-f769e0d80c79:fx

# Zkontrolujte logy při vytváření blog postu:
sudo docker logs -f aas-web-prod
```

### Problém: 404 při přístupu na /Blog

```bash
# Restartujte Nginx:
sudo docker compose -f docker-compose.prod.yml restart nginx
```

### Problém: Obrázky se nezobrazují

```bash
# Ujistěte se, že složka pro upload existuje:
sudo docker exec aas-web-prod ls -la /app/wwwroot/uploads/

# Pokud ne, vytvořte ji:
sudo docker exec aas-web-prod mkdir -p /app/wwwroot/uploads/blog
```

## 📊 Monitoring

### Sledování DeepL API Usage

DeepL Free tier limit: 500,000 znaků/měsíc

```bash
# Počet blog postů v databázi:
sudo docker exec -it aas-db-prod psql -U aas -d aas -c "SELECT COUNT(*) FROM \"BlogPosts\";"
```

### Sledování diskového prostoru

```bash
# Kontrola využití disku:
df -h /app

# Velikost upload složky:
sudo docker exec aas-web-prod du -sh /app/wwwroot/uploads/
```

### Databázové statistiky

```bash
# Připojení k databázi:
sudo docker exec -it aas-db-prod psql -U aas -d aas

# SQL queries:
# Počet kolekcí s AAS Verified:
SELECT COUNT(*) FROM "Collections" WHERE "AASVerified" = true;

# Počet komentářů:
SELECT COUNT(*) FROM "Comments";

# Počet publikovaných blog postů:
SELECT COUNT(*) FROM "BlogPosts" WHERE "Published" = true;

# Ukončení:
\q
```

## 🛡️ Security Notes

1. **DeepL API Key**: Je uložen v `.env.production` - nikdy nesdílejte tento soubor!
2. **Komentáře**: Uživatelé mohou komentovat pouze když jsou přihlášeni
3. **Blog Admin**: Pouze admin role může spravovat blog
4. **Antiforgery**: Všechny formuláře jsou chráněny proti CSRF útokům

## 🔄 Rollback (v případě problémů)

Pokud potřebujete vrátit změny:

```bash
# 1. Zastavte služby
sudo docker compose -f docker-compose.prod.yml down

# 2. Obnovte předchozí Docker image (pokud existuje backup)
# Nebo použijte git k návratu na předchozí commit

# 3. Rollback databázové migrace (pokud je potřeba)
# POZOR: Toto smaže nové tabulky!
sudo docker exec aas-web-prod dotnet ef migrations remove --project /app/src/AAS.Web

# 4. Restartujte služby
sudo docker compose -f docker-compose.prod.yml up -d
```

## 📞 Support

Pokud narazíte na problémy:

1. Zkontrolujte logy: `sudo docker logs -f aas-web-prod`
2. Zkontrolujte databázové připojení
3. Ověřte, že všechny porty jsou otevřené (80, 443)
4. Zkontrolujte, že SSL certifikáty jsou platné

## 📝 Co dělat dále

### První kroky po deployu:

1. **Vytvořte první blog post**
   - Přihlaste se jako admin
   - Přejděte na Admin → Manage Blog
   - Vytvořte uvítací příspěvek

2. **Označte vybrané kolekce jako AAS Verified**
   - Upravte kolekce v admin panelu
   - Zaškrtněte AAS Verified u prověřených předmětů

3. **Otestujte komentáře**
   - Zaregistrujte testovacího uživatele
   - Přidejte komentář na kolekci
   - Ověřte, že můžete editovat a mazat vlastní komentáře

4. **Propagujte nové funkce**
   - Informujte uživatele o novém blogu
   - Zvýrazněte AAS Verified badge na kolekcích
   - Povzbuďte uživatele k zanechání komentářů

---

✅ **Deployment Complete!** Všechny nové funkce by měly být nyní aktivní.
