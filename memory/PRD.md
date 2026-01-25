# Aristocratic Artwork Sale - Product Requirements Document

## Original Problem Statement
ASP.NET Core web application for selling aristocratic artwork, antiques, jewelry, and watches. The application features multi-language support (10 languages), GDPR compliance, and professional presentation of luxury items.

## User Personas
- **Buyers**: Collectors and investors seeking authenticated luxury items
- **Sellers**: Private owners looking to sell valuable artwork and collectibles
- **Admin**: Site administrators managing collections, blog posts, and inquiries

## Core Requirements
- Multi-language support (EN, CS, DE, ES, FR, HI, JA, PT, RU, ZH)
- GDPR-compliant cookie consent and data handling
- Professional gallery presentation with lightbox
- AAS Verified authentication badge system
- Contact/inquiry system
- Admin panel for content management
- Blog system with translations

## Technology Stack
- **Backend**: ASP.NET Core 8, Entity Framework Core, PostgreSQL
- **Frontend**: Razor Pages, Bootstrap 5, Vanilla JavaScript
- **Localization**: .resx resource files with IStringLocalizer
- **Security**: Content Security Policy, Subresource Integrity, GDPR consent

---

## Implemented Features (as of 2026-01-25)

### Session Work Completed

#### 1. Collection Category Filter Update ✅
- Removed "All" category from collections filter
- Renamed "Other" category to "Decorative Arts"
- Added translations to all 10 languages:
  - EN: "Decorative Arts"
  - CS: "Dekorativní umění"
  - DE: "Dekorative Kunst"
  - ES: "Artes Decorativas"
  - FR: "Arts Décoratifs"
  - HI: "सजावटी कला"
  - JA: "装飾芸術"
  - PT: "Artes Decorativas"
  - RU: "Декоративное искусство"
  - ZH: "装饰艺术"

#### 2. HowTo Page Text Updates ✅
- Updated HowToSellStep2Text: "In case of interest, our team of experts will conduct a thorough assessment of your item and provide you with a highest level international laboratory expertise at seller's expenses."
- Updated HowToSellStep3Text: "If you agree to the offer, we will agree on a convenient and safe way to hand over the items for both parties."
- Translations updated in all 10 languages

#### 3. Background Music Implementation ✅
- Added Tchaikovsky Swan Lake MP3 file
- Created music toggle button (bottom-left corner)
- Implemented persistent playback across page navigation using sessionStorage
- Music starts muted by default (GDPR/UX friendly)
- Visual feedback: gold pulsing animation when playing, gray when muted

#### 4. Price Field Type Change ✅
- Changed Price from decimal to string in Collection model
- Created database migration (PriceToString) preserving existing data
- Updated Admin Create/Edit forms with text input
- Added placeholder "e.g., 1500 or Price on request"
- Implemented XSS sanitization using HtmlEncode
- Updated all views to handle both numeric and text prices

#### 5. Typography & Font Review ✅
- Confirmed Inter font usage throughout site
- CSS variables system in place for consistent styling
- Font loaded conditionally after GDPR consent

### Previously Completed Features
- Collection image gallery with lightbox and zoom
- GDPR-compliant Google Analytics and Fonts loading
- Subresource Integrity for all CDN assets
- Cookie consent banner with settings modal
- Localization system fix (Resources.SharedResources naming)
- Social media share icons (SRI hash corrected)
- Site-wide typography unification

---

## Database Schema (Key Tables)
- **Collections**: Id, Title, Slug, Category, Description, Price (string), Status, Currency, AASVerified
- **CollectionTranslation**: Id, CollectionId, Language, Title, Description
- **CollectionImages**: Id, CollectionId, FileName, Width, Height, Bytes, SortOrder
- **BlogPosts**: Id, TitleCs, ContentCs, TitleEn, ContentEn, ...

---

## Backlog / Future Tasks
1. ~~Collection category filter update~~ ✅
2. ~~HowTo page text updates~~ ✅
3. ~~Background music implementation~~ ✅
4. ~~Price field type change~~ ✅
5. ~~Final font/color review~~ ✅

No pending tasks from this session.

---

## Technical Notes
- App runs on port 8001 (ASP.NET Core, not supervisor-managed)
- PostgreSQL on localhost:5432 with user aas_user
- Resource files must have both naming conventions for localization to work:
  - SharedResources.xx.resx
  - Resources.SharedResources.xx.resx
- Environment: ASPNETCORE_ENVIRONMENT=Development

## Test Credentials
- Admin: admin@localhost / Admin123!@#$
