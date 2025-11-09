# NavArch Studio Feature Inventory

**Last Updated**: November 4, 2025  
**Purpose**: Comprehensive catalog of all features, their status, technical debt, and roadmap

---

## 📂 Directory Structure

This folder contains organized documentation of all features across the NavArch Studio platform:

| File | Description |
|------|-------------|
| **01-FEATURE-STATUS-OVERVIEW.md** | High-level dashboard with totals and critical status |
| **02-HYDROSTATICS-FEATURES.md** | Hydrostatics module features (calculations, stability, export) |
| **03-HULL-SIZING-FEATURES.md** | Hull sizing module features (solver, workspace, analysis) |
| **04-RESISTANCE-POWERING-FEATURES.md** | Resistance & powering features (Holtrop-Mennen, charts) |
| **05-CATALOG-FEATURES.md** | Reference data catalog (hulls, propellers, water properties) |
| **06-INFRASTRUCTURE-FEATURES.md** | AWS, CI/CD, logging, security, deployment |
| **07-FRONTEND-FEATURES.md** | UI/UX features (components, layouts, interactions) |
| **08-TESTING-DEBT.md** | Testing status, coverage gaps, blockers |
| **09-TECHNICAL-DEBT.md** | All accumulated technical debt by priority |
| **10-QUICK-WINS.md** | Low-effort, high-impact improvements |
| **11-FEATURE-ROADMAP.md** | Prioritized roadmap for next 3 months |

---

## 🎯 How to Use This Inventory

### For Planning
1. Review **01-FEATURE-STATUS-OVERVIEW.md** for current state
2. Check **10-QUICK-WINS.md** for immediate opportunities
3. Consult **11-FEATURE-ROADMAP.md** for prioritized work

### For Development
1. Find your module (02-07) to see what's complete/incomplete
2. Check **08-TESTING-DEBT.md** before writing new features
3. Review **09-TECHNICAL-DEBT.md** to avoid known pitfalls

### For Stakeholders
1. **01-FEATURE-STATUS-OVERVIEW.md** - Executive summary
2. **11-FEATURE-ROADMAP.md** - What's coming next
3. Individual module files - Deep dive into specific areas

---

## 📊 Status Definitions

| Status | Meaning | Next Action |
|--------|---------|-------------|
| **✅ Complete** | Feature is implemented, tested, and deployed | Maintenance only |
| **🔧 In Progress** | Feature is partially implemented | Continue development |
| **📋 Planned** | Feature is designed but not started | Ready to implement |
| **🚫 Blocked** | Feature cannot progress due to external dependency | Resolve blocker |
| **⚠️ Needs Enhancement** | Feature works but needs improvement | Plan enhancement |
| **🐛 Has Issues** | Feature has known bugs or technical debt | Fix issues |

---

## 🏷️ Priority Levels

| Priority | Definition | Timeframe |
|----------|------------|-----------|
| **🔴 Critical** | Blocks other work or production deployment | This week |
| **🟠 High** | Important for user experience or system stability | This month |
| **🟡 Medium** | Valuable but not urgent | Next quarter |
| **🟢 Low** | Nice to have, no immediate impact | Backlog |

---

## 📏 Complexity Scale

| Size | Time Estimate | Description |
|------|---------------|-------------|
| **XS** | < 1 hour | Trivial change |
| **S** | 1-4 hours | Small feature or fix |
| **M** | 1-2 days | Medium feature |
| **L** | 3-5 days | Large feature |
| **XL** | 1-2 weeks | Epic or major module |

---

## 🔄 Updating This Inventory

### When to Update
- ✅ Feature completed
- 🔧 Status changed (planned → in progress → complete)
- 🚫 New blocker discovered
- 📋 New feature planned
- 🐛 Technical debt identified

### How to Update
1. Edit the appropriate module file (02-07)
2. Update status emoji and description
3. Move completed items to "Completed" section
4. Update **01-FEATURE-STATUS-OVERVIEW.md** totals
5. Adjust **11-FEATURE-ROADMAP.md** if priorities change

### Format Standards
- Use consistent emoji indicators
- Keep descriptions concise (1-2 sentences)
- Link to detailed docs in `temp/` or `.plan/`
- Include file paths for code locations
- Estimate effort for planned features

---

## 📚 Related Documentation

### Implementation Details
- **temp/** - Implementation summaries, progress reports, debug notes
- **.plan/completed-features/** - Completed feature documentation
- **.plan/hull-sizing/plan/** - Hull sizing detailed plans

### Architectural Docs
- **.plan/ARCHITECTURE.md** - System architecture
- **.plan/DEPLOYMENT_WORKFLOW.md** - Deployment processes
- **docs/** - Runtime configuration and diagnostics

### Phase Plans
- **.plan/phase1-phase14*.md** - Original phase plans
- **.plan/PRIORITIES.md** - Current priorities and phases

---

## 🎓 Feature Template

When documenting a new feature, use this template:

```markdown
### [Feature Name]

**Status**: Complete | In Progress | Planned | Blocked  
**Priority**: Critical | High | Medium | Low  
**Complexity**: XS | S | M | L | XL  
**Module**: [Module Name]  
**Phase**: Phase X

**Description**: [1-2 sentence description]

**Current State**: [What's implemented]

**Remaining Work**: [What's left to do]

**Blockers**: [Any blocking issues, or "None"]

**Related Docs**:
- `temp/IMPLEMENTATION_SUMMARY.md`
- `.plan/phase-plan.md`

**Code Locations**:
- Backend: `backend/ServiceName/...`
- Frontend: `frontend/src/components/...`
- Tests: `backend/ServiceName.Tests/...`

**Dependencies**: [Other features this depends on]

**Estimated Effort**: [Time estimate for remaining work]
```

---

## 💡 Tips for Using the Inventory

### Finding Information Quickly
- 🔍 Use Ctrl+F to search across all files
- 📊 Start with the overview for big picture
- 🎯 Jump to module files for specific features
- ⚡ Check quick wins for immediate tasks

### Planning a Sprint
1. Review **09-TECHNICAL-DEBT.md** for critical issues
2. Select items from **10-QUICK-WINS.md** for easy progress
3. Choose 1-2 medium features from module files
4. Balance new features with technical debt paydown

### Understanding Dependencies
- Each feature lists its dependencies
- Check blockers before starting work
- Review related docs for context
- Test integrations after changes

---

## 🤝 Contributing

### Adding New Features
1. Choose appropriate module file (02-07)
2. Follow the feature template format
3. Update overview totals
4. Add to roadmap if high priority

### Reporting Issues
1. Document in **09-TECHNICAL-DEBT.md**
2. Assign priority level
3. Link to related features
4. Estimate fix effort

### Completing Features
1. Update status to ✅ Complete
2. Move to "Completed Features" section
3. Update test coverage in **08-TESTING-DEBT.md**
4. Verify no new technical debt introduced

---

## 📞 Questions?

If you need clarification on:
- **Feature status** - Check module file and related docs
- **Priority** - Review with team lead or project manager
- **Technical details** - See implementation summaries in `temp/`
- **Architecture** - Consult `.plan/ARCHITECTURE.md`

---

**Maintained by**: Development Team  
**Review Frequency**: Weekly during sprint planning  
**Last Major Update**: November 4, 2025






