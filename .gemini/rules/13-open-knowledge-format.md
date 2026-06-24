# Rule: Open Knowledge Format (OKF) Documentation Standards

## Metadata
- **ID**: RULE-013-OKF-STANDARDS
- **Scope**: Entire Project (Documentation and AI Resources)
- **Target Language**: Markdown / YAML
- **Status**: Active

## Overview
This rule defines the standards for creating, updating, and structuring project documentation and metadata assets in compliance with the Google **Open Knowledge Format (OKF) v0.1** specification. This ensures all knowledge resources are portable, human-readable, and easily parseable by AI agents under the LLM-wiki pattern.

---

## 1. OKF Document Structure
Every document inside the documentation directory (e.g., `docs/`) must follow the strict Markdown-with-YAML-frontmatter layout.

### Frontmatter Requirements
Every Markdown file must begin with a YAML frontmatter block enclosed by triple-dashes (`---`).
*   **Mandatory Field**:
    *   `type`: (string) The classification of the knowledge asset. Must be one of:
        *   `architecture_document` - System structures and layer patterns.
        *   `database_table` - Structural database entity details.
        *   `api_endpoint` - REST or message-based endpoint definition.
        *   `pipeline_specification` - Data flows or ETL details.
        *   `business_rule` - Enforced core business calculations or invariants.
        *   `prompt_template` - Formatted instruction outlines for AI orchestration.
        *   `code_pattern` - Reusable coding guides or practices.
        *   `index` - Reserved for folder directories.
        *   `log` - Reserved for project changelogs.
*   **Common Optional Fields**:
    *   `title`: (string) Human-readable title of the document.
    *   `description`: (string) Short summary of the asset's purpose.
    *   `resource`: (string) Reference identifier or code namespace.
    *   `tags`: (array of strings) Categorical markers for tagging search indexes.
    *   `timestamp`: (ISO 8601 string) Date and time of creation/last update.

### Body Format
*   Following the closing `---`, write in standard UTF-8 Markdown.
*   Utilize structured HTML5 headings (`#`, `##`), lists, code blocks, and markdown tables to format specifications.

---

## 2. Directory Indexing (`index.md`)
Every documentation subfolder (and the root `docs/` folder) must include an `index.md` file containing directory pointers for progressive discovery.

### Requirements:
1.  Frontmatter must set:
    *   `type: index`
    *   `okf_version: "0.1"`
2.  The body must list the folders and files in that directory using standard relative Markdown links.

### Example `index.md` Frontmatter:
```yaml
---
type: index
okf_version: "0.1"
title: Documentation Directory
description: Navigation index for system architecture and data resources.
timestamp: 2026-06-24T07:15:00-03:00
---
```

---

## 3. Version Tracking (`log.md`)
A central `log.md` file must exist at the root of the documentation folder to track chronological changes to the knowledge vault.

### Requirements:
1.  Frontmatter must set `type: log`.
2.  The body must contain a Markdown table detailing each document update with the following columns:
    *   `Date` (ISO 8601 string or date)
    *   `Agent / Author` (Identifies who/which subagent made the change)
    *   `Action` (e.g., Created, Modified, Deleted)
    *   `Target Document` (Relative link to the modified document)
    *   `Summary of Changes` (Brief description of updates)

### Example `log.md` Content:
```markdown
---
type: log
title: Knowledge Catalog Update Log
description: Chronological log of catalog additions and revisions.
---

# Update Log

| Date | Agent / Author | Action | Target Document | Summary of Changes |
| :--- | :--- | :--- | :--- | :--- |
| 2026-06-24 | documentation-architect | Created | [system_overview.md](./system_overview.md) | Initial generation of clean architecture overview |
```

---

## 4. Document Interlinking
*   Always link related OKF concepts using relative markdown links (e.g., `[Database Details](./database/users_table.md)`).
*   Avoid absolute paths, environment-specific URLs, or platform-dependent links.
