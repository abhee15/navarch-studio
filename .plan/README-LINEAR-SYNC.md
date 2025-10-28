# Linear Issue Import & Cleanup

This script helps you import cleaned-up issues to Linear and archive old ones.

## Setup

### 1. Install Dependencies

```bash
cd scripts
npm install
```

### 2. Get Your Linear API Key

1. Go to https://linear.app/settings/api
2. Create a new Personal API key
3. Copy the key

### 3. Get Your Team ID

1. Go to Linear
2. Open any issue
3. Look at the URL: `https://linear.app/[WORKSPACE]/team/[TEAM-ID]/issue/...`
4. Copy the TEAM-ID (e.g., "NAV" for NavArch Dev)

Or run this to find it:

```bash
LINEAR_API_KEY=your_key node -e "const {LinearClient} = require('@linear/sdk'); new LinearClient({apiKey: process.env.LINEAR_API_KEY}).teams().then(teams => teams.nodes.forEach(t => console.log(t.name, '->', t.id)))"
```

### 4. Set Environment Variables

**PowerShell (Windows):**

```powershell
$env:LINEAR_API_KEY="lin_api_..."
$env:LINEAR_TEAM_ID="your-team-id"
```

**Bash (Mac/Linux):**

```bash
export LINEAR_API_KEY="lin_api_..."
export LINEAR_TEAM_ID="your-team-id"
```

Or create a `.env` file (add to .gitignore!):

```
LINEAR_API_KEY=lin_api_...
LINEAR_TEAM_ID=your-team-id
```

## Usage

### Import New Issues from CSV

```bash
node sync-linear.js import ../Linear_Import_Clean.csv
```

This will:

- ✅ Read the cleaned CSV file
- ✅ Create EPICs first (NAV-EPIC-1, etc.)
- ✅ Create Sprint milestones (NAV-100, NAV-200, etc.)
- ✅ Create individual stories (NAV-101+)
- ✅ Set up parent-child relationships
- ✅ Add labels (Phase 1, Backend, Frontend, etc.)
- ✅ Set priorities and estimates

### Archive Old Issues

**Archive specific issues:**

```bash
node sync-linear.js cleanup NAV-1,NAV-2,NAV-3,NAV-4,NAV-5
```

**Archive a range:**

```bash
node sync-linear.js range 1 67
```

This archives NAV-1 through NAV-67 (your old issues).

## Workflow

### Recommended Import Process

1. **Backup first** (optional but recommended):

   - Export your current Linear issues
   - Keep a backup just in case

2. **Import new structure**:

   ```bash
   cd scripts
   npm install
   node sync-linear.js import ../Linear_Import_Clean.csv
   ```

3. **Verify in Linear**:

   - Check that EPICs appear (NAV-EPIC-1 to NAV-EPIC-5)
   - Check Sprint milestones (NAV-100, NAV-200, NAV-300, NAV-400)
   - Verify parent-child relationships
   - Check labels are applied

4. **Archive old issues** (if import looks good):

   ```bash
   node sync-linear.js range 1 67
   ```

5. **Cleanup script itself** (optional):
   - Keep for future use, or
   - Remove scripts/node_modules after import

## What Gets Created

### EPICs (5 total)

- NAV-EPIC-1: Phase 1 - Hydrostatics MVP
- NAV-EPIC-2: Phase 2 - Resistance & Propulsion
- NAV-EPIC-3: Phase 3 - Seakeeping & 3D
- NAV-EPIC-4: Phase 4 - Advanced Stability
- NAV-EPIC-5: Phase 5 - Polish & Production

### Sprint 1 (14 stories)

- NAV-100: Sprint 1 milestone
- NAV-101 to NAV-113: Database, services, frontend foundation

### Sprint 2 (15 stories)

- NAV-200: Sprint 2 milestone
- NAV-201 to NAV-215: Core hydrostatic calculations

### Sprint 3 & 4 (Placeholders)

- NAV-300: Sprint 3 milestone (Curves & Viz)
- NAV-400: Sprint 4 milestone (Trim & Reports)

## Troubleshooting

### "LINEAR_API_KEY environment variable not set"

Make sure you exported the variable in your current shell session.

### "Team not found"

Double-check your LINEAR_TEAM_ID. Run the team list command from Setup step 3.

### "Rate limit exceeded"

The script includes 100ms delays between requests. If you still hit limits, increase the delay in the code.

### Issues created but parent relationships missing

Linear processes relationships asynchronously. Wait a few seconds and refresh.

### Labels not matching

The script creates labels if they don't exist. Check Linear settings > Labels to verify.

## Tips

- **Start small**: Import just Sprint 1 first to test
- **Use a test workspace**: If available, test in a sandbox first
- **Keep the CSV**: You can re-import after adjustments
- **Archive safely**: Archived issues can be restored in Linear

## After Import

Once imported, you can:

1. ✅ Adjust priorities in Linear UI
2. ✅ Assign team members
3. ✅ Add due dates
4. ✅ Create cycles/sprints
5. ✅ Move to active sprint
6. ✅ Start working! 🚀
