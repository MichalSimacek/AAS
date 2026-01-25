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

### Pages Redesigned (2026-01-25) ✅
- Home page (`/`)
- About page (`/About`)
- Contact page (`/Contacts`)
- HowTo page (`/HowTo`)
- Blog Index (`/Blog`)
- Blog Post detail (`/Blog/Post/{id}`)
- Collections Landing (`/Collections/Landing`)
- Collections Index (`/Collections`)
- Collections Detail (`/Collections/Details/{slug}`)

### Bug Fixes (2026-01-25) ✅
- **Collection routing fixed**: Links now correctly point to `/Collections/Details/{slug}` instead of broken `/item/{slug}`
- **Music player removed**: Background music feature removed as it didn't work properly

### "Coming soon" Replaced with "Discover" (2026-01-25) ✅
- Collections Landing page now shows "Discover" for empty categories
- Added preview cards section for latest items from each category

### Previous Features (preserved)
- Collections Landing page with 5 category cards
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
│   └── site.css              # Complete redesign with dark theme + animations
├── Pages/Collections/
│   └── Landing.cshtml        # Category landing with dynamic previews
├── Views/
│   ├── Home/Index.cshtml     # Redesigned home page
│   ├── About/Index.cshtml    # Redesigned with stats & services
│   ├── Contacts/Index.cshtml # Redesigned contact form
│   ├── HowTo/Index.cshtml    # Redesigned steps
│   ├── Blog/
│   │   ├── Index.cshtml      # Redesigned blog list
│   │   └── Post.cshtml       # Redesigned blog post detail
│   ├── Collections/
│   │   ├── Index.cshtml      # Collection grid (routing fixed)
│   │   └── Detail.cshtml     # Redesigned collection detail
│   └── Shared/_Layout.cshtml # Navigation, footer, fonts, animations (music removed)
└── Resources/                # 10 language files
```

---

## Test Reports
- `/app/test_reports/iteration_3.json` - Initial redesign tests (100% pass)
- `/app/test_reports/iteration_4.json` - Full redesign verification (100% pass)

---

## Technical Notes
- Google Fonts: Playfair Display, Cinzel, Manrope (loaded after GDPR consent)
- CSS Variables for consistent theming
- Hover effects with smooth transitions
- Purple radial gradients in hero sections
- Scroll-reveal animations via Intersection Observer API
- CSS keyframe animations (fade-in-up, spotlight-bg, glow)

## Test Credentials
- Admin: admin@localhost / Admin123!@#$

---

## Backlog / Future Enhancements
1. ~~Replace "Coming soon" with dynamic previews~~ ✅ DONE
2. ~~Fix collection routing~~ ✅ DONE
3. ~~Remove broken music player~~ ✅ DONE
4. Add more micro-interactions to enhance the luxury feel
5. Implement lazy loading for images
6. Add dark/light theme toggle (currently dark only)
