#!/usr/bin/env node

/**
 * Linear Issue Import using GraphQL directly (more reliable)
 */

const fs = require('fs');
const csv = require('csv-parser');
const https = require('https');

const LINEAR_API_KEY = process.env.LINEAR_API_KEY || 'lin_api_HsoVVTGEOnMZN8Rgs2TsPrfVFFP5ftawLR7XU4CO';
const LINEAR_TEAM_ID = process.env.LINEAR_TEAM_ID || '92d8ebfb-4bd3-44e1-8b2e-52ec95fb420d'; // NAV team UUID
const LINEAR_USER_ID = '28393907-40ba-4ba9-bd04-d240ac0de527'; // abhee15@gmail.com

function delay(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

function makeGraphQLRequest(query, variables) {
  return new Promise((resolve, reject) => {
    const data = JSON.stringify({ query, variables });
    
    const options = {
      hostname: 'api.linear.app',
      port: 443,
      path: '/graphql',
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': LINEAR_API_KEY,
        'Content-Length': data.length
      }
    };
    
    const req = https.request(options, (res) => {
      let body = '';
      res.on('data', (chunk) => body += chunk);
      res.on('end', () => {
        try {
          const json = JSON.parse(body);
          if (json.errors) {
            reject(new Error(json.errors[0].message));
          } else {
            resolve(json.data);
          }
        } catch (e) {
          reject(e);
        }
      });
    });
    
    req.on('error', reject);
    req.write(data);
    req.end();
  });
}

async function getWorkflowStateId(stateName) {
  const query = `
    query($teamId: String!) {
      team(id: $teamId) {
        states {
          nodes {
            id
            name
            type
          }
        }
      }
    }
  `;
  
  const data = await makeGraphQLRequest(query, { teamId: LINEAR_TEAM_ID });
  const states = data.team.states.nodes;
  
  // Map status names
  const stateMap = {
    'Backlog': states.find(s => s.type === 'backlog' || s.name === 'Backlog'),
    'Todo': states.find(s => s.type === 'unstarted' || s.name === 'Todo'),
    'In Progress': states.find(s => s.type === 'started' || s.name === 'In Progress'),
    'Done': states.find(s => s.type === 'completed' || s.name === 'Done'),
  };
  
  return (stateMap[stateName] || stateMap['Backlog']).id;
}

async function createIssue(title, description, priority, estimate, stateId, parentId = null) {
  const mutation = `
    mutation($input: IssueCreateInput!) {
      issueCreate(input: $input) {
        success
        issue {
          id
          identifier
          title
        }
      }
    }
  `;
  
  const priorityMap = {
    'No priority': 0,
    'Urgent': 1,
    'High': 2,
    'Medium': 3,
    'Low': 4,
  };
  
  const input = {
    teamId: LINEAR_TEAM_ID,
    title: title,
    description: description,
    priority: priorityMap[priority] || 0,
    stateId: stateId,
  };
  
  // Add optional fields
  if (estimate && !isNaN(parseInt(estimate))) {
    input.estimate = parseInt(estimate);
  }
  
  if (parentId) {
    input.parentId = parentId;
  }
  
  // Don't set assigneeId initially - causes membership errors
  // Can be assigned in Linear UI after import
  
  const data = await makeGraphQLRequest(mutation, { input });
  return data.issueCreate.issue;
}

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

async function importIssues(csvFilePath) {
  console.log('📥 Starting Linear import from:', csvFilePath);
  console.log('');
  
  const issues = await parseCSV(csvFilePath);
  console.log(`Found ${issues.length} issues to import`);
  console.log('');
  
  // Get default state ID
  const stateId = await getWorkflowStateId('Backlog');
  console.log(`Using state ID: ${stateId}`);
  console.log('');
  
  // Track created issues by their CSV ID for parent relationship
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
    try {
      // Skip empty rows
      if (!issueData.Title) {
        console.log(`⚠️  Skipping row with no title`);
        continue;
      }
      
      // Determine parent issue ID if specified
      let parentIssueId = null;
      if (issueData['Parent issue']) {
        parentIssueId = issueMap.get(issueData['Parent issue']);
        if (!parentIssueId) {
          console.log(`⚠️  Warning: Parent issue ${issueData['Parent issue']} not found yet for ${issueData.ID}`);
        }
      }
      
      const issue = await createIssue(
        issueData.Title,
        issueData.Description || '',
        issueData.Priority || 'No priority',
        issueData.Estimate,
        stateId,
        parentIssueId
      );
      
      if (issue) {
        issueMap.set(issueData.ID, issue.id);
        created++;
        
        const emoji = issueData.ID.includes('EPIC') ? '🎯' : 
                      issueData.ID.match(/NAV-\d00$/) ? '📦' : '✅';
        console.log(`${emoji} Created: ${issue.identifier} - ${issueData.Title.substring(0, 60)}...`);
      }
      
      // Rate limiting: wait 2 seconds between requests to avoid hitting limits
      await delay(2000);
    } catch (error) {
      console.error(`❌ Error creating "${issueData.Title?.substring(0, 40)}...":`, error.message);
      failed++;
    }
  }
  
  console.log('');
  console.log('════════════════════════════════════════');
  console.log(`✅ Import complete!`);
  console.log(`   Created: ${created} issues`);
  console.log(`   Failed:  ${failed} issues`);
  console.log('════════════════════════════════════════');
}

// Main
const csvFile = process.argv[2] || '../Linear_Import_Clean.csv';

if (!fs.existsSync(csvFile)) {
  console.error(`❌ Error: File not found: ${csvFile}`);
  process.exit(1);
}

importIssues(csvFile).catch(error => {
  console.error('❌ Fatal error:', error);
  process.exit(1);
});

