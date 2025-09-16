import fetch from 'node-fetch';
import nock from 'nock';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

function loadMockResponse() {
  const mockFile = path.join(__dirname, '..', 'shared', 'mock_responses.json');
  return JSON.parse(fs.readFileSync(mockFile, 'utf8'));
}

describe('Macys Node.js Scraper Tests', () => {
  afterEach(() => {
    nock.cleanAll();
  });

  test('payload structure', () => {
    const body = {
      'source': 'universal',
      'url': 'https://www.macys.com/shop/sale?id=3536&trackingid=407x733169&m_sc=sem&m_sb=google&m_tp=trademark&m_ac=google_trademark_international&m_ag=macy%27score_exact&m_cn=ggl_trademark_intl_lithuania_exact&m_pi=go_cmp-94807774_adg-154238318312_ad-674544461209_kwd-252677959_dev-c_ext-102882313401_prd-&gad_source=1&gclid=cj0kcqiayewrbhddarisagp1mwsg3z6ogoqrztdycjyqio5togc316ldkwuqkkbhrmiv4i_ho0gcjlkaagl6ealw_wcb'
    };

    expect(body.source).toBe('universal');
    expect(body.url).toContain('macys.com');
    expect(Object.keys(body)).toHaveLength(2);
  });

  test('authentication setup', async () => {
    const mockResponse = loadMockResponse();
    const username = 'test_user';
    const password = 'test_pass';

    const scope = nock('https://realtime.oxylabs.io')
      .post('/v1/queries')
      .matchHeader('authorization', (val) => {
        const expectedAuth = 'Basic ' + Buffer.from(`${username}:${password}`).toString('base64');
        return val === expectedAuth;
      })
      .reply(200, mockResponse);

    const body = {
      'source': 'universal',
      'url': 'https://www.macys.com/shop/sale'
    };

    const response = await fetch('https://realtime.oxylabs.io/v1/queries', {
      method: 'post',
      body: JSON.stringify(body),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Basic ' + Buffer.from(`${username}:${password}`).toString('base64'),
      }
    });

    expect(response.status).toBe(200);
    expect(scope.isDone()).toBe(true);
  });

  test('HTTP client configuration', async () => {
    const mockResponse = loadMockResponse();

    const scope = nock('https://realtime.oxylabs.io')
      .post('/v1/queries')
      .matchHeader('content-type', 'application/json')
      .reply(200, function(uri, requestBody) {
        const parsedBody = typeof requestBody === 'string' ? JSON.parse(requestBody) : requestBody;
        expect(parsedBody.source).toBe('universal');
        expect(parsedBody.url).toContain('macys.com');
        return mockResponse;
      });

    const body = {
      'source': 'universal',
      'url': 'https://www.macys.com/shop/sale'
    };

    const response = await fetch('https://realtime.oxylabs.io/v1/queries', {
      method: 'post',
      body: JSON.stringify(body),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Basic ' + Buffer.from('user:pass1').toString('base64'),
      }
    });

    expect(response.status).toBe(200);
    expect(scope.isDone()).toBe(true);
  });

  test('API response structure', async () => {
    const mockResponse = loadMockResponse();

    nock('https://realtime.oxylabs.io')
      .post('/v1/queries')
      .reply(200, mockResponse);

    const body = {
      'source': 'universal',
      'url': 'https://www.macys.com/shop/sale'
    };

    const response = await fetch('https://realtime.oxylabs.io/v1/queries', {
      method: 'post',
      body: JSON.stringify(body),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Basic ' + Buffer.from('user:pass1').toString('base64'),
      }
    });

    const data = await response.json();

    expect(data).toHaveProperty('results');
    expect(Array.isArray(data.results)).toBe(true);
    expect(data.results.length).toBeGreaterThan(0);

    const firstResult = data.results[0];
    expect(firstResult).toHaveProperty('content');
    expect(firstResult).toHaveProperty('job_id');
    expect(firstResult).toHaveProperty('status_code');
    expect(firstResult.status_code).toBe(200);
  });
});
