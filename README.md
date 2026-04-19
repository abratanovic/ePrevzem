# 📦 ePrevzem – Sistem za varen prevzem dokumentov

## 🧾 Opis projekta

Sistem ePrevzem je zasnovan kot **generična (multi-tenant) platforma**, ki omogoča različnim organizacijam uporabo pametnih paketnikov za varen prevzem dokumentov ali drugih občutljivih predmetov.

Razvit je v okviru projekta _Pametni paketnik_ na Fakulteti za elektrotehniko, računalništvo in informatiko Univerze v Mariboru, v okviru študijskega programa Računalništvo in informacijske tehnologije (VS).

Sistem omogoča organizacijam (npr. upravne enote, univerze, podjetja), da pripravijo občutljive dokumente za prevzem v paketniku, pri čemer lahko dokument prevzame samo ustrezno identificiran uporabnik. To zagotavlja hitrejši in bolj fleksibilen prevzem dokumentov, saj uporabniku ni potrebno čakati na dostavo po pošti ali v čakalnih vrstah na upravni enoti.

----------

## ✅ Prednosti rešitve  
  
Sistem ePrevzem prinaša več ključnih prednosti:  
  
- ⏱️ **Brez čakanja na dostavo**  
Uporabniku ni potrebno čakati na dostavo dokumentov po pošti.  
  
- 🕒 **Brez čakalnih vrst**  
Prevzem dokumentov poteka brez čakanja v vrsti na upravni enoti ali drugi organizaciji.  
  
- 📍 **Fleksibilen prevzem**  
Dokument lahko uporabnik prevzame kadarkoli v času veljavnosti.  
  
- 🔐 **Povečana varnost**  
Dostop je omogočen samo po uspešni identifikaciji.  
  
- 📊 **Popolna sledljivost**  
Vsi dogodki so zabeleženi v sistemu.

----------

## 🎯 Glavne funkcionalnosti

### Varen proces prevzema

-   Obvestilo uporabniku, ko je dokument pripravljen
    
-   Varna identifikacija pred prevzemom (simulacija SI-TRUST / eOsebna)
    
-   Odklep ustreznega predalčka preko mobilne aplikacije
    
-   Beleženje vseh odklepov in dogodkov (audit log)
    

### Funkcionalnosti za uporabnika

-   Pregled aktivnih prevzemov
    
-   Prikaz roka za prevzem in lokacije paketnika
    
-   Začetek postopka identifikacije
    
-   Odklep predalčka
    
-   Delegacija prevzema drugi osebi (elektronsko pooblastilo)
    
-   Pregled zgodovine prevzemov
    

### Funkcionalnosti za organizacijo (portal)

-   Upravljanje uporabnikov in organizacij
    
-   Kreiranje zahtevkov za prevzem dokumentov
    
-   Dodeljevanje dokumentov paketnikom in predalčkom
    
-   Spremljanje statusa prevzema
    
-   Pregled dnevnika odklepov in dogodkov
    

### Napredna varnost

-   Dogodkovno video snemanje ob:
    
    -   zaznavi gibanja pred paketnikom
        
    -   odklepu predalčka
        
-   Povezava video dogodkov z evidenco odklepov
    
-   Kratkotrajna hramba posnetkov (zaradi varstva podatkov)
    

----------

## 🧠 Koncept sistema

Sistem ePrevzem temelji na centraliziranem zalednem sistemu, ki povezuje organizacije, uporabnike in pametne paketnike v enoten proces varnega prevzema.

Možni scenariji uporabe:

-   upravne enote (osebni dokumenti)
    
-   univerze (diplome)
    
-   podjetja (pogodbe, oprema)
    
-   banke (kartice)
    

----------

## 🏗️ Arhitektura

### Backend

-   REST API
    
-   poslovna logika za upravljanje prevzemov
    
-   modul za identifikacijo uporabnika
    
-   abstrakcija za komunikacijo s paketnikom
    
-   beleženje dogodkov (audit log)

-   zasnova po principih ločitve odgovornosti (Clean Architecture)
    

### Frontend

-   uporabniška mobilna aplikacija
    
-   administratorski portal za organizacije
    

### Integracija paketnikov

Sistem omogoča komunikacijo s pametnimi paketniki preko namenskega integracijskega sloja.  
  
V okviru projekta je implementirana **dejanska komunikacija s paketnikom**, ki omogoča:  
- odklep posameznega predalčka na zahtevo sistema,  
- povezavo med logiko prevzema in fizično napravo,  
- beleženje uspešnosti odklepa.  
  
Integracija temelji na uporabi paketnikov podjetja Direct4.me, pri čemer je komunikacija prilagojena razpoložljivim vmesnikom naprave.  
  
Arhitektura sistema omogoča tudi razširitev na druge tipe paketnikov z zamenjavo implementacije integracijskega sloja.

----------

## 🔄 Osnovni potek uporabe

1.  Organizacija ustvari zahtevek za prevzem dokumenta
    
2.  Dokument se shrani v predalček paketnika
    
3.  Uporabnik prejme obvestilo
    
4.  Uporabnik pride do paketnika
    
5.  V aplikaciji se varno identificira
    
6.  Sistem preveri pravice dostopa
    
7.  Predalček se odklene
    
8.  Dogodek se zabeleži in posname
    

----------

## 🔐 Identifikacija uporabnika

Sistem uporablja simulacijo državnih storitev za identifikacijo:

-   Mock **SI-TRUST / SI-PASS**
    
-   Mock **eOsebna (NFC / biometrija)**
    
Identifikacijski modul je zasnovan tako, da omogoča kasnejšo integracijo z realnimi identitetnimi sistemi brez večjih sprememb v arhitekturi.

----------

## 🛠️ Tehnologije

### Backend

-   .NET (ASP.NET Core Web API)
    
-   Entity Framework Core
    
-   PostgreSQL podatkovna baza
    

### Frontend

-   Flutter (mobilna uporabniška aplikacija za Android in iOS)
- React (spletna aplikacija - administracijska platforma za organizacije) 

----------

## 🚀 Cilji projekta

-   omogočiti varen prevzem občutljivih dokumentov
    
-   zmanjšati potrebo po fizičnih obiskih in čakanju
    
-   zagotoviti sledljivost vseh dejanj
    
-   omogočiti razširljiv sistem za več organizacij
    
----------

## Ekipa

- Adnan Bratanović
- Edvin Bečić
- Emir Ribić

----------

## Opombe

-   Projekt je prototip za izobraževalne namene
    
-   Nekateri deli sistema (identifikacija, paketnik) so simulirani
    
-   Fokus je na arhitekturi, varnosti in uporabniški izkušnji
    

----------

## Vsebina repozitorija

- backend (REST API)  

- frontend (React admin portal)  

- mobilna aplikacija (Flutter)

----------
