#!/usr/bin/env python3
import os
import re
import sys

# Define valid types from RULE-013-OKF-STANDARDS
VALID_OKF_TYPES = {
    "architecture_document",
    "database_table",
    "api_endpoint",
    "pipeline_specification",
    "business_rule",
    "prompt_template",
    "code_pattern",
    "index",
    "log"
}

def parse_frontmatter(content):
    """
    Parses a simple YAML frontmatter delimited by '---' at the start of a file.
    Returns a dictionary of parsed keys and values, and any parsing error message.
    """
    if not content.startswith("---"):
        return None, "File does not start with triple-dash frontmatter delimiter ('---')"
        
    # Find the closing delimiter
    parts = content.split("---", 2)
    if len(parts) < 3:
        return None, "Missing closing triple-dash frontmatter delimiter ('---')"
        
    yaml_block = parts[1]
    metadata = {}
    
    for line in yaml_block.splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
            
        if ":" not in line:
            return None, f"Malformed line in frontmatter (missing colon): '{line}'"
            
        key, value = line.split(":", 1)
        key = key.strip()
        value = value.strip().strip('"').strip("'")
        
        # Handle list format like [a, b, c]
        if value.startswith("[") and value.endswith("]"):
            value = [v.strip().strip('"').strip("'") for v in value[1:-1].split(",") if v.strip()]
            
        metadata[key] = value
        
    return metadata, None

def check_relative_links(file_path, content, all_files):
    """
    Extracts markdown links and verifies that their relative targets exist on disk.
    Returns a list of error messages for broken links.
    """
    # Regex to find links: [text](link)
    # Ignore absolute links starting with http://, https://, or mailto:
    link_pattern = re.compile(r'\[([^\]]+)\]\(([^)]+)\)')
    errors = []
    
    file_dir = os.path.dirname(file_path)
    
    for match in link_pattern.finditer(content):
        link_text = match.group(1)
        link_target = match.group(2).split("#")[0]  # Remove section anchors
        
        # Ignore external links, mailto and placeholders
        if link_target.startswith(("http://", "https://", "mailto:", "javascript:")) or not link_target.strip():
            continue
            
        # Target path relative to the current file
        target_path = os.path.normpath(os.path.join(file_dir, link_target))
        
        # Check if target file exists
        if not os.path.exists(target_path):
            errors.append(f"Broken link in '{os.path.basename(file_path)}': text='{link_text}', target='{link_target}' (Resolved to: '{target_path}')")
            
    return errors

def validate_catalog(docs_dir):
    """
    Walks the docs directory and validates all markdown files against OKF rules.
    """
    print("=" * 60)
    print("   GALVÃO - OKF DOCUMENTATION CATALOG VALIDATOR")
    print("=" * 60)
    
    errors_found = False
    validated_count = 0
    
    # Gather all files in the directory for validation context
    all_files = []
    for root, dirs, files in os.walk(docs_dir):
        for file in files:
            all_files.append(os.path.normpath(os.path.join(root, file)))
            
    for file_path in all_files:
        # We only validate markdown files
        if not file_path.endswith(".md"):
            continue
            
        relative_path = os.path.relpath(file_path, docs_dir)
        validated_count += 1
        
        print(f"Validating: {relative_path}...", end="")
        
        try:
            with open(file_path, "r", encoding="utf-8") as f:
                content = f.read()
        except Exception as e:
            print(" [FAIL]")
            print(f"  Error: Could not read file: {e}")
            errors_found = True
            continue
            
        # Parse YAML frontmatter
        metadata, err = parse_frontmatter(content)
        if err:
            print(" [FAIL]")
            print(f"  Metadata error: {err}")
            errors_found = True
            continue
            
        # Verify required 'type' field
        doc_type = metadata.get("type")
        if not doc_type:
            print(" [FAIL]")
            print("  Metadata error: Missing mandatory 'type' field in frontmatter")
            errors_found = True
            continue
            
        if doc_type not in VALID_OKF_TYPES:
            print(" [FAIL]")
            print(f"  Metadata error: Invalid 'type' value: '{doc_type}'. Must be one of {sorted(list(VALID_OKF_TYPES))}")
            errors_found = True
            continue
            
        # Verify root index version constraint
        if relative_path == "index.md":
            okf_version = metadata.get("okf_version")
            if okf_version != "0.1":
                print(" [FAIL]")
                print(f"  Index error: Root 'index.md' must declare 'okf_version: \"0.1\"' (Found: '{okf_version}')")
                errors_found = True
                continue
                
        # Check for broken links
        link_errors = check_relative_links(file_path, content, all_files)
        if link_errors:
            print(" [FAIL]")
            for err in link_errors:
                print(f"  {err}")
            errors_found = True
            continue
            
        print(" [PASS]")
        
    print("-" * 60)
    if errors_found:
        print(f"Validation FAILED. Checked {validated_count} documents. Resolve the errors listed above.")
        return False
    else:
        print(f"Validation PASSED. Checked {validated_count} documents successfully.")
        return True

if __name__ == "__main__":
    # Determine the script directory
    script_dir = os.path.dirname(os.path.abspath(__file__))
    
    # Target directory is the 'docs' folder where the script is located
    docs_dir = script_dir
    
    # Run validation
    success = validate_catalog(docs_dir)
    sys.exit(0 if success else 1)
