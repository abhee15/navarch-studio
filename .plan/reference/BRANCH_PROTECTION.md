# Branch Protection Rules

This document describes the recommended branch protection rules for the repository to ensure code quality, maintain deployment stability, and enforce review processes.

## 🎯 Overview

Branch protection rules prevent direct pushes to important branches, require code reviews, and ensure all quality checks pass before merging.

## 🔒 Main Branch Protection

The `main` branch represents production-ready code. All changes must go through pull requests with approval.

### Required Status Checks

✅ **Must pass before merging:**

- `frontend-checks` - Frontend linting, testing, type checking, and building
- `backend-checks` - Backend testing and code analysis (all 3 services)
- `terraform-checks` - Terraform formatting and validation
- `security-checks` - Security vulnerability scanning
- `docker-build-checks` - Docker image building and scanning

### Required Reviewers

- ✅ **At least 1 reviewer required**
- ✅ **Dismiss stale reviews** when new commits are pushed
- ✅ **Require review from code owners** (if CODEOWNERS file exists)
- ❌ **No self-approval** (enforce for administrators)

### Merge Requirements

- ✅ **Require branches to be up to date** before merging
- ✅ **Require linear history** (no merge commits, use squash or rebase)
- ✅ **Include administrators** in restrictions

### Additional Settings

- ✅ **Require signed commits** (optional, recommended for enterprises)
- ✅ **Require deployments to succeed** before merging (optional)
- ❌ **Allow force pushes:** Disabled
- ❌ **Allow deletions:** Disabled

## 🛠️ Develop Branch Protection

The `develop` branch is the integration branch for development. Slightly less strict than `main`.

### Required Status Checks

✅ **Must pass before merging:**

- `frontend-checks` - Frontend linting, testing, and building
- `backend-checks` - Backend testing and code analysis
- `terraform-checks` - Terraform validation

### Required Reviewers

- ✅ **At least 1 reviewer required**
- ✅ **Dismiss stale reviews** when new commits are pushed
- ⚠️ **Code owners optional** (can be skipped for faster iteration)

### Merge Requirements

- ✅ **Require branches to be up to date** before merging
- ⚠️ **Linear history recommended** but not required
- ✅ **Include administrators** in restrictions

### Additional Settings

- ❌ **Allow force pushes:** Disabled
- ❌ **Allow deletions:** Disabled

## 🌿 Feature Branches

Feature branches (`feature/*`) are unprotected by default, allowing developers flexibility. However, they must pass PR checks when merging into `develop` or `main`.

### Naming Convention

- `feature/phase1-local-stack`
- `feature/phase2-microservices`
- `feature/add-user-profile`
- `feature/fix-login-bug`

## 📋 How to Configure Branch Protection

### Using GitHub Web UI

#### Main Branch

1. Go to your GitHub repository
2. Click on **Settings** tab
3. Click on **Branches** in the left sidebar
4. Click on **Add rule** (or **Edit** if rule exists)
5. Enter branch name pattern: `main`
6. Configure the following:

   **Protect matching branches:**

   - ✅ Require a pull request before merging
     - ✅ Require approvals: **1**
     - ✅ Dismiss stale pull request approvals when new commits are pushed
     - ✅ Require review from Code Owners
   - ✅ Require status checks to pass before merging
     - ✅ Require branches to be up to date before merging
     - **Add status checks:**
       - `frontend-checks`
       - `backend-checks`
       - `terraform-checks`
       - `security-checks`
       - `docker-build-checks`
   - ✅ Require conversation resolution before merging
   - ✅ Require linear history
   - ❌ Allow force pushes
   - ❌ Allow deletions
   - ✅ Do not allow bypassing the above settings (include administrators)

7. Click **Create** or **Save changes**

#### Develop Branch

Repeat the above steps with branch name pattern: `develop`

- Same as main, but:
  - ⚠️ Remove `security-checks` and `docker-build-checks` (optional, for faster iteration)
  - ⚠️ Linear history is recommended but not required

### Using GitHub CLI

```bash
# Install GitHub CLI if not already installed
# https://cli.github.com/

# Main branch protection
gh api repos/:owner/:repo/branches/main/protection \
  --method PUT \
  --field required_status_checks='{"strict":true,"contexts":["frontend-checks","backend-checks","terraform-checks","security-checks","docker-build-checks"]}' \
  --field enforce_admins=true \
  --field required_pull_request_reviews='{"dismissal_restrictions":{},"dismiss_stale_reviews":true,"require_code_owner_reviews":true,"required_approving_review_count":1}' \
  --field restrictions=null \
  --field required_linear_history=true \
  --field allow_force_pushes=false \
  --field allow_deletions=false

# Develop branch protection
gh api repos/:owner/:repo/branches/develop/protection \
  --method PUT \
  --field required_status_checks='{"strict":true,"contexts":["frontend-checks","backend-checks","terraform-checks"]}' \
  --field enforce_admins=true \
  --field required_pull_request_reviews='{"dismissal_restrictions":{},"dismiss_stale_reviews":true,"require_code_owner_reviews":false,"required_approving_review_count":1}' \
  --field restrictions=null \
  --field allow_force_pushes=false \
  --field allow_deletions=false
```

### Using Terraform (Advanced)

```hcl
# github_branch_protection.tf
terraform {
  required_providers {
    github = {
      source  = "integrations/github"
      version = "~> 5.0"
    }
  }
}

provider "github" {
  token = var.github_token
  owner = var.github_owner
}

resource "github_branch_protection" "main" {
  repository_id = var.repository_name

  pattern          = "main"
  enforce_admins   = true
  allows_deletions = false
  allows_force_pushes = false
  require_signed_commits = false

  required_pull_request_reviews {
    dismiss_stale_reviews           = true
    require_code_owner_reviews      = true
    required_approving_review_count = 1
  }

  required_status_checks {
    strict = true
    contexts = [
      "frontend-checks",
      "backend-checks",
      "terraform-checks",
      "security-checks",
      "docker-build-checks"
    ]
  }
}

resource "github_branch_protection" "develop" {
  repository_id = var.repository_name

  pattern          = "develop"
  enforce_admins   = true
  allows_deletions = false
  allows_force_pushes = false

  required_pull_request_reviews {
    dismiss_stale_reviews           = true
    require_code_owner_reviews      = false
    required_approving_review_count = 1
  }

  required_status_checks {
    strict = true
    contexts = [
      "frontend-checks",
      "backend-checks",
      "terraform-checks"
    ]
  }
}
```

## 👥 Code Owners

Create a `.github/CODEOWNERS` file to specify who should review changes to specific files.

### Example CODEOWNERS file

```plaintext
# Global owners (review all changes)
* @your-github-username

# Frontend
frontend/ @frontend-team @your-github-username

# Backend services
backend/IdentityService/ @backend-team @auth-expert
backend/ApiGateway/ @backend-team @api-expert
backend/DataService/ @backend-team @data-expert

# Infrastructure
terraform/ @devops-team @infrastructure-lead
.github/workflows/ @devops-team

# Database
database/ @database-team @backend-team

# Documentation
*.md @documentation-team
docs/ @documentation-team

# Configuration files
*.yml @devops-team
*.yaml @devops-team
Dockerfile @devops-team
docker-compose.yml @devops-team
```

**See:** `.github/CODEOWNERS` in this repository

## 🚀 Deployment Workflows

### Dev Environment

- **Trigger:** Push to `develop` branch
- **Workflow:** `.github/workflows/ci-dev.yml`
- **Protection:** None (auto-deploys)

### Staging Environment

- **Trigger:** Push to `main` branch
- **Workflow:** `.github/workflows/ci-staging.yml`
- **Protection:** Recommended to add manual approval

### Production Environment

- **Trigger:** Manual workflow dispatch
- **Workflow:** `.github/workflows/ci-prod.yml`
- **Protection:** **Required reviewers and manual approval**

#### Configure Production Environment Protection

1. Go to **Settings** → **Environments**
2. Click on **production** (or create it)
3. Configure:
   - ✅ **Required reviewers:** Add trusted team members
   - ✅ **Wait timer:** 0 minutes (or set delay if needed)
   - ⚠️ **Deployment branches:** Only `main` or tags
4. Save

## ✅ Verification Checklist

After setting up branch protection:

- [ ] `main` branch protection is active
- [ ] `develop` branch protection is active
- [ ] Required status checks are configured
- [ ] At least 1 reviewer required for PRs
- [ ] Stale review dismissal enabled
- [ ] Linear history enforced (main) or recommended (develop)
- [ ] Force pushes disabled
- [ ] Deletions disabled
- [ ] Administrators included in restrictions
- [ ] CODEOWNERS file created
- [ ] Production environment requires manual approval

## 🧪 Testing Branch Protection

1. Create a feature branch: `git checkout -b feature/test-protection`
2. Make a small change and push
3. Try to merge directly to `main` → Should be blocked
4. Create a PR to `develop` → Should require checks to pass
5. Try to merge without approval → Should be blocked
6. Get approval and merge → Should succeed

## 🚨 Troubleshooting

### "Required status checks are failing"

**Cause:** One or more CI checks failed.

**Solution:** Fix the failing checks (linting, tests, build errors) and push again.

### "Review required"

**Cause:** Branch protection requires approval before merging.

**Solution:** Request review from a team member or code owner.

### "Branch is out of date"

**Cause:** Base branch (e.g., `main`) has new commits after PR was created.

**Solution:** Update your branch:

```bash
git checkout main
git pull
git checkout your-feature-branch
git rebase main
git push --force-with-lease
```

### "Cannot bypass branch protection"

**Cause:** Trying to force push or bypass checks.

**Solution:** This is intentional. Follow the proper PR and review process.

## 📚 Additional Resources

- [GitHub Branch Protection Rules](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/defining-the-mergeability-of-pull-requests/about-protected-branches)
- [GitHub Code Owners](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-code-owners)
- [GitHub Environments](https://docs.github.com/en/actions/deployment/targeting-different-environments/using-environments-for-deployment)

---

**Last Updated:** Phase 7 - CI/CD Pipeline Setup





