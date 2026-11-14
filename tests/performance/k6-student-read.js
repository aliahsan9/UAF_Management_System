import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';
const failures = new Rate('http_failures');
const latency = new Trend('student_read_latency', true);
export const options = {
  stages: [
    { duration: '30s', target: 5 },
    { duration: '1m', target: 20 },
    { duration: '30s', target: 50 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.02'],
    http_req_duration: ['p(95)<750'],
    student_read_latency: ['p(95)<750'],
    http_failures: ['rate<0.02'],
  },
};
export default function () {
  const base = __ENV.BASE_URL || 'https://localhost:7001';
  const token = __ENV.BEARER_TOKEN;
  const response = http.get(`${base}/api/v1/students?page=1&pageSize=20`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
    tags: { endpoint: 'students-list' },
  });
  latency.add(response.timings.duration);
  failures.add(response.status >= 500 || response.status === 0);
  check(response, {
    'responds without server error': (r) => r.status < 500,
    'authorized request returns success': (r) => !token || r.status === 200,
    'anonymous request is rejected': (r) => !!token || r.status === 401,
  });
  sleep(1);
}
