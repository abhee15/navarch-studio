# Update Linear Issue via CLI

## Quick Start

### 1. Get Your Linear API Key

1. Go to https://linear.app/settings/api
2. Click "Create new" under Personal API keys
3. Give it a name like "CLI Script"
4. Copy the generated API key

### 2. Set the API Key (Choose one method)

**Option A: Temporary (current session only)**

```powershell
$env:LINEAR_API_KEY = "lin_api_YOUR_KEY_HERE"
```

**Option B: Persistent (saved for all sessions)**

```powershell
[Environment]::SetEnvironmentVariable("LINEAR_API_KEY", "lin_api_YOUR_KEY_HERE", "User")
# Then restart PowerShell
```

### 3. Run the Update Script

```powershell
.\update-linear-issue.ps1
```

## What the Script Does

1. ✅ Finds the NAV-14 issue
2. ✅ Adds a detailed completion comment with all deliverables
3. ✅ Marks the issue as "Complete" (moves to Done state)

## Expected Output

```
Step 1: Finding issue NAV-14...
✓ Found issue: NAV-14 - Epic: Phase 1 Hydrostatics MVP
  Current state: In Progress

Step 2: Adding completion comment...
✓ Comment added successfully

Step 3: Marking issue as complete...
✓ Issue marked as complete!

🎉 Successfully updated NAV-14 to 'Done' state
   View at: https://linear.app/sri-abhishikth-mallepudi/issue/NAV-14
```

## Troubleshooting

**Error: "LINEAR_API_KEY environment variable not set"**

- Make sure you've set the API key using one of the methods in step 2
- If using Option B, restart PowerShell after setting it

**Error: "Could not find a 'completed' state"**

- Your Linear workspace might use a different state name
- The script will automatically find the first "completed" type state

**Error: "Request failed"**

- Check that your API key is correct
- Make sure you have permission to update issues in Linear

## Manual Alternative

If the script doesn't work, you can also:

1. Go to https://linear.app/sri-abhishikth-mallepudi/issue/NAV-14
2. Copy the completion comment from the script
3. Paste it as a comment
4. Change the status to "Done" manually
