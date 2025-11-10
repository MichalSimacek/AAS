# 🖥️ Požadavky na server pro AAS

## 📊 Přehled konfigurací

### 🟡 Minimální (testování, velmi malý provoz)
**Použití:** Testování, demo, <50 návštěvníků/den

| Parametr | Hodnota |
|----------|---------|
| **CPU** | 1 vCore |
| **RAM** | 2 GB |
| **Disk** | 20 GB SSD |
| **Bandwidth** | 1 TB/měsíc |
| **Cena** | $5-10/měsíc |

**Výkon:**
- ✅ Základní funkce fungují
- ⚠️ Pomalé nahrávání obrázků
- ⚠️ Překlad může být pomalý
- ❌ Nepůjde škálovat

### 🟢 Doporučená (produkce, střední provoz)
**Použití:** Produkční web, 50-500 návštěvníků/den

| Parametr | Hodnota |
|----------|---------|
| **CPU** | 2 vCores |
| **RAM** | 4 GB |
| **Disk** | 50 GB SSD |
| **Bandwidth** | 2 TB/měsíc |
| **Cena** | $15-25/měsíc |

**Výkon:**
- ✅ Plynulý chod aplikace
- ✅ Rychlé nahrávání obrázků
- ✅ Překlad funguje dobře
- ✅ Prostor pro růst
- ✅ Můžete mít 100-200 kolekcí

### 🔵 Optimální (vysoký provoz)
**Použití:** Velký web, 500-2000 návštěvníků/den

| Parametr | Hodnota |
|----------|---------|
| **CPU** | 4 vCores |
| **RAM** | 8 GB |
| **Disk** | 100 GB SSD |
| **Bandwidth** | 5 TB/měsíc |
| **Cena** | $40-60/měsíc |

**Výkon:**
- ✅ Excelentní výkon
- ✅ Zvládne velkou zátěž
- ✅ Rychlý překlad
- ✅ Můžete mít 500+ kolekcí

---

## 💰 Doporučení poskytovatelé VPS

### 1. DigitalOcean (nejpopulárnější)
**Doporučeno: Droplet 4GB RAM**
- **Cena:** $24/měsíc
- **Parametry:** 2 vCPU, 4GB RAM, 80GB SSD, 4TB transfer
- **Výhody:** 
  - Snadná administrace
  - Výborná dokumentace
  - 1-click Docker install
  - Free backups snapshot
- **URL:** https://www.digitalocean.com/pricing/droplets

### 2. Hetzner (nejlevnější, EU)
**Doporučeno: CX21**
- **Cena:** €5.83/měsíc (~$6.50)
- **Parametry:** 2 vCPU, 4GB RAM, 40GB SSD, 20TB transfer
- **Výhody:**
  - Excelentní cena/výkon
  - Datacentra v EU (GDPR friendly)
  - Velmi rychlé SSD
- **Nevýhody:** 
  - Méně známý mimo EU
- **URL:** https://www.hetzner.com/cloud

### 3. Vultr
**Doporučeno: High Frequency 2GB**
- **Cena:** $12/měsíc
- **Parametry:** 1 vCPU, 2GB RAM, 55GB SSD, 2TB transfer
- **Výhody:**
  - Rychlé NVMe SSD
  - Globální datacentra
  - Hodinové účtování
- **URL:** https://www.vultr.com/pricing/

### 4. Linode (Akamai)
**Doporučeno: Linode 4GB**
- **Cena:** $24/měsíc
- **Parametry:** 2 vCPU, 4GB RAM, 80GB SSD, 4TB transfer
- **Výhody:**
  - Stabilní dlouholetá firma
  - Dobrá podpora
  - Backup systém
- **URL:** https://www.linode.com/pricing/

### 5. Contabo (nejlevnější velké servery)
**Doporučeno: Cloud VPS 2**
- **Cena:** €6.99/měsíc (~$7.50)
- **Parametry:** 4 vCores, 6GB RAM, 100GB SSD, 32TB transfer
- **Výhody:**
  - Nejlepší cena/výkon
  - Hodně zdrojů za málo peněz
- **Nevýhody:**
  - Horší podpora
  - Pomalejší síť než konkurence
- **URL:** https://contabo.com/en/vps/

---

## 🎯 Mé doporučení

### Pro začátek (prvních 6 měsíců):
**Hetzner CX21** - €5.83/měsíc
- Skvělý poměr cena/výkon
- 4GB RAM dostatečná
- EU lokace (rychlé pro ČR)
- Můžete upgradovat kdykoliv

### Pro dlouhodobý provoz:
**DigitalOcean 4GB Droplet** - $24/měsíc
- Stabilní a spolehlivé
- Skvělá dokumentace
- Jednoduché škálování
- Automatické backupy

### Pokud máte malý budget:
**Vultr High Frequency 2GB** - $12/měsíc
- Stačí pro začátek
- Rychlé SSD
- Můžete upgradovat

---

## 📈 Očekávaný růst disku

**Obrázky** (hlavní spotřeba):
- Průměrná kolekce: 5-10 obrázků
- Průměrná velikost obrázku: 2-5 MB
- 100 kolekcí × 7 obrázků × 3 MB = ~2.1 GB

**Databáze:**
- 100 kolekcí: ~50 MB
- 1000 kolekcí: ~500 MB

**Zálohy** (denní):
- 7 denních záloh × 2 GB = ~14 GB

**Celkem pro 100 kolekcí:** ~20-25 GB  
**Doporučený disk:** 50-80 GB

---

## 🔧 Optimalizace výkonu

### Pokud máte jen 2GB RAM:

1. **Limitujte Docker kontejnery:**
```yaml
# docker-compose.prod.yml
services:
  web:
    deploy:
      resources:
        limits:
          memory: 1G
  db:
    deploy:
      resources:
        limits:
          memory: 512M
```

2. **Optimalizujte PostgreSQL:**
```bash
# V .env.production
DATABASE_URL=Host=db;Port=5432;Database=aas_production;Username=aasuser;Password=xxx;Maximum Pool Size=50;
```

3. **Vypněte překlad (pokud nepotřebujete):**
```bash
TRANSLATION_ENABLED=false
```

4. **Povolte swap:**
```bash
sudo fallocate -l 2G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

---

## 💾 Optimalizace disku

### Pokud máte jen 20GB disk:

1. **Automatické čištění Docker images:**
```bash
# Přidejte do crontabu
0 4 * * 0 docker system prune -af --volumes
```

2. **Komprimujte staré logy:**
```bash
# Přidejte do crontabu
0 3 * * * find /opt/aas/logs -name "*.log" -mtime +7 -exec gzip {} \;
```

3. **Omezte počet záloh:**
```bash
# V backup.sh změňte z 7 na 3
ls -t $BACKUP_DIR/aas_backup_*.tar.gz | tail -n +4 | xargs -r rm
```

4. **Optimalizujte velikost obrázků:**
Přidejte do `ImageService.cs` resize na max 2000px šířku.

---

## 🌍 Latence podle lokace

**Server v EU (Frankfurt/Amsterdam):**
- ČR: 10-20ms ⭐
- EU: 20-50ms
- USA: 100-150ms
- Asie: 200-300ms

**Server v USA (New York):**
- ČR: 100-120ms
- EU: 80-120ms
- USA: 10-30ms ⭐
- Asie: 150-250ms

**Doporučení:** Pro české publikum volte EU server (Hetzner/DigitalOcean Frankfurt).

---

## 🎛️ Monitoring spotřeby

### Kontrola RAM:
```bash
free -h
docker stats
```

### Kontrola disku:
```bash
df -h
du -sh /opt/aas/wwwroot/uploads
```

### Kontrola CPU:
```bash
top
htop
```

---

## ⚠️ Kdy upgradovat?

**Upgradn RAM pokud:**
- Aplikace crashuje s OOM (Out of Memory)
- `free -h` ukazuje <500MB free často
- Swap je používán >50% času

**Upgradněte CPU pokud:**
- Load average >2 (na 2 core serveru)
- Překlad trvá >10 sekund
- Stránky se načítají >3 sekundy

**Upgradněte disk pokud:**
- Volné místo <10GB
- Zálohy selhávají kvůli místu
- Nemůžete nahrát obrázky

---

## 📞 Závěr

### 🏆 Nejlepší volba pro váš projekt:

**Hetzner CX21** (€5.83/měsíc) nebo **DigitalOcean 4GB** ($24/měsíc)

**Proč:**
- ✅ 4GB RAM stačí na plynulý běh
- ✅ 2 CPU cores zvládnou zátěž
- ✅ 40-80GB disku pro stovky kolekcí
- ✅ Snadné upgrade když potřebujete
- ✅ Skvělý poměr cena/výkon

**Začněte s Hetzner, pokud máte malý budget.**  
**Přejděte na DigitalOcean, když chcete stabilitu a podporu.**

---

**Poznámka:** Všechny ceny jsou orientační k listopadu 2024.
