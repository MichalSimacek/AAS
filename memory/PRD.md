# Aristocratic Artwork Sale - Product Requirements Document

## Original Problem Statement
ASP.NET Core web application for selling aristocratic artwork, antiques, jewelry, and watches. The application features multi-language support (10 languages), GDPR compliance, and professional presentation of luxury items.

## Technology Stack
- **Backend**: ASP.NET Core 8, Entity Framework Core, PostgreSQL
- **Frontend**: Razor Pages, Bootstrap 5, Vanilla JavaScript
- **Localization**: .resx resource files with IStringLocalizer
- **Security**: Content Security Policy, Subresource Integrity, GDPR consent

---

## Implemented Features (as of 2026-01-25)

### Session 2 - Latest Changes

#### 1. Collections Landing Page ✅ NEW
- Created at `/Collections/Landing` as Razor Page
- Displays 5 category cards: Paintings, Jewelry, Watches, Statues, Decorative Arts
- Each card has Bootstrap icon, title, item count or "Coming soon"
- Hover effects with "Explore →" call-to-action
- Hero section with dark gradient background
- Info section with AAS Verified badge explanation
- Navigation updated to link to Landing page

#### 2. Background Music Fix ✅ FIXED
- Fixed SRI hash for Swiper CDN (was causing JS loading failure)
- Music toggle button visible in bottom-left corner
- Tchaikovsky Swan Lake MP3 plays on click
- sessionStorage persists playback state across navigation
- Visual feedback: gold pulsing when playing, gray when muted
- Default state: muted (respects browser autoplay policies)

### Previous Session Changes (preserved)

#### Collection Category Filter ✅
- Removed "All" category from filter
- Renamed "Other" to "Decorative Arts"
- Translations in all 10 languages

#### HowTo Page Text Updates ✅
- Updated expert assessment text (seller's expenses)
- Updated handover text (both parties)
- Translations in all 10 languages

#### Price Field Type Change ✅
- Changed Price from decimal to string
- Database migration preserves existing data
- XSS sanitization implemented
- Supports text like "Price on request"

#### Typography & GDPR/Security ✅
- Inter font used consistently
- Consent-based loading for GA and Google Fonts
- SRI hashes on all CDN assets

---

## Database Schema (Key Tables)
- **Collections**: Id, Title, Slug, Category, Description, Price (string), Status, Currency, AASVerified
- **CollectionTranslation**: Id, CollectionId, LanguageCode, Title, Description
- **CollectionImages**: Id, CollectionId, FileName, Width, Height, Bytes, SortOrder

---

## File Structure
```
/app/src/AAS.Web/
├── Pages/
│   └── Collections/
│       └── Landing.cshtml          # NEW - Collections landing page
├── Controllers/
│   └── CollectionsController.cs    # Index, Details actions
├── Views/
│   ├── Collections/
│   │   ├── Index.cshtml            # Category grid view
│   │   └── Detail.cshtml           # Single item view
│   └── Shared/
│       └── _Layout.cshtml          # Music toggle, navigation
├── wwwroot/
│   └── audio/
│       └── tchaikovsky-swan-lake.mp3  # Background music
└── Resources/
    └── Resources.SharedResources.*.resx  # 10 language files
```

---

## Test Reports
- `/app/test_reports/iteration_1.json` - Previous session tests
- `/app/test_reports/iteration_2.json` - Current session tests (100% pass)

---

## Technical Notes
- App runs on port 8001 (ASP.NET Core, not supervisor-managed)
- PostgreSQL on localhost:5432 with user aas_user
- Environment: ASPNETCORE_ENVIRONMENT=Development
- Landing page uses Razor Pages (not MVC) to avoid routing conflicts

## Test Credentials
- Admin: admin@localhost / Admin123!@#$

---

## Backlog / Future Tasks
(No pending tasks from current requirements)

## Potential Enhancements
- Add actual collection items to categories
- Implement search functionality
- Add user favorites/wishlist
- Newsletter subscription for new arrivals
