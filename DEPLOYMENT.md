# Deployment Instructions

## Přestavění a nasazení

```bash
cd /app

# 1. Rebuild Docker image (KRITICKÉ!)
sudo docker compose -f docker-compose.prod.yml build --no-cache web

# 2. Restart služeb
sudo docker compose -f docker-compose.prod.yml down
sudo docker compose -f docker-compose.prod.yml up -d

# 3. Kontrola logů
sudo docker logs -f aas-web-prod

# Počkejte na zprávu: "Application started. Press Ctrl+C to shut down."
```

## Implementované funkce

1. **AAS Verified Badge** - Ikona autenticity na kolekcích
2. **Systém komentářů** - Uživatelé mohou komentovat kolekce
3. **Blog** - Admin může vytvářet články s automatickým překladem (DeepL)
4. **"Jak na to"** - Průvodce nákupem/prodejem (už existoval)
5. **DeepL integrace** - Automatický překlad do 10 jazyků

## První kroky po nasazení

1. Přihlaste se jako admin
2. Navštivte `/Admin/Blog` a vytvořte první příspěvek
3. Označte vybrané kolekce jako "AAS Verified" v admin editaci
