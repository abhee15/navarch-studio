const https = require('https');

const LINEAR_API_KEY = 'lin_api_HsoVVTGEOnMZN8Rgs2TsPrfVFFP5ftawLR7XU4CO';

const query = `
  query {
    teams {
      nodes {
        id
        key
        name
      }
    }
  }
`;

const data = JSON.stringify({ query });

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
    const json = JSON.parse(body);
    if (json.data) {
      json.data.teams.nodes.forEach(t => {
        console.log(`Team: ${t.name} | Key: ${t.key} | UUID: ${t.id}`);
      });
    } else {
      console.error('Error:', json.errors);
    }
  });
});

req.on('error', (e) => console.error(e));
req.write(data);
req.end();

