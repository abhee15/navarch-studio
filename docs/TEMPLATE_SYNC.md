# Syncing Updates from Sri-Template

This document explains how to pull updates from the `sri-template` repository into `sri-subscription` while preserving project-specific configurations.

## Setup (One-time)

Add sri-template as an upstream remote:

```bash
cd c:\Abhi\Projects\Sri\sri-subscription
git remote add template https://github.com/abhee15/sri-template.git
git fetch template
```

## Pulling Template Updates

When sri-template is updated and you want to pull changes:

### Option 1: Automated Sync (Recommended)

Use the automated sync script that protects your configs:

```powershell
.\scripts\sync-from-template.ps1
```

This script will:
1. Backup all project-specific config files
2. Fetch and merge from sri-template
3. Restore your protected configs automatically
4. Show you the merge status

### Option 2: Manual Sync

If you prefer manual control:

```bash
# 1. Fetch latest from template
git fetch template

# 2. Create a sync branch
git checkout -b sync-template-updates

# 3. Merge template changes (may have conflicts)
git merge template/main --allow-unrelated-histories

# 4. Resolve conflicts (keep sri-subscription-specific values)
# Common conflicts will be in:
#   - backend/sri-subscription.sln (keep yours)
#   - docker-compose.yml ports (keep yours: 3001, 5011-5013, 5433, 5051)
#   - terraform variables (keep project_name = "sri-subscription")
#   - README.md (keep yours)
#   - .env files (keep yours)
#   - vite.config.ts (keep port 3001 and proxy to 5012)

# 5. Test locally
docker-compose up

# 6. Merge to develop
git checkout develop
git merge sync-template-updates
git push origin develop
```

## Selective Cherry-Picking

If you only want specific features/fixes:

```bash
# 1. View template commits
git log template/main --oneline

# 2. Cherry-pick specific commits
git cherry-pick <commit-hash>

# 3. Resolve conflicts if any
git cherry-pick --continue
```

## Files to NEVER Sync from Template

When merging from sri-template, **always keep sri-subscription version** for:

### Critical Config Files (Will Break if Overwritten)
- `backend/sri-subscription.sln` - Project-specific solution file
- `docker-compose.yml` - Uses different ports (3001, 5011-5013, 5433, 5051)
- `docker-compose.override.yml` - Local development config
- `frontend/vite.config.ts` - Port 3001 + proxy to port 5012
- `backend/*/Properties/launchSettings.json` - Ports 5011, 5012, 5013

### Environment-Specific Files
- `terraform/setup/terraform.tfvars` - Project name = "sri-subscription"
- `terraform/deploy/environments/*.tfvars` - Environment configs
- `.env*` files - API URLs, client IDs
- `backend/*/appsettings.json` - Connection strings
- `backend/*/appsettings.Development.json` - Local DB config (sri_subscription_dev)

### Documentation
- `README.md` - Project-specific intro
- `docs/SETUP_SRI_SUBSCRIPTION.md` - Setup log
- `docs/MULTI_PROJECT_COSTS.md` - Cost analysis

## During Merge Conflicts

```bash
# For files above, always choose sri-subscription version:
git checkout --ours <file-path>

# Or to keep theirs (template):
git checkout --theirs <file-path>

# Common workflow:
git merge template/main
# Conflict on docker-compose.yml
git checkout --ours docker-compose.yml
git add docker-compose.yml
git commit
```

## Best Practices

1. **Regular syncs**: Pull template updates monthly to avoid large merge conflicts
2. **Test thoroughly**: Always test in local dev environment before deploying
3. **Document changes**: Keep CHANGELOG.md of template updates applied
4. **Branch strategy**: Always sync via feature branch, never directly to main
5. **Shared improvements**: If you make general improvements to sri-subscription, consider contributing them back to sri-template

## What to Sync

✅ **Safe to sync**:
- Bug fixes in backend services
- New features in Shared/ project
- Frontend component improvements
- GitHub Actions workflow improvements
- Terraform module enhancements
- Documentation updates
- Security patches

⚠️ **Requires careful merge**:
- docker-compose.yml (port conflicts)
- terraform variables (project names)
- appsettings.json (connection strings)
- Package versions (test compatibility)

❌ **Never sync** (keep sri-subscription version):
- .sln file names
- Project-specific documentation
- Project-specific configurations
- Environment-specific values
- Port configurations

## Troubleshooting

### Template remote not found
```bash
git remote add template https://github.com/abhee15/sri-template.git
```

### Merge conflicts
Use the automated script which handles this for you:
```powershell
.\scripts\sync-from-template.ps1
```

### Need to abort a merge
```bash
git merge --abort
```

### Want to see what changed in template
```bash
git log template/main --oneline --graph --since="1 month ago"
```

