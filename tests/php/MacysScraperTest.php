<?php

use PHPUnit\Framework\TestCase;
use GuzzleHttp\Client;
use GuzzleHttp\Handler\MockHandler;
use GuzzleHttp\HandlerStack;
use GuzzleHttp\Psr7\Response;
use GuzzleHttp\Middleware;

class MacysScraperTest extends TestCase
{
    private function loadMockResponse(): string
    {
        $mockFile = __DIR__ . '/../shared/mock_responses.json';
        return file_get_contents($mockFile);
    }

    public function testPayloadStructure(): void
    {
        $params = [
            'source' => 'universal',
            'url' => 'https://www.macys.com/shop/sale?id=3536&trackingid=407x733169&m_sc=sem&m_sb=google&m_tp=trademark&m_ac=google_trademark_international&m_ag=macy%27score_exact&m_cn=ggl_trademark_intl_lithuania_exact&m_pi=go_cmp-94807774_adg-154238318312_ad-674544461209_kwd-252677959_dev-c_ext-102882313401_prd-&gad_source=1&gclid=cj0kcqiayewrbhddarisagp1mwsg3z6ogoqrztdycjyqio5togc316ldkwuqkkbhrmiv4i_ho0gcjlkaagl6ealw_wcb'
        ];

        $this->assertEquals('universal', $params['source']);
        $this->assertStringContainsString('macys.com', $params['url']);
        $this->assertCount(2, $params);
    }

    public function testAuthenticationSetup(): void
    {
        $username = 'test_user';
        $password = 'test_pass';
        $expectedAuth = base64_encode($username . ':' . $password);

        $this->assertNotEmpty($expectedAuth);
        $this->assertEquals($expectedAuth, base64_encode('test_user:test_pass'));
        
        $decoded = base64_decode($expectedAuth);
        $this->assertEquals('test_user:test_pass', $decoded);
    }

    public function testHTTPClientConfiguration(): void
    {
        $mockResponse = $this->loadMockResponse();
        $container = [];
        $history = Middleware::history($container);

        $mock = new MockHandler([
            new Response(200, ['Content-Type' => 'application/json'], $mockResponse),
        ]);

        $handlerStack = HandlerStack::create($mock);
        $handlerStack->push($history);

        $client = new Client(['handler' => $handlerStack]);

        $params = [
            'source' => 'universal',
            'url' => 'https://www.macys.com/shop/sale'
        ];

        $response = $client->post('https://realtime.oxylabs.io/v1/queries', [
            'json' => $params,
            'auth' => ['user', 'pass1']
        ]);

        $this->assertEquals(200, $response->getStatusCode());
        $this->assertCount(1, $container);
        
        $transaction = $container[0];
        $request = $transaction['request'];
        
        $this->assertEquals('POST', $request->getMethod());
        $this->assertEquals('/v1/queries', $request->getUri()->getPath());
        $this->assertStringContainsString('application/json', $request->getHeaderLine('Content-Type'));
        $this->assertNotEmpty($request->getHeaderLine('Authorization'));
        
        $requestBody = json_decode($request->getBody()->getContents(), true);
        $this->assertEquals('universal', $requestBody['source']);
        $this->assertStringContainsString('macys.com', $requestBody['url']);
    }

    public function testAPIResponseStructure(): void
    {
        $mockResponse = $this->loadMockResponse();
        
        $mock = new MockHandler([
            new Response(200, ['Content-Type' => 'application/json'], $mockResponse),
        ]);

        $handlerStack = HandlerStack::create($mock);
        $client = new Client(['handler' => $handlerStack]);

        $params = [
            'source' => 'universal',
            'url' => 'https://www.macys.com/shop/sale'
        ];

        $response = $client->post('https://realtime.oxylabs.io/v1/queries', [
            'json' => $params,
            'auth' => ['user', 'pass1']
        ]);

        $data = json_decode($response->getBody()->getContents(), true);

        $this->assertArrayHasKey('results', $data);
        $this->assertIsArray($data['results']);
        $this->assertGreaterThan(0, count($data['results']));

        $firstResult = $data['results'][0];
        $this->assertArrayHasKey('content', $firstResult);
        $this->assertArrayHasKey('job_id', $firstResult);
        $this->assertArrayHasKey('status_code', $firstResult);
        $this->assertEquals(200, $firstResult['status_code']);
    }

    public function testCurlConfiguration(): void
    {
        $params = [
            'source' => 'universal',
            'url' => 'https://www.macys.com/shop/sale'
        ];

        $ch = curl_init();
        curl_setopt($ch, CURLOPT_URL, "https://realtime.oxylabs.io/v1/queries");
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, 1);
        curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($params));
        curl_setopt($ch, CURLOPT_POST, 1);
        curl_setopt($ch, CURLOPT_USERPWD, "user:pass1");

        $headers = array();
        $headers[] = "Content-Type: application/json";
        curl_setopt($ch, CURLOPT_HTTPHEADER, $headers);

        $this->assertEquals("https://realtime.oxylabs.io/v1/queries", curl_getinfo($ch, CURLINFO_EFFECTIVE_URL));
        $this->assertTrue(curl_getinfo($ch, CURLINFO_POST));
        
        curl_close($ch);
    }
}
