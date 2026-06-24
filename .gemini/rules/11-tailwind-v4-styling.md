# Rule: Tailwind CSS v4 Theme and Styling Standards

## Metadata
- **ID**: RULE-011-TAILWIND-V4-STYLING
- **Scope**: Frontend (Angular templates & CSS)
- **Target Types**: HTML, CSS, TS Component styling
- **Status**: Active

## Overview
This rule defines the styling standards for the frontend application using Tailwind CSS v4. It enforces the use of custom theme tokens, modern typography, glassmorphism aesthetics, smooth motion transitions, and responsive grid layouts to preserve a premium visual quality.

---

## 1. Tailwind v4 Theme Tokens (`@theme` in `styles.css`)
Developers must always use the variables configured in the `@theme` block of [styles.css](file:///home/gilmar/Development/ai-driven-development/projects/galvao/frontend/src/styles.css). Never hardcode arbitrary colors or fonts in the inline HTML class lists.

### A. Color Palette
- **Backgrounds**:
  - Primary canvas: `bg-bg-primary` (mapped to `#FAF9F6`)
  - Accent cards/sections: `bg-bg-secondary` (mapped to `#F3F0E8`)
  - Dark elements/overlays: `bg-bg-dark` (mapped to `#1F1F1C`)
- **Text**:
  - Primary headings/body: `text-text-primary` (mapped to `#1E1E1A`)
  - Secondary metadata/labels: `text-text-secondary` (mapped to `#6B6961`)
  - Light mode inverse: `text-text-light` (mapped to `#FAF9F6`)
- **Accent Gold**:
  - Brand gold highlight: `text-accent-gold` (mapped to `#C5A880`)
  - Brand gold hover state: `text-accent-gold-hover` (mapped to `#B4966E`)

### B. Typography
- **Sans-Serif (Body & UI)**:
  - Font Stack: `font-sans` (uses Google Font 'Outfit')
- **Serif (Headings & Titles)**:
  - Font Stack: `font-serif` (uses Google Font 'Playfair Display')

---

## 2. Micro-Animations and Transitions
To maintain a responsive and alive user experience:
- **Hover Transitions**: Always use the smooth custom transitions and transformation offsets when designing interactive elements:
  - Transition duration & curve: `transition-all duration-400 ease-[cubic-bezier(0.16,1,0.3,1)]`
  - Action offset on hover: `-translate-y-1.5 shadow-lg border-accent-gold`
- **Button Standards**:
  - Use the preset `.btn`, `.btn-primary`, `.btn-secondary`, and `.btn-gold` helper utility styles to keep buttons aligned with standard heights, paddings, and hover styles.

---

## 3. Premium Interface Patterns (Glassmorphism & Modals)
- **Glass Navbar & Panels**: Use the glass opacity background combined with a light border and backdrop blur:
  - Glass combination: `bg-bg-primary/75 backdrop-blur-md border-b border-black/5`
- **Overlay & Modals**:
  - Backdrop overlay: `fixed inset-0 bg-bg-dark/40 backdrop-blur-xs z-50 flex items-center justify-center`
  - Modal content containers must slide up and fade in smoothly on load.

---

## 4. Example Premium Component Template
```html
<div class="premium-card p-6 flex flex-col justify-between h-full bg-bg-primary border border-black/10 rounded-xl transition-all duration-400 ease-[cubic-bezier(0.16,1,0.3,1)] hover:-translate-y-1.5 hover:shadow-lg hover:border-accent-gold">
  <div>
    <span class="badge badge-gold bg-accent-gold/15 text-[#a48252] mb-3">Featured Item</span>
    <h3 class="text-xl font-serif text-text-primary mb-2">The Golden Hour Showroom</h3>
    <p class="text-sm text-text-secondary font-sans leading-relaxed">
      Curated showcase of architectural design models constructed using premium materials.
    </p>
  </div>
  <div class="mt-6 flex justify-between items-center">
    <span class="text-lg font-serif text-accent-gold font-semibold">$1,200.00</span>
    <a routerLink="/showroom" class="btn btn-sm btn-primary">View Details</a>
  </div>
</div>
```
