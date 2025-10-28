# Deployment Workflow

## Simple Deployment Flow

This template uses a streamlined deployment process optimized for quick iteration:

```
Git Push (main) → Dev (automatic) → Staging (manual) → Prod (manual)
```

## Automatic Deployment (Dev)

Every push to the `main` branch automatically triggers a **Dev** deployment:

```bash
git add .
git commit -m "feat: Add new feature"
git push origin main  # ← Triggers Dev deployment
```

**What happens:**

1. ✅ Quality checks run FIRST (ESLint, TypeScript, dotnet format, tests)
   - If ANY check fails: workflow stops, nothing deploys (fail-fast)
   - If ALL pass: proceeds with build and deployment
2. ✅ Docker images built and pushed to ECR
3. ✅ Infrastructure deployed to Dev environment
4. ✅ Database migrations run
5. ✅ Frontend deployed to CloudFront
6. ✅ Smoke tests verify deployment

**Monitor progress:**

- Go to: `https://github.com/YOUR_USERNAME/YOUR_REPO/actions`
- Watch the "Deploy to Dev" workflow

## Manual Deployment (Staging)

When Dev is stable and you want to test in a staging environment:

1. Go to GitHub: **Actions** → **Deploy to Staging** → **Run workflow**
2. Click **Run workflow** on the `main` branch
3. Wait for deployment to complete (~10-15 minutes)

**Use staging for:**

- Final testing before production
- Client demos
- Integration testing with external services
- Performance testing

## Manual Deployment (Production)

When Staging is stable and you're ready to go live:

1. Go to GitHub: **Actions** → **Deploy to Production** → **Run workflow**
2. Enter a version tag (e.g., `v1.0.0`)
3. Click **Run workflow**
4. Approve the deployment (requires manual approval)
5. Wait for deployment to complete (~10-15 minutes)

**Production is special:**

- Requires manual approval for safety
- Uses larger instance sizes for better performance
- Enables Multi-AZ for high availability
- Has longer backup retention (30 days)

## Quick Reference

| Environment | Trigger                   | When to Use                   |
| ----------- | ------------------------- | ----------------------------- |
| **Dev**     | Push to `main`            | Every commit, rapid iteration |
| **Staging** | Manual trigger            | Pre-production testing, demos |
| **Prod**    | Manual trigger + approval | Production releases           |

## PR Checks

Every pull request automatically runs:

- ✅ TypeScript/React linting and tests
- ✅ .NET build and tests
- ✅ Terraform validation
- ✅ Security scanning

**Merge when:** All checks pass ✅

## Environment URLs

After deployment, find your URLs in the workflow logs:

**Dev:**

```
Frontend: https://xxx.cloudfront.net
API Gateway: https://xxx.execute-api.us-east-1.amazonaws.com
Identity Service: https://xxx.execute-api.us-east-1.amazonaws.com
Data Service: https://xxx.execute-api.us-east-1.amazonaws.com
```

**Staging/Prod:**

- Same pattern, different URLs
- Optionally configure custom domains

## Rollback

If something goes wrong:

### Option 1: Revert and Redeploy

```bash
git revert HEAD
git push origin main  # ← Triggers new Dev deployment
```

### Option 2: Destroy and Recreate

```bash
# Go to GitHub Actions → Destroy Environment
# Select environment (dev/staging/prod)
# Type "DESTROY" to confirm
```

⚠️ **Warning:** Destroying deletes all data in that environment!

## Cost Optimization

To minimize AWS costs during development:

1. **Use Dev environment only** - Skip staging if not needed
2. **Destroy when not in use:**
   ```bash
   # Destroy dev environment after work hours
   # Recreate next day (Phase 4 resources persist)
   ```
3. **Monitor AWS costs:**
   - Check AWS Budgets alerts
   - Review Cost Explorer for anomalies

## Troubleshooting

### Deployment fails with "Backend not found"

**Problem:** Phase 4 not completed  
**Solution:** See `docs/DEPLOYMENT_PREREQUISITES.md`

### Deployment succeeds but app doesn't work

**Problem:** Missing environment variables or secrets  
**Solution:** Verify all GitHub secrets are configured

### Docker image push fails

**Problem:** ECR repositories don't exist  
**Solution:** Run Phase 4 to create ECR repositories

### Terraform state lock error

**Problem:** Previous deployment didn't complete  
**Solution:**

```bash
# Wait 5-10 minutes for lock to expire, then retry
# OR manually release lock in DynamoDB table
```

## Best Practices

✅ **DO:**

- Push to `main` frequently for continuous deployment
- Test in Dev before promoting to Staging
- Use semantic commit messages (`feat:`, `fix:`, `chore:`)
- Monitor deployment logs for errors
- Run smoke tests after deployment

❌ **DON'T:**

- Push directly to Staging/Prod (use manual triggers)
- Deploy to Prod without testing in Dev first
- Skip PR checks by pushing with `--no-verify`
- Leave Dev environment running 24/7 if not needed
- Deploy on Fridays (unless necessary 😅)

## Need Help?

- **Prerequisites:** `docs/DEPLOYMENT_PREREQUISITES.md`
- **GitHub Secrets:** `docs/GITHUB_SECRETS.md`
- **Cost Optimization:** `.plan/COST_OPTIMIZATION.md`
- **AWS Setup:** `.plan/phase4-aws-setup.md`

---

**Happy Deploying! 🚀**





