# Aristocratic Artwork Sale - Product Requirements Document

## Original Problem Statement
ASP.NET Core web application for selling aristocratic artwork, antiques, jewelry, and watches. The application features multi-language support (10 languages), GDPR compliance, and professional presentation of luxury items.

## Design Language (Updated 2026-01-25)
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
- Consistent dark theme across all pages
- Glass-morphism effects on cards

### Previous Features (preserved)
- Collections Landing page with 5 category cards
- Background music with toggle button
- Multi-language support (10 languages)
- GDPR-compliant cookie consent
- Collection category filter (Decorative Arts renamed)
- Price field accepting text values
- HowTo page text updates

---

## File Structure
```
/app/src/AAS.Web/
├── wwwroot/css/
│   └── site.css              # Complete redesign with dark theme
├── Pages/Collections/
│   └── Landing.cshtml        # Category landing page
├── Views/
│   ├── Home/Index.cshtml     # Redesigned home page
│   ├── Collections/Index.cshtml  # Collection grid
│   └── Shared/_Layout.cshtml # Navigation, footer, fonts
└── Resources/                # 10 language files
```

---

## Test Reports
- `/app/test_reports/iteration_3.json` - Design redesign tests (100% pass)

---

## Technical Notes
- Google Fonts: Playfair Display, Cinzel, Manrope
- CSS Variables for consistent theming
- Hover effects with smooth transitions
- Purple radial gradients in hero sections

## Test Credentials
- Admin: admin@localhost / Admin123!@#$
