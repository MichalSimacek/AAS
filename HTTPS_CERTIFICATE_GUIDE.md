# 🔒 HTTPS Development Certificate Guide

## ❓ Proč vidím security varování?

Když poprvé spustíte aplikaci stisknutím F5, uvidíte **2 bezpečnostní varování**. To je **normální a očekávané** pro ASP.NET Core lokální vývoj.

## 📋 Varování která uvidíte

### Varování 1: VS Code
```
Security Warning
The selected launch configuration is configured to launch a web browser
but no trusted development certificate was found.
Create a trusted self-signed certificate?

[Yes] [More Information] [Cancel]
```

**Odpověď: [Yes]**

### Varování 2: Windows Security
```
Rozhodli jste se nainstalovat certifikát z certifikační autority (CA),
která se prezentuje jako: localhost

Upozornění:
Jestliže nainstalujete tento kořenový certifikát, bude systém Windows
automaticky důvěřovat všem certifikátům vydaným touto certifikační autoritou.

Chcete tento certifikát nainstalovat?

[Ano] [Ne]
```

**Odpověď: [Ano]**

---

## ✅ Je to bezpečné?

**ANO, je to naprosto bezpečné!** Zde je proč:

### Co se děje?
1. ASP.NET Core používá **HTTPS** i pro lokální vývoj (port 5001)
2. Pro HTTPS je potřeba **SSL/TLS certifikát**
3. .NET SDK vytvoří **development certifikát** pouze pro váš počítač
4. Tento certifikát funguje pouze pro `localhost` a `127.0.0.1`
5. Certifikát je platný pouze na **vašem počítači**

### Proč je to bezpečné?
- ✅ Certifikát je **self-signed** (sami sobě důvěřujeme)
- ✅ Platí pouze pro `localhost` (ne pro internet)
- ✅ Je uložený pouze na vašem PC
- ✅ Nemůže být zneužit k útoku na jiné servery
- ✅ Je to **standardní praxe** pro .NET development
- ✅ Můžete ho kdykoliv smazat

---

## 🛠️ Manuální správa certifikátu

### Vytvořit a důvěřovat certifikátu
```bash
dotnet dev-certs https --trust
```

### Ověřit, že certifikát existuje
```bash
dotnet dev-certs https --check
```

Výstup:
```
A valid HTTPS certificate is already present.
```

### Smazat certifikát (pokud chcete začít znovu)
```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

### Zobrazit informace o certifikátu
```bash
dotnet dev-certs https --check --verbose
```

---

## 🔍 Co dělá certifikát?

### Bez certifikátu (HTTP):
```
http://localhost:5000  ✅ Funguje
https://localhost:5001 ❌ Browser error: "Your connection is not private"
```

### S certifikátem (HTTPS):
```
http://localhost:5000  ✅ Funguje (redirect na HTTPS)
https://localhost:5001 ✅ Funguje s HTTPS
```

---

## 🎯 Kdy certifikát potřebujete?

### Potřebujete:
- ✅ Při lokálním vývoji ASP.NET Core aplikace
- ✅ Při testování HTTPS funkcí (cookies, secure headers)
- ✅ Při F5 debugging ve VS Code nebo Visual Studio
- ✅ Při testování API endpointů s HTTPS

### Nepotřebujete:
- ❌ V produkci (používá se skutečný certifikát, např. Let's Encrypt)
- ❌ V Dockeru (pokud neděláte HTTPS debugging)
- ❌ Při buildu aplikace (jen při spuštění)

---

## 🔧 Troubleshooting

### "Certificate is not trusted" i po instalaci

**Řešení 1: Restart browseru**
```bash
# Zavřete všechny instance browseru a otevřete znovu
```

**Řešení 2: Reinstalace certifikátu**
```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

**Řešení 3: Restart VS Code**
```bash
# Zavřete VS Code a otevřete znovu
code .
```

### Windows zrušil instalaci certifikátu

Pokud jste klikli **[Ne]** na Windows varování:

```bash
# Spusťte znovu
dotnet dev-certs https --trust

# Tentokrát klikněte [Ano]
```

### Browser stále zobrazuje "Not Secure"

**Edge/Chrome:**
1. Otevřete `chrome://settings/security`
2. Zkontrolujte "Manage certificates"
3. V sekci "Trusted Root Certification Authorities" by měl být "localhost"

**Firefox:**
Firefox používá vlastní certificate store. Buď:
1. Přijměte výjimku v browseru
2. Nebo importujte certifikát ručně do Firefoxu

### Chci certifikát smazat

```bash
# Smazání dev certifikátu
dotnet dev-certs https --clean
```

Windows může zobrazit varování - klikněte **[Ano]** pro potvrzení odstranění.

---

## 📚 Dodatečné informace

### Kde je certifikát uložen?

**Windows:**
```
CurrentUser\My (Personal certificate store)
CurrentUser\Root (Trusted Root Certification Authorities)
```

Můžete ho zobrazit přes:
1. Win + R → `certmgr.msc`
2. Personal → Certificates → najděte "localhost"

### Kryptografický otisk

Každý certifikát má unikátní SHA1 kryptografický otisk, např.:
```
656D2EA3 A3861BC7 F2F16299 FE8CBA1A 48637860
```

Tento otisk je **unikátní pro váš počítač** a každý vývojář má jiný.

### Platnost certifikátu

Development certifikáty jsou platné **1 rok** od vytvoření.

Po roce můžete vytvořit nový:
```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

---

## ⚠️ Bezpečnostní poznámky

### ✅ BEZPEČNÉ pro development:
- ✅ Použití dev certifikátu na localhost
- ✅ Důvěryhodnost certifikátu na vývojovém PC
- ✅ Testování HTTPS lokálně

### ❌ NEBEZPEČNÉ - NIKDY NEDĚLEJTE:
- ❌ Použití dev certifikátu v produkci
- ❌ Sdílení dev certifikátu s ostatními
- ❌ Export private key dev certifikátu
- ❌ Důvěryhodnost neznámým certifikátům
- ❌ Instalace certifikátů z nedůvěryhodných zdrojů

---

## 🎓 Pro produkci

V produkčním prostředí použijte **skutečný SSL/TLS certifikát**:

### Možnosti:
1. **Let's Encrypt** - Zdarma, auto-renew
2. **Cloudflare** - Zdarma pro základní použití
3. **Komerční CA** - DigiCert, GlobalSign, atd.

V tomto projektu je production certifikát konfigurován v:
- `DEPLOYMENT.md` - Nginx + Let's Encrypt
- `docker-compose.yml` - Production setup

---

## 📖 Oficiální dokumentace

Microsoft dokumentace:
- [Enforce HTTPS in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl)
- [Trust the ASP.NET Core HTTPS development certificate](https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl#trust-the-aspnet-core-https-development-certificate-on-windows-and-macos)

---

## ✅ Shrnutí

**Když vidíte security varování:**
1. VS Code varování → **[Yes]**
2. Windows varování → **[Ano]**
3. To je **normální** a **bezpečné** pro lokální vývoj
4. Certifikát funguje pouze na vašem PC
5. Je to **standardní praxe** pro ASP.NET Core

**Pokud jste klikli Ne/Cancel:**
```bash
dotnet dev-certs https --trust
```

**Certifikát je automaticky součástí `dev-setup.ps1`!**

---

*Poslední aktualizace: 2025-11-05*
*Pro další pomoc viz: QUICK_START.md, DEVELOPMENT.md*
