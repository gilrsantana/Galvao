---
name: manage-okf-documentation
description: Guide AI agents to generate, update, and organize documentation and resource templates as Google OKF (Open Knowledge Format) bundles.
---

# Skill: Managing OKF Documentation

This skill outlines how to build, extend, and validate a Google Open Knowledge Format (OKF) compliant documentation bundle inside a project directory.

---

## Steps

### 1. Structure Initialization
*   Locate or create the root documentation directory (typically named `docs/` at the root of the project).
*   Ensure the directory structure is organized:
    *   `docs/README.md` - Main system/project overview.
    *   `docs/index.md` - Global bundle directory index.
    *   `docs/log.md` - Chronological log of modifications.
    *   Subdirectories (e.g., `docs/architecture/`, `docs/database/`, `docs/api/`) for different documentation domains.

### 2. Crafting OKF Documents
When creating or editing a documentation file:
1.  Add the YAML frontmatter block at the very top of the markdown file.
2.  Define the appropriate `type` field based on the rule `RULE-013-OKF-STANDARDS`.
3.  Fill out optional metadata fields such as `title`, `description`, `tags`, and `timestamp`.
4.  Provide the structured content in markdown below the frontmatter.

#### Example Document Creation:
```markdown
---
type: database_table
title: Users Database Schema
description: Columns, types, and constraints for the primary User entity.
resource: StoreBackend.Domain.Entities.User
tags: [database, schema, identity]
timestamp: 2026-06-24T07:16:00-03:00
---

# Users Schema

| Column Name | Data Type | Constraint | Description |
| :--- | :--- | :--- | :--- |
| Id | UNIQUEIDENTIFIER | PRIMARY KEY | Version 7 UUID |
| Email | NVARCHAR(256) | UNIQUE, NOT NULL | User account email |
```

### 3. Populating Directories (`index.md`)
*   Every subdirectory must contain an `index.md` file representing the local directory index.
*   The frontmatter of the root `docs/index.md` must declare `okf_version: "0.1"`.
*   Maintain lists of links to subfolders and documentation files inside the body of each index.

#### Example Directory Index (`index.md`):
```markdown
---
type: index
okf_version: "0.1"
title: System Catalog
description: Central entry point to the system catalog documentation.
---

# System Catalog

*   [System Overview](./README.md)
*   [Architecture Details](./architecture/index.md)
*   [Database Models](./database/index.md)
```

### 4. Logging Modifications (`log.md`)
*   Whenever a file is created, updated, or deleted, add a row to the central `log.md` update table.
*   Include the Date, Author/Agent name, Action type, Target Document relative link, and a brief description of the edit.

### 5. OKF Conformance Validation
Before finalizing a task:
1.  Verify that all non-reserved `.md` files in `docs/` have a YAML frontmatter starting with `---` and ending with `---`.
2.  Verify that the `type` field is present and non-empty in all files.
3.  Check that all relative folder links and target paths resolve correctly and aren't broken.
