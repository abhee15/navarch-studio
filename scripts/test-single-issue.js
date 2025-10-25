#!/usr/bin/env node

/**
 * Test creating a single Linear issue
 */

const https = require('https');

const LINEAR_API_KEY = 'lin_api_HsoVVTGEOnMZN8Rgs2TsPrfVFFP5ftawLR7XU4CO';
const LINEAR_TEAM_ID = '92d8ebfb-4bd3-44e1-8b2e-52ec95fb420d'; // NAV team UUID

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
          console.log('Response:', JSON.stringify(json, null, 2));
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

async function getWorkflowStateId() {
  console.log('📍 Getting workflow state...\n');
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
  const backlogState = data.team.states.nodes.find(s => s.type === 'backlog');
  console.log(`✅ Found Backlog state: ${backlogState.id}\n`);
  return backlogState.id;
}

async function createTestIssue() {
  console.log('🧪 Creating test issue...\n');
  
  const stateId = await getWorkflowStateId();
  
  const mutation = `
    mutation($input: IssueCreateInput!) {
      issueCreate(input: $input) {
        success
        issue {
          id
          identifier
          title
          url
        }
      }
    }
  `;
  
  const input = {
    teamId: LINEAR_TEAM_ID,
    title: '[TEST] Phase 1: Hydrostatics MVP',
    description: 'Complete hydrostatics module with geometry input, computation engine, visualization, and reporting.\n\n**Deliverables:**\n- Vessel geometry CRUD\n- Core hydrostatic calculations\n- Curves generation\n- 3D visualization\n- PDF/Excel reports',
    priority: 1, // Urgent
    stateId: stateId,
    estimate: 40,
  };
  
  const data = await makeGraphQLRequest(mutation, { input });
  
  if (data.issueCreate.success) {
    const issue = data.issueCreate.issue;
    console.log(`\n✅ SUCCESS!`);
    console.log(`   ID: ${issue.identifier}`);
    console.log(`   Title: ${issue.title}`);
    console.log(`   URL: ${issue.url}`);
  } else {
    console.log('❌ Failed to create issue');
  }
}

createTestIssue().catch(error => {
  console.error('\n❌ Error:', error.message);
  process.exit(1);
});

