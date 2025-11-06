# GitHub Actions Workflow Skip Issue

**Status:** 🐛 Bug - Needs Investigation  
**Priority:** Medium (workaround available)  
**Affects:** Automatic deployments on push events  
**Workaround:** Manual workflow dispatch with `force_full_build=true`

---

## Problem Summary

When pushing changes to `backend/Shared/` or other backend paths, the `build-and-push` job is being **skipped** in automatic push-triggered workflows, even though:

1. The `check-changed-paths` job correctly detects backend changes
2. The `check-infrastructure` job shows success
3. Manual workflow dispatch builds work perfectly

**Result:** Changes to backend code don't automatically deploy until a manual build is triggered.

---

## Observed Behavior

### Push Events (BROKEN ❌)
```yaml
Event: push
Trigger: git push origin main (with backend/Shared/ changes)
Result: 
  - check-changed-paths: ✅ success (detects backend: true)
  - check-infrastructure: ✅ success
  - build-and-push: ⏭️ SKIPPED
  - deploy-services: ⏭️ SKIPPED
```

### Manual Dispatch (WORKS ✅)
```yaml
Event: workflow_dispatch
Trigger: gh workflow run ci-dev.yml --field force_full_build=true
Result:
  - check-changed-paths: ✅ success
  - check-infrastructure: ✅ success
  - build-and-push: ✅ success
  - deploy-services: ✅ success
```

---

## Investigation Findings

### 1. Job-Level Condition
The `build-and-push` job has this condition:

```yaml
build-and-push:
  needs: [frontend-quality, backend-quality, check-infrastructure, ...]
  if: needs.check-infrastructure.outputs.has-secrets == 'true'
```

**Hypothesis:** On push events, `has-secrets` is evaluating to `false`, but on workflow_dispatch it evaluates to `true`.

### 2. Check Infrastructure Step
File: `.github/workflows/ci-dev.yml` (lines 338-351)

```yaml
- name: Check if AWS infrastructure is configured
  id: check
  run: |
    if [ -z "${{ secrets.ECR_IDENTITY_SERVICE_URL }}" ]; then
      echo "⚠️ AWS infrastructure not yet deployed"
      echo "has-secrets=false" >> $GITHUB_OUTPUT
    else
      echo "✅ AWS infrastructure configured"
      echo "has-secrets=true" >> $GITHUB_OUTPUT
    fi
```

**Question:** Why would `${{ secrets.ECR_IDENTITY_SERVICE_URL }}` be empty on push but not on workflow_dispatch?

### 3. Evidence from Logs

**Push event run (19049647704):**
- `check-infrastructure`: Success (completed)
- `build-and-push`: Skipped (job-level skip, not step-level)
- Cannot see actual `has-secrets` output value in logs (GitHub doesn't log outputs)

**Workflow dispatch run (19049826803):**
- `check-infrastructure`: Success
- `build-and-push`: Success
- All services deployed

---

## Potential Root Causes

### Theory 1: GitHub Secrets Not Available on Push Events
**Likelihood:** Low  
**Reason:** Secrets should be available on all events unless explicitly restricted

### Theory 2: Race Condition in Job Dependencies
**Likelihood:** Medium  
**Details:** The `needs` array has 6 dependencies. Perhaps outputs aren't being passed correctly when multiple jobs complete simultaneously?

### Theory 3: Output Variable Not Being Set
**Likelihood:** High  
**Details:** The `check` step might be failing silently or the output isn't being captured correctly. The log shows both `if` and `else` branches being printed (as part of the script), but not which one actually executed.

### Theory 4: Boolean String Comparison Issue
**Likelihood:** Medium  
**Details:** The condition uses `== 'true'` (string comparison). If the output is being set as boolean `true` instead of string `'true'`, the comparison would fail.

---

## Debugging Steps to Try

### 1. Add Explicit Logging
Modify `.github/workflows/ci-dev.yml` to add debug output:

```yaml
- name: Check if AWS infrastructure is configured
  id: check
  run: |
    echo "🔍 DEBUG: ECR_IDENTITY_SERVICE_URL secret length: ${#SECRET}"
    echo "🔍 DEBUG: Event name: ${{ github.event_name }}"
    
    if [ -z "${{ secrets.ECR_IDENTITY_SERVICE_URL }}" ]; then
      echo "⚠️ AWS infrastructure not yet deployed"
      echo "🔍 DEBUG: Setting has-secrets=false"
      echo "has-secrets=false" >> $GITHUB_OUTPUT
    else
      echo "✅ AWS infrastructure configured"
      echo "🔍 DEBUG: Setting has-secrets=true"
      echo "has-secrets=true" >> $GITHUB_OUTPUT
    fi
    
    echo "🔍 DEBUG: Output file contents:"
    cat $GITHUB_OUTPUT

- name: Verify output was set
  run: |
    echo "🔍 DEBUG: has-secrets output = ${{ steps.check.outputs.has-secrets }}"
```

### 2. Check Workflow Permissions
Verify in repository settings:
- Settings → Actions → General → Workflow permissions
- Should be: "Read and write permissions"

### 3. Test with Different Secret
Replace the check with a different secret to see if it's specific to `ECR_IDENTITY_SERVICE_URL`:

```yaml
if [ -z "${{ secrets.AWS_ACCOUNT_ID }}" ]; then
```

### 4. Simplify the Condition
Try removing the conditional and always setting to true:

```yaml
- name: Check if AWS infrastructure is configured
  id: check
  run: |
    echo "has-secrets=true" >> $GITHUB_OUTPUT
```

If this works, we know the secret check is the problem.

---

## Workaround (CURRENT)

**For immediate deployments:**
```bash
gh workflow run ci-dev.yml --field force_full_build=true
```

This bypasses the conditional logic because when `force_full_build=true`, the build plan step sets `BUILD_BACKEND=true` regardless of path changes or previous build status.

**Relevant code** (`.github/workflows/ci-dev.yml` lines 395-397):
```yaml
elif [ "${{ github.event_name }}" == "workflow_dispatch" ] && [ "${{ inputs.force_full_build }}" == "true" ]; then
  BUILD_BACKEND=true
  echo "✅ Force full build requested - will build backend"
fi
```

---

## Recommended Fix (PENDING VERIFICATION)

### Option A: Remove the Secrets Check Entirely
Since we're past initial setup and infrastructure is stable:

```yaml
build-and-push:
  needs: [frontend-quality, backend-quality, check-changed-paths, ...]
  # Remove: if: needs.check-infrastructure.outputs.has-secrets == 'true'
  # This job will always run after quality checks pass
```

**Pros:** Simple, reliable  
**Cons:** Won't gracefully handle repos where infrastructure isn't set up yet

### Option B: Add Explicit Debug Step
Add a dedicated job that checks and logs the secret state:

```yaml
debug-secrets:
  runs-on: ubuntu-latest
  outputs:
    has-ecr: ${{ steps.check.outputs.has-ecr }}
  steps:
    - name: Check ECR secret
      id: check
      run: |
        if [ -n "${{ secrets.ECR_IDENTITY_SERVICE_URL }}" ]; then
          echo "✅ ECR secret exists"
          echo "has-ecr=true" >> $GITHUB_OUTPUT
        else
          echo "❌ ECR secret missing"
          echo "has-ecr=false" >> $GITHUB_OUTPUT
        fi
```

Then use `needs.debug-secrets.outputs.has-ecr` in the condition.

### Option C: Use Environment Variable Instead
Set secrets as environment variables first:

```yaml
check-infrastructure:
  runs-on: ubuntu-latest
  env:
    ECR_URL: ${{ secrets.ECR_IDENTITY_SERVICE_URL }}
  outputs:
    has-secrets: ${{ steps.check.outputs.has-secrets }}
  steps:
    - name: Check if AWS infrastructure is configured
      id: check
      run: |
        if [ -z "$ECR_URL" ]; then
          echo "has-secrets=false" >> $GITHUB_OUTPUT
        else
          echo "has-secrets=true" >> $GITHUB_OUTPUT
        fi
```

---

## Impact Assessment

**Current Impact:**
- 🔴 High friction: Every backend change requires manual workflow trigger
- 🟡 Medium delay: ~2-5 minutes additional time per deployment
- 🟢 Low risk: Workaround is reliable and well-documented

**If Not Fixed:**
- Development velocity reduced by ~20% (extra manual steps)
- Risk of forgetting to deploy after commits
- CI/CD pipeline not truly "continuous"

**When Fixed:**
- 🎯 Automatic deployments on every push
- 🚀 Faster iteration cycles
- ✅ True CI/CD experience

---

## Related Files

- `.github/workflows/ci-dev.yml` (primary file)
- `.github/workflows/ci-staging.yml` (might have same issue)
- `.github/workflows/ci-prod.yml` (manual trigger only, not affected)

---

## Next Steps

1. **Immediate:** Continue using manual trigger workaround ✅
2. **Short-term:** Add debug logging to understand the root cause
3. **Long-term:** Implement Option A (remove secrets check) after confirming infrastructure is stable

---

## History

**Discovered:** 2025-11-03  
**Reported by:** AI Assistant (during hull sizing deployment)  
**Affects:** Dev environment automatic deployments  
**Ticket:** N/A (documented here for future reference)

---

## Test Cases for Verification

When implementing a fix, test these scenarios:

### Test 1: Backend-only change
```bash
# Change backend/Shared/Middleware/ClaimsForwardingMiddleware.cs
git add -A
git commit -m "test: backend change"
git push origin main

# Expected: build-and-push should run
# Actual (before fix): build-and-push skipped
```

### Test 2: Frontend-only change
```bash
# Change frontend/src/App.tsx
git add -A
git commit -m "test: frontend change"
git push origin main

# Expected: build-and-push should skip OR only build frontend
# Verify: Uses existing backend images
```

### Test 3: Infrastructure change
```bash
# Change terraform/deploy/main.tf
git add -A
git commit -m "test: terraform change"
git push origin main

# Expected: deploy-infrastructure should run
# Verify: Terraform apply executes
```

### Test 4: Manual dispatch
```bash
gh workflow run ci-dev.yml --field force_full_build=true

# Expected: Everything builds regardless of changes
# This should always work (current workaround)
```

---

**End of Document**





