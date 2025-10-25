const { LinearClient } = require('@linear/sdk');

const apiKey = process.env.LINEAR_API_KEY;
const teamId = process.env.LINEAR_TEAM_ID;

if (!apiKey) {
  console.error('❌ LINEAR_API_KEY not set');
  process.exit(1);
}

if (!teamId) {
  console.error('❌ LINEAR_TEAM_ID not set');
  process.exit(1);
}

console.log('🔍 Verifying Linear credentials...\n');

const client = new LinearClient({ apiKey });

async function verify() {
  try {
    // Test 1: Get viewer info
    const viewer = await client.viewer;
    console.log('✓ API Key Valid');
    console.log(`  User: ${viewer.name} (${viewer.email})`);
    
    // Test 2: List teams
    const teams = await client.teams();
    console.log('\n✓ Teams accessible:');
    teams.nodes.forEach(t => {
      const marker = t.key === teamId ? '→' : ' ';
      console.log(`  ${marker} ${t.name} (${t.key})`);
    });
    
    // Test 3: Verify specific team
    const team = teams.nodes.find(t => t.key === teamId);
    if (team) {
      console.log(`\n✓ Team "${teamId}" found: ${team.name}`);
      
      // Test 4: Check existing issues
      const issues = await client.issues({ 
        filter: { team: { key: { eq: teamId } } },
        first: 5
      });
      console.log(`\n✓ Connection successful!`);
      console.log(`  Found ${issues.nodes.length} recent issues in team ${teamId}`);
      
      if (issues.nodes.length > 0) {
        console.log('  Recent issues:');
        issues.nodes.forEach(issue => {
          console.log(`    - ${issue.identifier}: ${issue.title}`);
        });
      }
      
      console.log('\n✅ All checks passed! Ready to import stories.');
    } else {
      console.error(`\n❌ Team "${teamId}" not found`);
      console.log('Available teams:', teams.nodes.map(t => t.key).join(', '));
      process.exit(1);
    }
    
  } catch (error) {
    console.error('\n❌ Error:', error.message);
    if (error.message.includes('Authentication')) {
      console.error('   Check your LINEAR_API_KEY');
    }
    process.exit(1);
  }
}

verify();

