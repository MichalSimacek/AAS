# Aristocratic Artwork Sale - Product Requirements Document

## Session 2026-04-21 (pt.3) — Bulk actions in Admin Collections

### Completed
- **Bulk selection UI**: checkboxes on every row + "select all" in table header with indeterminate
  state when partial selection. Selected rows highlighted with gold left border.
- **Bulk actions bar** (appears when ≥1 selected): shows selected count, Clear button, and three
  action buttons — Hide selected / Show selected / Delete selected. Confirmation dialog for each.
- **Backend endpoint** `POST /Admin/Collections/BulkAction` (antiforgery protected) in
  `Areas/Admin/Controllers/CollectionsController.cs` handles `hide`, `show`, `delete` actions;
  uses `.AsTracking()` + single `SaveChangesAsync()` per request. For `delete` also cleans up
  image and audio files on disk.
- Current visibility filter preserved across bulk redirect (hidden `visibility` form input).
- CSS for bulk bar + custom dark gold-accented checkboxes in `wwwroot/css/admin-dark.css`.
- E2E verified via curl + UI screenshot: bulk hide flips IsHidden for multiple IDs, tabs update
  counts live, bulk delete removes rows and frees assets.

## Session 2026-04-21 (pt.2) — Hide/Show fix, admin filter, dark admin redesign

### Completed
- **Hide/Show bug ROOT CAUSE fixed**: `AppDbContext` uses `QueryTrackingBehavior.NoTracking` by default,
  so `ToggleVisibility` was mutating a non-tracked entity and `SaveChangesAsync()` persisted nothing.
  Fixed by adding `.AsTracking()` in `Areas/Admin/Controllers/CollectionsController.ToggleVisibility`.
  End-to-end test verified: toggle → DB flips → public `/Collections` hides/shows the item immediately.
- **Response cache hardened**: public `CollectionsController` marked
  `[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None, Duration = 0)]` to prevent any
  downstream (browser / CDN / proxy) caching of visibility state.
- **Admin visibility filter (All / Visible / Hidden) added**: new `visibility` query param in Admin
  `CollectionsController.Index`; new tab pills UI in `Areas/Admin/Views/Collections/Index.cshtml` with
  live counts per tab; current filter preserved across toggle redirects via hidden form field.
  Removed broken price-range inputs (controller no longer supports them — `Price` is free-text).
- **Admin panel dark aristocratic redesign**: 
  - New global override stylesheet `wwwroot/css/admin-dark.css` (re-themes all admin pages without
    rewriting each cshtml — rules scoped by `.admin-dashboard`, `.admin-collections-page`,
    `.admin-blog-page`, `.admin-inquiries-page`).
  - New wrapper `Areas/Admin/Views/Shared/_AdminLayout.cshtml` that injects `admin-dark.css` into
    the main `_Layout.cshtml`.
  - `_ViewStart.cshtml` updated to use the new layout.
  - All admin pages now match site aesthetic: `#050505` background, `#D4AF37` gold accents,
    Playfair Display headings, gold-gradient primary buttons, dark glass cards, custom dark tables.

### Pending / follow-up
- Ensure env var `DEEPL_API_KEY` is set on the production server (systemd unit / docker-compose).
- Consider adding 2FA for admin account.
- Consider replacing `unsafe-inline` in CSP with nonce-based CSP.

## Session 2026-04-21 — Security hardening & translations

### Completed
- Translations: Added ~1,146 resource entries across 10 languages (`Resources.SharedResources.*.resx`).
  Deduplicated case-insensitive duplicates. Russian/German/etc. now fully translated on nav, cookie banner,
  contact form, landing page, blog.
- Collection visibility toggle: new `IsHidden` column + EF migration `AddCollectionIsHidden`; admin action
  `POST /Admin/Collections/ToggleVisibility/{id}` with antiforgery; eye/eye-slash button in admin table with
  row striping + `Hidden` badge. Public `CollectionsController.Index`, `Details`, `Pages/Collections/Landing`
  (category counts, hero images, previews), and `SitemapController` all filter `!c.IsHidden`.
- Rate limiting: ASP.NET Core `RateLimiter` with global policy (300/min/IP) + named policies `auth` (10/15min),
  `api` (60/min), `contact` (5/15min), `comments` (20/10min). Applied to Login, Register, Contacts, Comments API.
- CSRF hardening: `CommentsController` now uses `[AutoValidateAntiforgeryToken]`.
- File upload: `BlogController` image upload hardened — extension whitelist, 8 MB cap, ImageSharp content
  validation, GUID-only filename (never trusts client name).
- Stored XSS defense: `Ganss.Xss.HtmlSanitizer` sanitizes blog `ContentCs` before DB save.
- Cookie hardening: application cookies are HttpOnly, SameSite=Lax, Secure=SameAsRequest, 2h sliding.
- SMTP injection: `ContactsController.Submit` strips CR/LF/control chars, validates email with
  `System.Net.Mail.MailAddress`, enforces max lengths (120/254/200/5000).
- Secret removal: Hardcoded DeepL API key removed from `appsettings.Production.json` (now empty;
  resolved from env var `DEEPL_API_KEY` at runtime).
- Vulnerable dependencies upgraded: `HtmlSanitizer 9.0.892`, `MailKit 4.16.0`.
- Request body limit: `FormOptions.MultipartBodyLengthLimit = 100 MB`.
- Build verified on local container with .NET 8 SDK — 0 errors, 1 pre-existing warning.

### Pending / follow-up
- Ensure env var `DEEPL_API_KEY` is set on the production server (systemd unit / docker-compose).
- Consider adding 2FA for admin account.
- Consider replacing `unsafe-inline` in CSP with nonce-based CSP.
- `Collection.Price` stays as `string` by user request (allows free-text like "Price on request").


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
