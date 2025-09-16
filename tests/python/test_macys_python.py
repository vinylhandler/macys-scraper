import pytest
import requests
import requests_mock
import json
import os
from pathlib import Path


def load_mock_response():
    mock_file = Path(__file__).parent.parent / "shared" / "mock_responses.json"
    with open(mock_file, 'r') as f:
        return json.load(f)


class TestMacysPython:
    
    def test_payload_structure(self):
        payload = {
            'source': 'universal',
            'url': 'https://www.macys.com/shop/sale?id=3536&trackingid=407x733169&m_sc=sem&m_sb=google&m_tp=trademark&m_ac=google_trademark_international&m_ag=macy%27score_exact&m_cn=ggl_trademark_intl_lithuania_exact&m_pi=go_cmp-94807774_adg-154238318312_ad-674544461209_kwd-252677959_dev-c_ext-102882313401_prd-&gad_source=1&gclid=cj0kcqiayewrbhddarisagp1mwsg3z6ogoqrztdycjyqio5togc316ldkwuqkkbhrmiv4i_ho0gcjlkaagl6ealw_wcb'
        }
        
        assert payload['source'] == 'universal'
        assert 'macys.com' in payload['url']
        assert isinstance(payload, dict)
        assert len(payload) == 2

    def test_authentication_setup(self):
        username = 'test_user'
        password = 'test_pass'
        
        with requests_mock.Mocker() as m:
            mock_response = load_mock_response()
            m.post('https://realtime.oxylabs.io/v1/queries', json=mock_response)
            
            payload = {
                'source': 'universal',
                'url': 'https://www.macys.com/shop/sale'
            }
            
            response = requests.request(
                'POST',
                'https://realtime.oxylabs.io/v1/queries',
                auth=(username, password),
                json=payload,
            )
            
            assert response.status_code == 200
            assert m.last_request.headers.get('Authorization') is not None
            assert 'Basic' in m.last_request.headers.get('Authorization')

    def test_http_client_configuration(self):
        with requests_mock.Mocker() as m:
            mock_response = load_mock_response()
            m.post('https://realtime.oxylabs.io/v1/queries', json=mock_response)
            
            payload = {
                'source': 'universal',
                'url': 'https://www.macys.com/shop/sale'
            }
            
            response = requests.request(
                'POST',
                'https://realtime.oxylabs.io/v1/queries',
                auth=('user', 'pass1'),
                json=payload,
            )
            
            assert response.status_code == 200
            assert m.last_request.method == 'POST'
            assert m.last_request.url == 'https://realtime.oxylabs.io/v1/queries'
            assert 'application/json' in m.last_request.headers.get('Content-Type', '')
            
            request_data = json.loads(m.last_request.text)
            assert request_data['source'] == 'universal'
            assert 'macys.com' in request_data['url']

    def test_api_response_structure(self):
        with requests_mock.Mocker() as m:
            mock_response = load_mock_response()
            m.post('https://realtime.oxylabs.io/v1/queries', json=mock_response)
            
            payload = {
                'source': 'universal',
                'url': 'https://www.macys.com/shop/sale'
            }
            
            response = requests.request(
                'POST',
                'https://realtime.oxylabs.io/v1/queries',
                auth=('user', 'pass1'),
                json=payload,
            )
            
            data = response.json()
            assert 'results' in data
            assert len(data['results']) > 0
            
            result = data['results'][0]
            assert 'content' in result
            assert 'job_id' in result
            assert 'status_code' in result
            assert result['status_code'] == 200
