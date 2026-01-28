# Aristocratic Artwork Sale - Product Requirements Document

## Original Problem Statement
ASP.NET Core web application for selling aristocratic artwork, antiques, jewelry, and watches. The application features multi-language support (10 languages), GDPR compliance, and professional presentation of luxury items.

## Design Language (Updated 2026-01-28)
**Theme**: Elegant Dark with Purple & Gold - "Aristocratic Nonchalance"

### Color Palette
- Background Main: `#050505`
- Background Surface: `#0d0a10`
- Background Elevated: `#151019`
- Primary Purple: `#4B0082`
- Primary Deep: `#240046`
- Primary Light: `#7B2CBF`
- Gold: `#D4AF37`
- Gold Dim: `#8a702a`
- Gold Light: `#F5E6C4`
- Text Main: `#e8e8e8`
- Text Muted: `#9ca3af`

### Typography
- Headings: Playfair Display (serif)
- Accents: Cinzel (uppercase, letter-spaced)
- Body: Manrope (sans-serif)

### Design Principles
- Nonchalant elegance
- Subtle sophistication
- Restrained luxury
- Dark theme with purple gradients
- Gold accents for CTAs and highlights
- Scroll-reveal animations for luxurious feel

---

## Technology Stack
- **Backend**: ASP.NET Core 8, Entity Framework Core, PostgreSQL
- **Frontend**: Razor Pages, Bootstrap 5, Vanilla JavaScript
- **Localization**: .resx resource files with IStringLocalizer
- **Security**: Content Security Policy, Subresource Integrity, GDPR consent

---

## Implemented Features

### Complete Visual Redesign (2026-01-25) ✅
- Dark theme with purple gradient backgrounds
- Gold accents for buttons, icons, hover states
- Elegant serif typography for headings
- Category cards with purple gradient and gold icons
- Footer with 3-column layout and social icons
- Consistent dark theme across ALL pages
- Glass-morphism effects on cards
- Scroll-reveal animations using Intersection Observer

### Bug Fixes (2026-01-28) ✅
- **Mobile Image Gallery Fixed**: Enhanced touch support for thumbnail selection on mobile devices
- **Admin Dashboard Images Fixed**: Collection thumbnails now display correctly in the admin panel
- **Category Localization Fixed**: "GoldSilver" and "Other" categories now use correct localization keys

### Privacy Policy Page Redesign (2026-01-28) ✅
- Complete redesign to match dark aristocratic theme
- Purple gradient hero section with GDPR badge
- Numbered section cards with gold accents
- Cookie category cards with icons
- GDPR rights grid with hover effects
- Contact information cards
- Responsive design for mobile

### Previous Features (preserved)
- Collections Landing page with 5 category cards
- Multi-language support (10 languages)
- GDPR-compliant cookie consent banner (redesigned)
- Collection category filter
- Price field accepting text values
- Admin edit button on collection detail pages

---

## File Structure
```
/app/src/AAS.Web/
├── wwwroot/css/
│   └── site.css              # Complete redesign with dark theme + animations
├── Pages/Collections/
│   └── Landing.cshtml        # Category landing with dynamic previews
├── Views/
│   ├── Home/
│   │   ├── Index.cshtml      # Redesigned home page
│   │   └── Privacy.cshtml    # REDESIGNED 2026-01-28
│   ├── Collections/
│   │   ├── Index.cshtml      # Collection grid (localization fixed)
│   │   └── Detail.cshtml     # Mobile gallery FIXED
│   └── Shared/_Layout.cshtml # Navigation, footer, cookie banner
├── Areas/Admin/
│   ├── Controllers/CollectionsController.cs  # Image loading FIXED
│   └── Views/Collections/Index.cshtml        # Image display FIXED
└── Resources/                # 10 language files
/app/docs/
└── admin-account-change-guide.md  # NEW: Guide for changing admin credentials
```

---

## Test Credentials
- Admin: admin@localhost / Admin123!@#$

---

## Completed Tasks (This Session)
1. ✅ Installed .NET SDK 8 and PostgreSQL (environment reset)
2. ✅ Fixed mobile image gallery touch support
3. ✅ Fixed admin dashboard image thumbnails (Include first image in query)
4. ✅ Fixed category localization keys (GoldSilver, Other)
5. ✅ Redesigned Privacy Policy page with dark theme
6. ✅ Created admin account change guide

---

## Backlog / Future Enhancements
1. ~~Replace "Coming soon" with dynamic previews~~ ✅ DONE
2. ~~Fix collection routing~~ ✅ DONE
3. ~~Remove broken music player~~ ✅ DONE
4. ~~Fix mobile gallery~~ ✅ DONE
5. ~~Fix admin image previews~~ ✅ DONE
6. ~~Redesign Privacy page~~ ✅ DONE
7. Add more micro-interactions to enhance the luxury feel
8. Implement lazy loading for images
9. Add dark/light theme toggle (currently dark only)

---

## Known Issues (Resolved)
- ~~Mobile gallery not responding to touch~~ FIXED
- ~~Admin dashboard showing placeholder instead of images~~ FIXED
- ~~Gold & Silver category not translating~~ FIXED
