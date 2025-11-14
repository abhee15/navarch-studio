# NavArch Studio Planning Documentation

This directory contains planning, design, and reference documentation for the NavArch Studio project.

## Directory Structure

### 📁 `active/`
**Active development work and current planning documents**
- `hull-sizing/` - Current hull sizing module documentation and plans

### 📁 `phases/`
**Phase-by-phase implementation plans**
- All `phase*.md` files documenting the implementation phases
- Organized by phase number (phase0 through phase11)

### 📁 `features/`
**Feature inventory and roadmap**
- Feature status overview
- Feature documentation by module (Hydrostatics, Hull Sizing, Resistance, Catalog, etc.)
- Technical debt tracking
- Feature roadmap

### 📁 `deployment/`
**Deployment and infrastructure documentation**
- Deployment prerequisites and readiness checklists
- Deployment workflows
- Environment configuration
- AWS IAM setup
- GitHub secrets configuration

### 📁 `archive/`
**Historical and completed documentation**
- `summaries/` - Final summaries and completion reports
- `completed-features/` - Feature implementation summaries
- `hull-sizing/` - Completed hull-sizing documentation

### 📁 `reference/`
**Reference documentation and guides**
- Architecture documentation
- Setup guides
- Migration strategies
- Manual testing guides
- User guides
- Diagnostics and troubleshooting

### 📁 `app-docs/`
**Application-specific documentation and data**
- `catalog/` - Catalog feature documentation
- `hull-sizing/` - Hull sizing module specs and data
- `hydrostatics/` - Hydrostatics module specs and reference data
- `resistance and powering/` - Resistance/powering specs and templates
- `templates/` - Data templates and formats

### 📁 `decisions/`
**Architectural and design decisions**
- Key design decisions with rationale

## Quick Navigation

### Looking for...?

- **Current work?** → `active/`
- **Implementation phases?** → `phases/`
- **Feature status?** → `features/README.md`
- **Deployment info?** → `deployment/`
- **Architecture docs?** → `reference/ARCHITECTURE.md`
- **Setup instructions?** → `reference/SETUP.md`
- **Completed features?** → `archive/completed-features/`
- **Historical summaries?** → `archive/summaries/`

## File Organization Principles

1. **Active work** stays in `active/` for easy access
2. **Completed work** moves to `archive/` to reduce clutter
3. **Reference docs** stay in `reference/` for long-term access
4. **Phase plans** are in `phases/` for chronological reference
5. **Features** are tracked in `features/` for current status

## Maintenance

When documents are completed:
1. Move to appropriate `archive/` subdirectory
2. Update links in this README if needed
3. Keep active documentation in `active/` or root `features/`

When new phase/feature work starts:
1. Create documentation in appropriate directory
2. Update this README with new navigation pointers
3. Link from relevant feature/phase documentation

## Notes

- CSV files in `app-docs/` that have been integrated into the codebase are kept for reference
- Duplicate files that are actively seeded from `backend/DataService/Data/Seeds/` have been removed
- Large datasets (like Ship_D_Dataset) are not stored in `.plan/` to keep repository size manageable
