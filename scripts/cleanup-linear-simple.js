#!/usr/bin/env node

/**
 * Simple Linear cleanup script with better rate limiting
 */

const { LinearClient } = require('@linear/sdk');

const LINEAR_API_KEY = process.env.LINEAR_API_KEY || 'lin_api_HsoVVTGEOnMZN8Rgs2TsPrfVFFP5ftawLR7XU4CO';
const LINEAR_TEAM_ID = process.env.LINEAR_TEAM_ID || 'NAV';

const client = new LinearClient({ apiKey: LINEAR_API_KEY });

async function delay(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

async function archiveOldIssues() {
  console.log('🧹 Archiving NAV-1 through NAV-67...\n');
  
  const team = await client.team(LINEAR_TEAM_ID);
  const allIssues = await team.issues();
  
  let archived = 0;
  let notFound = 0;
  
  // Find issues 1-67
  for (let i = 1; i <= 67; i++) {
    try {
      const issue = allIssues.nodes.find(iss => iss.number === i);
      
      if (!issue) {
        console.log(`⚠️  NAV-${i} not found (may already be archived)`);
        notFound++;
        continue;
      }
      
      // Archive it
      await client.updateIssue(issue.id, {
        archivedAt: new Date().toISOString(),
      });
      
      archived++;
      console.log(`🗑️  Archived: NAV-${i} - ${issue.title.substring(0, 60)}...`);
      
      // Wait 500ms between requests to avoid rate limiting
      await delay(500);
    } catch (error) {
      console.error(`❌ Error archiving NAV-${i}:`, error.message);
    }
  }
  
  console.log('\n════════════════════════════════════════');
  console.log(`✅ Cleanup complete!`);
  console.log(`   Archived:  ${archived} issues`);
  console.log(`   Not found: ${notFound} issues`);
  console.log('════════════════════════════════════════');
}

archiveOldIssues().catch(console.error);

