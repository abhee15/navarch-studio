# Catalog Edit Permissions - Architecture Decision

**Date:** November 6, 2025  
**Status:** ✅ DECIDED  
**Decision Maker:** Product/Engineering

---

## 🎯 **DECISION**

**Keep all catalog data READ-ONLY until RBAC is implemented.**

---

## 📋 **CONTEXT**

### **Current State:**
- Real-World Catalog: 600 curated vessels (seeded from CSV)
- ML/Parametric Catalog: 5K-82K parametric hulls (MIT ShipD dataset)
- Both catalogs accessible via unified browser with toggle
- Users can **clone** catalog vessels to personal workspace
- Personal vessels have full CRUD operations

### **Question Raised:**
"Should we give users the ability to edit catalog data from the app, or should it be a separate process?"

### **Discussion:**
1. **Option A**: Enable edit in UI now (simple but risky)
2. **Option B**: Keep read-only, edit via migrations (current)
3. **Option C**: Enable edit with RBAC roles (future)

---

## ✅ **DECISION: OPTION B → C (Phased Approach)**

### **Phase 1 (NOW): Keep Read-Only**
- ✅ All catalog data remains read-only in UI
- ✅ Users clone to personal workspace to edit
- ✅ Catalog updates via database migrations (controlled process)
- ✅ Data integrity maintained
- ✅ Predictable state for testing/validation

### **Phase 2 (AFTER RBAC): Enable Admin Edit**
- 📋 Implement role-based access control (User, Contributor, Curator, Admin)
- 📋 Enable catalog editing for Admin/Curator roles only
- 📋 Add audit logging for all catalog changes
- 📋 Add approval workflow for community submissions (optional)

---

## 🎯 **RATIONALE**

### **Why Keep Read-Only Now:**
1. **Data Integrity** - Reference data stays consistent across all users
2. **Testing** - Predictable baseline for validation and benchmarking
3. **Multi-tenant** - Everyone sees same reference hulls (no confusion)
4. **Version Control** - Changes tracked via migrations (reviewable, rollback-able)
5. **Simple Security** - No permission checks needed yet
6. **Professional** - Curated data implies quality control

### **Why Not Enable Edit Now:**
1. ❌ **No RBAC** - All users would have same permissions (unsafe)
2. ❌ **No Audit Trail** - Can't track who changed what
3. ❌ **Data Corruption Risk** - Accidental edits to reference data
4. ❌ **Testing Issues** - Unstable baseline for validation
5. ❌ **Multi-tenant Conflict** - User A's edit affects User B

### **Why RBAC Is The Right Solution:**
1. ✅ **Role-based permissions** - Admin/Curator can edit, users can't
2. ✅ **Audit logging** - Track all changes with user, timestamp, reason
3. ✅ **Separation of concerns** - Reference vs personal data
4. ✅ **Community features** - Enable submissions with approval workflow
5. ✅ **Professional UX** - Clear who can do what

---

## 🏗️ **CURRENT ARCHITECTURE**

### **Catalog Data Flow (Read-Only):**
```
Seeding (Startup)
    ↓
Database (catalog_hulls, catalog_ml.parametric_hulls)
    ↓
API (GET /catalog/hulls, /catalog/parametric/browse)
    ↓
UI (UnifiedCatalogBrowser - View only)
    ↓
Clone Feature (Creates user vessel)
    ↓
Personal Vessels (Full CRUD)
```

### **Permission Model (Current):**
```
Catalog Data:
- View: ✅ All authenticated users
- Edit: ❌ No one (migrations only)
- Delete: ❌ No one

Personal Vessels:
- View: ✅ Owner only
- Edit: ✅ Owner only
- Delete: ✅ Owner only
```

---

## 🚀 **FUTURE ARCHITECTURE (With RBAC)**

### **Roles:**
- **User** (default) - View catalog, clone, edit personal vessels
- **Contributor** - + Submit new vessels for review
- **Curator** - + Approve/reject submissions, edit catalog
- **Admin** - + Full catalog CRUD, manage roles

### **Permission Model (Future):**
```
Catalog Data:
- View: ✅ All authenticated users
- Clone: ✅ All authenticated users
- Submit: ✅ Contributor+ (with approval queue)
- Edit: ✅ Curator+ (with audit log)
- Delete: ✅ Admin only (with confirmation)
- Approve: ✅ Curator+ (moderation workflow)

Personal Vessels:
- View: ✅ Owner (+ Admin for support)
- Edit: ✅ Owner only
- Delete: ✅ Owner only
```

### **Catalog Edit Workflow (Future):**
```
Admin/Curator clicks "Edit" in catalog browser
    ↓
Edit form (same as personal vessel editor)
    ↓
Validation (same rules as personal vessels)
    ↓
Save with audit log (user_id, timestamp, changes)
    ↓
Broadcast update (WebSocket/SignalR)
    ↓
All users see updated catalog
```

---

## 📊 **CATALOGS: ALWAYS READ-ONLY vs EDITABLE**

| Catalog | Editable After RBAC? | Rationale |
|---------|---------------------|-----------|
| **Real-World Catalog** | ✅ YES (Admin/Curator only) | Curated data that may need corrections, additions, geometry updates |
| **ML/Parametric Catalog** | ❌ NO (Always read-only) | Reference dataset from MIT ShipD - should not be modified to maintain scientific integrity |

### **ML Catalog Rationale:**
- 🔬 **Scientific Dataset** - MIT ShipD is a published research dataset
- 📜 **Citation Required** - Must preserve original data for reproducibility
- 🔒 **Immutable Reference** - Any modifications invalidate comparisons
- 📊 **82,000+ Hulls** - Too large for manual curation
- 🤖 **Generated Data** - Created by algorithms, not human-curated
- 📚 **Version Controlled** - Update entire dataset, not individual hulls

---

## ✅ **IMPLEMENTATION STATUS**

### **Completed (This Session):**
✅ Unified catalog browser with Real/ML toggle  
✅ Permission indicators (Editable vs Read-Only banners)  
✅ Lock badges on ML catalog cards  
✅ Edit icons on Real catalog cards (disabled for now)  
✅ "Add New Vessel" button (disabled for now)  
✅ Clone feature working for both catalogs  
✅ Clear visual distinction (green = real, purple = ML)  

### **Deferred (Until RBAC):**
📋 Enable "Edit" button for Admin role  
📋 Enable "Add New Vessel" for Admin role  
📋 Audit logging for catalog changes  
📋 Approval workflow for submissions  
📋 Role management UI  
📋 Permission middleware  

---

## 🎯 **NEXT STEPS**

### **Immediate (Now):**
1. ✅ Keep current UI as-is (edit buttons visible but not functional)
2. ✅ Document this decision
3. ✅ Continue with Phase 2 ML features

### **Before RBAC Implementation:**
1. 📋 Design role/permission schema
2. 📋 Plan audit logging structure
3. 📋 Design admin catalog edit UI
4. 📋 Plan migration path for existing users

### **RBAC Implementation (Future Phase):**
1. 📋 Add User.Role field (migration)
2. 📋 Create authorization middleware
3. 📋 Add permission checks to catalog endpoints
4. 📋 Enable edit UI for Admin/Curator roles
5. 📋 Add audit logging
6. 📋 Test role-based access
7. 📋 Deploy with feature flag

---

## 📚 **RELATED DOCUMENTS**

- `.plan/features/05-CATALOG-FEATURES.md` - Catalog feature status
- `temp/CATALOG-UNIFIED-FEATURE-SUMMARY.md` - Unified browser implementation
- `.plan/app-docs/hull-sizing/data-driven/PHASE2-COMPLETE-SUMMARY.md` - ML catalog
- **Future**: `.plan/features/RBAC-IMPLEMENTATION-PLAN.md` (to be created)

---

## 💬 **DECISION LOG**

**Q:** "Should catalog data be editable from the app or via separate process?"  
**A:** Separate process (migrations) NOW, editable in app AFTER RBAC.

**Q:** "Should ML catalog ever be editable?"  
**A:** NO - it's a reference dataset that must preserve scientific integrity.

**Q:** "Should real-world catalog be editable after RBAC?"  
**A:** YES - by Admin/Curator roles only, with audit logging.

**Q:** "When to implement RBAC?"  
**A:** After Phase 2 ML features are complete and deployed.

---

## ✅ **BENEFITS OF THIS DECISION**

### **Short-term (Current):**
- 🔒 Data integrity maintained
- 🧪 Stable testing baseline
- 👥 Consistent multi-tenant experience
- 🚀 Fast iteration (no permission complexity)
- 📋 Simple security model

### **Long-term (With RBAC):**
- 🎓 Professional data management
- 👔 Role-based access control
- 📜 Full audit trail
- 🌟 Community contribution pathway
- 🔐 Enterprise-ready permissions
- 📈 Scalable as user base grows

---

## 🎊 **DECISION CONFIRMED**

**Status:** ✅ APPROVED  
**Effective:** Immediate  
**Review Date:** After RBAC implementation  
**Owner:** Product/Engineering Team

**This decision ensures data quality and user trust while preserving flexibility for future enhancements.**

---

**Last Updated:** November 6, 2025  
**Next Review:** When RBAC implementation begins

