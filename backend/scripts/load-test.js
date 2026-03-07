// k6 load test script for MoneyTracker API critical endpoints.
// Run with: k6 run backend/scripts/load-test.js
//
// Environment variables:
//   BASE_URL - API base URL (default: http://localhost:5000)

import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export const options = {
  scenarios: {
    health: {
      executor: 'constant-vus',
      exec: 'healthCheck',
      vus: 5,
      duration: '30s',
      tags: { scenario: 'health' },
    },
    dashboard: {
      executor: 'ramping-vus',
      exec: 'dashboard',
      startVUs: 1,
      stages: [
        { duration: '10s', target: 10 },
        { duration: '20s', target: 10 },
        { duration: '10s', target: 0 },
      ],
      tags: { scenario: 'dashboard' },
    },
    transactions: {
      executor: 'ramping-vus',
      exec: 'transactions',
      startVUs: 1,
      stages: [
        { duration: '10s', target: 15 },
        { duration: '20s', target: 15 },
        { duration: '10s', target: 0 },
      ],
      tags: { scenario: 'transactions' },
    },
    insights: {
      executor: 'ramping-vus',
      exec: 'insights',
      startVUs: 1,
      stages: [
        { duration: '10s', target: 5 },
        { duration: '20s', target: 5 },
        { duration: '10s', target: 0 },
      ],
      tags: { scenario: 'insights' },
    },
  },
  thresholds: {
    'http_req_duration{scenario:health}': ['p(95)<200'],
    'http_req_duration{scenario:dashboard}': ['p(95)<500'],
    'http_req_duration{scenario:transactions}': ['p(95)<300'],
    'http_req_duration{scenario:insights}': ['p(95)<500'],
    http_req_failed: ['rate<0.05'],
  },
};

export function healthCheck() {
  const res = http.get(`${BASE_URL}/health`);
  check(res, {
    'health status is 200': (r) => r.status === 200,
    'health response contains ok': (r) => r.json().status === 'ok',
  });
  sleep(0.5);
}

export function dashboard() {
  const householdId = '00000000-0000-0000-0000-000000000001';
  const res = http.get(`${BASE_URL}/dashboard/${householdId}`, {
    headers: { 'Content-Type': 'application/json' },
  });
  check(res, {
    'dashboard status is 200 or 401': (r) => r.status === 200 || r.status === 401,
  });
  sleep(1);
}

export function transactions() {
  const householdId = '00000000-0000-0000-0000-000000000001';
  const res = http.get(`${BASE_URL}/households/${householdId}/transactions`, {
    headers: { 'Content-Type': 'application/json' },
  });
  check(res, {
    'transactions status is 200 or 401': (r) => r.status === 200 || r.status === 401,
  });
  sleep(1);
}

export function insights() {
  const householdId = '00000000-0000-0000-0000-000000000001';
  const res = http.get(`${BASE_URL}/households/${householdId}/insights`, {
    headers: { 'Content-Type': 'application/json' },
  });
  check(res, {
    'insights status is 200 or 401': (r) => r.status === 200 || r.status === 401,
  });
  sleep(1);
}
