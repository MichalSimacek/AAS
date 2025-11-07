# Nastavení automatického překladu kolekcí

## Problém
Názvy a popisy kolekcí jsou v databázi uloženy v angličtině. Pro automatický překlad do ostatních jazyků je potřeba nastavit překladač.

## Řešení

### Možnost 1: LibreTranslate (Zdarma, Self-hosted nebo Online)

1. **Online služba** (nejjednodušší):
   - Použijte veřejnou instanci: `https://libretranslate.com/translate`
   - NEBO použijte placený API klíč z https://libretranslate.com/

2. **Nastavení v `appsettings.json`**:
   ```json
   "Translation": {
     "Provider": "LibreTranslate",
     "Endpoint": "https://libretranslate.com/translate",
     "ApiKey": "",  // Ponechte prázdné pro veřejnou službu
     "Enabled": true  // ZMĚŇTE z false na true!
   }
   ```

3. **Restartujte aplikaci**

### Možnost 2: Google Translate API (Placené)

1. Vytvořte API klíč na: https://console.cloud.google.com/
2. Povolte Google Cloud Translation API
3. **Nastavení v `appsettings.json`**:
   ```json
   "Translation": {
     "Provider": "Google",
     "Endpoint": "https://translation.googleapis.com/language/translate/v2",
     "ApiKey": "VÁŠ_GOOGLE_API_KLÍČ",
     "Enabled": true
   }
   ```

### Možnost 3: Vypnout překlad (pouze základní překlady z .resx souborů)

Ponechte `"Enabled": false` v appsettings.json.

## Co je přeloženo automaticky?

✅ **S překladem zapnutým** (`Enabled: true`):
- Názvy kolekcí
- Popisy kolekcí
- Kategorie
- Všechny statické texty (menu, tlačítka, formuláře)

✅ **Bez překladu** (`Enabled: false`):
- Kategorie
- Všechny statické texty (menu, tlačítka, formuláře)
- ❌ Názvy a popisy kolekcí NEJSOU přeloženy (zůstanou v angličtině)

## Cache

Překlady jsou ukládány do databáze (tabulka `TranslationCaches`), takže stejný text se překládá pouze jednou. To výrazně zrychluje načítání stránek.

## Testování

1. Změňte jazyk na webu (např. na ruštinu)
2. Přejděte na stránku kolekce
3. Zkontrolujte, zda jsou název a popis přeloženy

## Poznámka

Pro nejlepší výsledky doporučujeme použít LibreTranslate API s API klíčem nebo Google Translate API. Veřejná instance LibreTranslate může být pomalá při vysokém zatížení.
