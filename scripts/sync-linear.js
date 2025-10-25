#!/usr/bin/env node

/**
 * Linear Issue Import & Cleanup Script
 * 
 * Prerequisites:
 * 1. npm install @linear/sdk csv-parser
 * 2. Set LINEAR_API_KEY environment variable
 * 3. Set LINEAR_TEAM_ID environment variable
 * 
 * Usage:
 *   node scripts/sync-linear.js import Linear_Import_Clean.csv
 *   node scripts/sync-linear.js cleanup NAV-1,NAV-2,NAV-3
 */

const { LinearClient } = require('@linear/sdk');
const fs = require('fs');
const path = require('path');
const csv = require('csv-parser');

// Configuration
const LINEAR_API_KEY = process.env.LINEAR_API_KEY;
const LINEAR_TEAM_ID = process.env.LINEAR_TEAM_ID;
const LINEAR_USER_ID = process.env.LINEAR_USER_ID || '28393907-40ba-4ba9-bd04-d240ac0de527'; // Default to abhee15@gmail.com

if (!LINEAR_API_KEY) {
  console.error('❌ Error: LINEAR_API_KEY environment variable not set');
  console.error('Get your API key from: https://linear.app/settings/api');
  process.exit(1);
}

if (!LINEAR_TEAM_ID) {
  console.error('❌ Error: LINEAR_TEAM_ID environment variable not set');
  console.error('Find your team ID in Linear settings');
  process.exit(1);
}

const client = new LinearClient({ apiKey: LINEAR_API_KEY });

/**
 * Parse CSV file and return array of issues
 */
async function parseCSV(filePath) {
  return new Promise((resolve, reject) => {
    const results = [];
    fs.createReadStream(filePath)
      .pipe(csv())
      .on('data', (data) => results.push(data))
      .on('end', () => resolve(results))
      .on('error', (error) => reject(error));
  });
}

/**
 * Map priority string to Linear priority number
 */
function mapPriority(priority) {
  const priorityMap = {
    'No priority': 0,
    'Urgent': 1,
    'High': 2,
    'Medium': 3,
    'Low': 4,
  };
  return priorityMap[priority] || 0;
}

/**
 * Map status string to Linear state
 */
async function getStateId(status) {
  const team = await client.team(LINEAR_TEAM_ID);
  const states = await team.states();
  
  const stateMap = {
    'Backlog': states.nodes.find(s => s.name === 'Backlog' || s.type === 'backlog'),
    'Todo': states.nodes.find(s => s.name === 'Todo' || s.type === 'unstarted'),
    'In Progress': states.nodes.find(s => s.name === 'In Progress' || s.type === 'started'),
    'Done': states.nodes.find(s => s.name === 'Done' || s.type === 'completed'),
  };
  
  return stateMap[status]?.id || stateMap['Backlog']?.id;
}

/**
 * Create or update issue in Linear
 */
async function createIssue(issueData, parentIssueId = null) {
  try {
    const stateId = await getStateId(issueData.Status);
    const priority = mapPriority(issueData.Priority);
    
    // Parse labels - skip automatic label creation/assignment
    // Labels can be added manually in Linear UI after import
    const labelIds = [];
    
    const issueInput = {
      teamId: LINEAR_TEAM_ID,
      title: issueData.Title,
      description: issueData.Description,
      priority: priority,
      stateId: stateId,
      assigneeId: LINEAR_USER_ID, // Assign to user
    };
    
    // Add estimate if provided
    if (issueData.Estimate && !isNaN(parseInt(issueData.Estimate))) {
      issueInput.estimate = parseInt(issueData.Estimate);
    }
    
    // Add parent if provided
    if (parentIssueId) {
      issueInput.parentId = parentIssueId;
    }
    
    const issueResponse = await client.createIssue(issueInput);
    const issue = await issueResponse.issue;
    
    return issue;
  } catch (error) {
    console.error(`❌ Error creating issue "${issueData.Title}":`, error.message);
    return null;
  }
}

/**
 * Import issues from CSV
 */
async function importIssues(csvFilePath) {
  console.log('📥 Starting Linear import from:', csvFilePath);
  console.log('');
  
  const issues = await parseCSV(csvFilePath);
  console.log(`Found ${issues.length} issues to import`);
  console.log('');
  
  // Track created issues by their ID for parent relationship
  const issueMap = new Map();
  
  // Sort issues so EPICs and milestones are created first
  const sortedIssues = issues.sort((a, b) => {
    const aId = a.ID || '';
    const bId = b.ID || '';
    const aIsEpic = aId.includes('EPIC');
    const bIsEpic = bId.includes('EPIC');
    const aIsMilestone = /NAV-\d00$/.test(aId);
    const bIsMilestone = /NAV-\d00$/.test(bId);
    
    if (aIsEpic && !bIsEpic) return -1;
    if (!aIsEpic && bIsEpic) return 1;
    if (aIsMilestone && !bIsMilestone) return -1;
    if (!aIsMilestone && bIsMilestone) return 1;
    return 0;
  });
  
  let created = 0;
  let failed = 0;
  
  for (const issueData of sortedIssues) {
    // Determine parent issue ID if specified
    let parentIssueId = null;
    if (issueData['Parent issue']) {
      parentIssueId = issueMap.get(issueData['Parent issue']);
      if (!parentIssueId) {
        console.log(`⚠️  Warning: Parent issue ${issueData['Parent issue']} not found yet for ${issueData.ID}`);
      }
    }
    
    const issue = await createIssue(issueData, parentIssueId);
    
    if (issue) {
      issueMap.set(issueData.ID, issue.id);
      created++;
      
      const emoji = issueData.ID.includes('EPIC') ? '🎯' : 
                    issueData.ID.match(/NAV-\d00$/) ? '📦' : '✅';
      console.log(`${emoji} Created: ${issueData.ID} - ${issueData.Title}`);
    } else {
      failed++;
    }
    
    // Rate limiting: wait 100ms between requests
    await new Promise(resolve => setTimeout(resolve, 100));
  }
  
  console.log('');
  console.log('════════════════════════════════════════');
  console.log(`✅ Import complete!`);
  console.log(`   Created: ${created} issues`);
  console.log(`   Failed:  ${failed} issues`);
  console.log('════════════════════════════════════════');
}

/**
 * Archive old issues by identifier
 */
async function cleanupIssues(issueIdentifiers) {
  console.log('🧹 Starting cleanup of old issues...');
  console.log('');
  
  const identifiers = issueIdentifiers.split(',').map(id => id.trim());
  
  let archived = 0;
  let notFound = 0;
  
  for (const identifier of identifiers) {
    try {
      // Search for issue by identifier
      const issues = await client.issues({
        filter: {
          team: { id: { eq: LINEAR_TEAM_ID } },
          number: { eq: parseInt(identifier.replace(/[^\d]/g, '')) }
        }
      });
      
      if (issues.nodes.length === 0) {
        console.log(`⚠️  Issue ${identifier} not found`);
        notFound++;
        continue;
      }
      
      const issue = issues.nodes[0];
      
      // Archive the issue
      await client.updateIssue(issue.id, {
        archivedAt: new Date().toISOString(),
      });
      
      archived++;
      console.log(`🗑️  Archived: ${identifier} - ${issue.title}`);
      
      // Rate limiting
      await new Promise(resolve => setTimeout(resolve, 100));
    } catch (error) {
      console.error(`❌ Error archiving ${identifier}:`, error.message);
    }
  }
  
  console.log('');
  console.log('════════════════════════════════════════');
  console.log(`✅ Cleanup complete!`);
  console.log(`   Archived:  ${archived} issues`);
  console.log(`   Not found: ${notFound} issues`);
  console.log('════════════════════════════════════════');
}

/**
 * Archive a range of issues
 */
async function cleanupRange(start, end) {
  const identifiers = [];
  for (let i = start; i <= end; i++) {
    identifiers.push(`NAV-${i}`);
  }
  await cleanupIssues(identifiers.join(','));
}

/**
 * Main CLI handler
 */
async function main() {
  const args = process.argv.slice(2);
  const command = args[0];
  
  if (!command) {
    console.log('Usage:');
    console.log('  Import:  node scripts/sync-linear.js import <csv-file>');
    console.log('  Cleanup: node scripts/sync-linear.js cleanup <issue-ids>');
    console.log('  Range:   node scripts/sync-linear.js range <start> <end>');
    console.log('');
    console.log('Examples:');
    console.log('  node scripts/sync-linear.js import Linear_Import_Clean.csv');
    console.log('  node scripts/sync-linear.js cleanup NAV-1,NAV-2,NAV-3');
    console.log('  node scripts/sync-linear.js range 1 67');
    process.exit(0);
  }
  
  try {
    if (command === 'import') {
      const csvFile = args[1];
      if (!csvFile) {
        console.error('❌ Error: Please provide CSV file path');
        process.exit(1);
      }
      if (!fs.existsSync(csvFile)) {
        console.error(`❌ Error: File not found: ${csvFile}`);
        process.exit(1);
      }
      await importIssues(csvFile);
    } else if (command === 'cleanup') {
      const issueIds = args[1];
      if (!issueIds) {
        console.error('❌ Error: Please provide issue IDs (comma-separated)');
        process.exit(1);
      }
      await cleanupIssues(issueIds);
    } else if (command === 'range') {
      const start = parseInt(args[1]);
      const end = parseInt(args[2]);
      if (isNaN(start) || isNaN(end)) {
        console.error('❌ Error: Please provide valid start and end numbers');
        process.exit(1);
      }
      await cleanupRange(start, end);
    } else {
      console.error(`❌ Error: Unknown command: ${command}`);
      process.exit(1);
    }
  } catch (error) {
    console.error('❌ Fatal error:', error);
    process.exit(1);
  }
}

// Run if called directly
if (require.main === module) {
  main();
}

module.exports = { importIssues, cleanupIssues };

