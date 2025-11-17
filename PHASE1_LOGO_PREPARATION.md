# Phase 1 - Logo Preparation Instructions

## AAS Verified Badge Logo

### Source Logo
Použijte existující logo z: `/app/src/AAS.Web/wwwroot/images/logo.png`

### Požadavky na badge logo:
1. **Rozměry:** 50x50 pixels
2. **Formát:** PNG s průhledným pozadím
3. **Design:** Identické logo jako na landing page, pouze zmenšené
4. **Umístění:** `/app/src/AAS.Web/wwwroot/images/aas-verified-badge.png`

### Jak vytvořit:
```bash
# Pokud máte ImageMagick na serveru:
convert /app/src/AAS.Web/wwwroot/images/logo.png -resize 50x50 -background none /app/src/AAS.Web/wwwroot/images/aas-verified-badge.png

# NEBO použijte online nástroj:
# 1. Stáhněte logo.png
# 2. Otevřete https://www.iloveimg.com/resize-image
# 3. Resize na 50x50px
# 4. Uložte jako aas-verified-badge.png
# 5. Nahrajte na server do /app/src/AAS.Web/wwwroot/images/
```

### Alternativa - SVG (doporučeno pro lepší kvalitu):
Pokud máte logo ve SVG formátu, můžete použít SVG přímo pro lepší zobrazení na high-DPI displejích.

Soubor by měl být: `/app/src/AAS.Web/wwwroot/images/aas-verified-badge.svg`

## Po přípravě loga pokračujte s deploymentem Phase 1
